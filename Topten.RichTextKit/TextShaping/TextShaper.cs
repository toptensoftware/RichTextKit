using HarfBuzzSharp;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Helper class for shaping text
    /// </summary>
    public class TextShaper : IDisposable
    {
        /// <summary>
        /// Cache of shapers for typefaces
        /// </summary>
        static Dictionary<SKTypeface, TextShaper> _shapers = new Dictionary<SKTypeface, TextShaper>();

        /// <summary>
        /// Get the text shaper for a particular type face
        /// </summary>
        /// <param name="typeface">The typeface being queried for</param>
        /// <returns>A TextShaper</returns>
        public static TextShaper ForTypeface(SKTypeface typeface)
        {
            lock (_shapers)
            {
                if (!_shapers.TryGetValue(typeface, out var shaper))
                {
                    shaper = new TextShaper(typeface);
                    _shapers.Add(typeface, shaper);
                }

                return shaper;
            }
        }

        /// <summary>
        /// Constructs a new TextShaper 
        /// </summary>
        /// <param name="typeface">The typeface of this shaper</param>
        private TextShaper(SKTypeface typeface)
        {
            // Load the typeface stream to a HarfBuzz font
            int index;
            using (var blob = typeface.OpenStream(out index).ToHarfBuzzBlob())
            using (var face = new Face(blob, (uint)index))
            {
                face.UnitsPerEm = (uint)typeface.UnitsPerEm;

                _font = new HarfBuzzSharp.Font(face);
                _font.SetScale(overScale, overScale);
                _font.SetFunctionsOpenType();
            }

            // Get font metrics for this typeface
            using (var paint = new SKPaint())
            {
                paint.Typeface = typeface;
                paint.TextSize = overScale;
                _fontMetrics = paint.FontMetrics;
            }
        }

        /// <summary>
        /// Dispose this text shaper
        /// </summary>
        public void Dispose()
        {
            if (_font != null)
            {
                _font.Dispose();
                _font = null;
            }
        }

        /// <summary>
        /// The HarfBuzz font for this shaper
        /// </summary>
        HarfBuzzSharp.Font _font;

        /// <summary>
        /// Font metrics for the font
        /// </summary>
        SKFontMetrics _fontMetrics;

        /// <summary>
        /// Returned as the result of a text shaping operation
        /// </summary>
        public struct Result
        {
            /// <summary>
            /// The glyph indicies of all glyphs required to render the shaped text
            /// </summary>
            public ushort[] GlyphIndicies;

            /// <summary>
            /// The position of each glyph
            /// </summary>
            public SKPoint[] Points;

            /// <summary>
            /// One entry for each glyph, showing the code point index
            /// of the characters it was derived from
            /// </summary>
            public int[] Clusters;

            /// <summary>
            /// The end position of the rendered text
            /// </summary>
            public SKPoint EndXCoord;

            /// <summary>
            /// The X-Position of each passed code point
            /// </summary>
            public float[] CodePointXCoords;

            /// <summary>
            /// The ascent of the font
            /// </summary>
            public float Ascent;

            /// <summary>
            /// The descent of the font
            /// </summary>
            public float Descent;

            /// <summary>
            /// The XMin for the font
            /// </summary>
            public float XMin;
        }

        /// <summary>
        /// Over scale used for all font operations
        /// </summary>
        const int overScale = 512;

        // Temporary hack until newer HarfBuzzSharp is released with support for AddUtf32
        [DllImport("libHarfBuzzSharp", CallingConvention = CallingConvention.Cdecl)]
		private extern static void hb_buffer_add_utf32 (IntPtr buffer, IntPtr text, int text_length, int item_offset, int item_length);

        /// <summary>
        /// Shape an array of utf-32 code points
        /// </summary>
        /// <param name="codePoints">The utf-32 code points to be shaped</param>
        /// <param name="style">The user style for the text</param>
        /// <param name="direction">LTR or RTL direction</param>
        /// <param name="clusterAdjustment">A value to add to all reported cluster numbers</param>
        /// <returns>A TextShaper.Result representing the shaped text</returns>
        public Result Shape(Slice<int> codePoints, IStyle style, TextDirection direction, int clusterAdjustment = 0)
        {
            using (var buffer = new HarfBuzzSharp.Buffer())
            {
                // Setup buffer
                unsafe
                {
                    fixed (int* pCodePoints = codePoints.Underlying)
                    {
                        hb_buffer_add_utf32(buffer.Handle, (IntPtr)(pCodePoints + codePoints.Start), codePoints.Length, 0, -1);
                    }
                }

                // Setup directionality (if supplied)
                switch (direction)
                {
                    case TextDirection.LTR:
                        buffer.Direction = Direction.LeftToRight;
                        break;

                    case TextDirection.RTL:
                        buffer.Direction = Direction.RightToLeft;
                        break;

                    default:
                        throw new ArgumentException(nameof(direction));
                }

                // Guess other attributes
                buffer.GuessSegmentProperties();

                // Shape it
                _font.Shape(buffer);

                // Create results
                var r = new Result();
                r.GlyphIndicies = buffer.GlyphInfos.Select(x => (ushort)x.Codepoint).ToArray();
                r.Clusters = buffer.GlyphInfos.Select(x => (int)x.Cluster + clusterAdjustment).ToArray();
                r.CodePointXCoords = new float[codePoints.Length];
                r.Points = new SKPoint[buffer.Length];

                // RTL?
                bool rtl = buffer.Direction == Direction.RightToLeft;

                // Work out glyph scaling and offsetting for super/subscript
                float glyphScale = style.FontSize / overScale;
                float glyphVOffset = 0;
                if (style.FontVariant == FontVariant.SuperScript)
                {
                    glyphScale *= 0.65f;
                    glyphVOffset -= style.FontSize * 0.35f;
                }
                if (style.FontVariant == FontVariant.SubScript)
                {
                    glyphScale *= 0.65f;
                    glyphVOffset += style.FontSize * 0.1f;
                }

                // Convert points
                var gp = buffer.GlyphPositions;
                float cursorX = 0;
                float cursorY = 0;
                for (int i = 0; i < buffer.Length; i++)
                {
                    // Update code point positions
                    if (!rtl)
                    {
                        // First cluster, different cluster, or same cluster with lower x-coord
                        if ( i == 0 ||
                            (r.Clusters[i] != r.Clusters[i - 1]) || 
                            (cursorX < r.CodePointXCoords[r.Clusters[i] - clusterAdjustment]))
                        {
                            r.CodePointXCoords[r.Clusters[i] - clusterAdjustment] = cursorX;
                        }
                    }
    
                    // Get the position
                    var pos = gp[i];

                    // Update glyph position
                    r.Points[i] = new SKPoint(
                        cursorX + pos.XOffset * glyphScale,
                        cursorY - pos.YOffset * glyphScale + glyphVOffset
                        );

                    // Update cursor position
                    cursorX += pos.XAdvance * glyphScale;
                    cursorY += pos.YAdvance * glyphScale;

                    // Store RTL cursor position
                    if (rtl)
                    {
                        // First cluster, different cluster, or same cluster with lower x-coord
                        if (i == 0 ||
                            (r.Clusters[i] != r.Clusters[i - 1]) ||
                            (cursorX > r.CodePointXCoords[r.Clusters[i] - clusterAdjustment]))
                        {
                            r.CodePointXCoords[r.Clusters[i] - clusterAdjustment] = cursorX;
                        }
                    }
                }

                // Finalize cursor positions by filling in any that weren't
                // referenced by a cluster
                if (rtl)
                {
                    r.CodePointXCoords[0] = cursorX;
                    for (int i = codePoints.Length - 2;  i >= 0; i--)
                    {
                        if (r.CodePointXCoords[i] == 0)
                            r.CodePointXCoords[i] = r.CodePointXCoords[i + 1];
                    }
                }
                else
                {
                    for (int i = 1; i < codePoints.Length; i++)
                    {
                        if (r.CodePointXCoords[i] == 0)
                            r.CodePointXCoords[i] = r.CodePointXCoords[i - 1];
                    }
                }

                // Also return the end cursor position
                r.EndXCoord = new SKPoint(cursorX, cursorY);

                // And some other useful metrics
                r.Ascent = _fontMetrics.Ascent * style.FontSize / overScale;
                r.Descent = _fontMetrics.Descent * style.FontSize / overScale;
                r.XMin = _fontMetrics.XMin * style.FontSize / overScale;

                // Done
                return r;
            }
        }
    }
}
