using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Topten.RichText
{
    /// <summary>
    /// Represents a single font run
    /// </summary>
    public class FontRun
    {
        /// <summary>
        /// The kind of run
        /// </summary>
        public FontRunKind RunKind = FontRunKind.Normal;

        /// <summary>
        /// The user run this typeface run was derived from
        /// </summary>
        public StyledRun StyledRun;

        /// <summary>
        /// Get the code points of this run
        /// </summary>
        public Slice<int> CodePoints => CodePointBuffer.SubSlice(Start, Length);

        /// <summary>
        /// Index into _codePoints buffer of the start of this run
        /// </summary>
        public int Start;

        /// <summary>
        /// The length of this run (in codepoints)
        /// </summary>
        public int Length;

        /// <summary>
        /// The index of the first character after this run
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// The user supplied style for this run
        /// </summary>
        public IStyle Style;

        /// <summary>
        /// The direction of this run
        /// </summary>
        public TextDirection Direction;

        /// <summary>
        /// The typeface of this run (use this over Style.Fontface)
        /// </summary>
        public SKTypeface Typeface;

        /// <summary>
        /// The glyph indicies
        /// </summary>
        public Slice<ushort> Glyphs;

        /// <summary>
        /// The glyph positions (relative to the entire text block)
        /// </summary>
        public Slice<SKPoint> GlyphPositions;

        /// <summary>
        /// The cluster numbers for each glyph
        /// </summary>
        public Slice<int> Clusters;

        /// <summary>
        /// The x-coords of each code point, relative to this text run
        /// </summary>
        public Slice<float> RelativeCodePointXCoords;

        /// <summary>
        /// Get the x-coord of a code point
        /// </summary>
        /// <remarks>
        /// For LTR runs this will be the x-coordinate to the left, or RTL
        /// runs it will be the x-coordinate to the right.
        /// </remarks>
        /// <param name="codePointIndex">The code point index (relative to the entire text block)</param>
        /// <returns>The x-coord relative to the entire text block</returns>
        public float GetXCoordOfCodePointIndex(int codePointIndex)
        {
            // Check in range
            if (codePointIndex < Start || codePointIndex > End)
                throw new ArgumentOutOfRangeException(nameof(codePointIndex));

            // End of run?
            if (codePointIndex == End)
                return XCoord + (Direction == TextDirection.LTR ? Width : 0);

            // Lookup
            return XCoord + RelativeCodePointXCoords[codePointIndex - Start];
        }

        /// <summary>
        /// The ascent of the font used in this run
        /// </summary>
        public float Ascent;

        /// <summary>
        /// The descent of the font used in this run
        /// </summary>
        public float Descent;


        /// <summary>
        /// The height of text in this run (ascent + descent)
        /// </summary>
        public float TextHeight => -Ascent + Descent;

        /// <summary>
        /// Calculate the half leading height for text in this run
        /// </summary>
        public float HalfLeading => (TextHeight * Style.LineHeight - TextHeight) / 2;

        /// <summary>
        /// Width of this typeface run
        /// </summary>
        public float Width;

        /// <summary>
        /// Horizontal position of this run, relative to the left margin
        /// </summary>
        public float XCoord;

        /// <summary>
        /// The line that owns this font run 
        /// </summary>
        public TextLine Line { get; internal set; }

        /// <summary>
        /// Get the next font run from this one
        /// </summary>
        public FontRun NextRun
        {
            get
            {
                var allRuns = Line.TextBlock.FontRuns as List<FontRun>; 
                int index = allRuns.IndexOf(this);
                if (index < 0 || index + 1 >= Line.Runs.Count)
                    return null;
                return Line.Runs[index + 1];
            }
        }

        /// <summary>
        /// Get the previous font run from this one
        /// </summary>
        public FontRun PreviousRun
        {
            get
            {
                var allRuns = Line.TextBlock.FontRuns as List<FontRun>; 
                int index = allRuns.IndexOf(this);
                if (index <= 0)
                    return null;
                return Line.Runs[index + 1];
            }
        }

        /// <summary>
        /// For debugging
        /// </summary>
        /// <returns>Debug string</returns>
        public override string ToString()
        {
            return $"{Start} - {End} @ {XCoord} - {XCoord + Width} = '{Utf32Utils.FromUtf32(CodePoints)}'";
        }

        /// <summary>
        /// Moves all glyphs by the specified offset amount
        /// </summary>
        /// <param name="dx">The x-delta to move glyphs by</param>
        /// <param name="dy">The y-delta to move glyphs by</param>
        public void MoveGlyphs(float dx, float dy)
        {
            for (int i = 0; i < GlyphPositions.Length; i++)
            {
                GlyphPositions[i].X += dx;
                GlyphPositions[i].Y += dy;
            }
        }

        /// <summary>
        /// Calculates the leading width of all character from the start of the run (either 
        /// the left or right depending on run direction) to the specified code point
        /// </summary>
        /// <param name="codePoint">The code point index to measure to</param>
        /// <returns>The distance from the start to the specified code point</returns>
        public float LeadingWidth(int codePoint)
        {
            // At either end?
            if (codePoint == this.End)
                return this.Width;
            if (codePoint == 0)
                return 0;

            // Internal, calculate the leading width (ie from code point 0 to code point N)
            int codePointIndex = codePoint - this.Start;
            if (this.Direction == TextDirection.LTR)
            {
                return this.RelativeCodePointXCoords[codePointIndex];
            }
            else
            {
                return this.Width - this.RelativeCodePointXCoords[codePointIndex];
            }

        }

        /// <summary>
        /// Calculate the position at which to break a text run
        /// </summary>
        /// <param name="maxWidth">The max width available</param>
        /// <param name="force">Whether to force the use of at least one glyph</param>
        /// <returns>The code point position to break at</returns>
        internal int FindBreakPosition(float maxWidth, bool force)
        {
            int lastFittingCodePoint = this.Start;
            int firstNonZeroWidthCodePoint = -1;
            var prevWidth = 0f;
            for (int i = this.Start; i < this.End; i++)
            {
                var width = this.LeadingWidth(i);
                if (prevWidth != width)
                {
                    if (firstNonZeroWidthCodePoint < 0)
                        firstNonZeroWidthCodePoint = i;

                    if (width < maxWidth)
                    {
                        lastFittingCodePoint = i;
                    }
                    else
                    {
                        break;
                    }
                }
                prevWidth = width;
            }

            if (lastFittingCodePoint > this.Start || !force)
                return lastFittingCodePoint;

            if (firstNonZeroWidthCodePoint > this.Start)
                return firstNonZeroWidthCodePoint;

            // Split at the end
            return this.End;
        }

        /// <summary>
        /// Split a typeface run into two separate runs, truncating this run at 
        /// the specified code point index and returning a new run containing the
        /// split off part.
        /// </summary>
        /// <param name="splitAtCodePoint">The code point index to split at</param>
        /// <returns>A new typeface run for the split off part</returns>
        internal FontRun Split(int splitAtCodePoint)
        {
            if (this.Direction == TextDirection.LTR)
            {
                return SplitLTR(splitAtCodePoint);
            }
            else
            {
                return SplitRTL(splitAtCodePoint);
            }
        }

        /// <summary>
        /// Split a LTR typeface run into two separate runs, truncating the passed
        /// run (LHS) and returning a new run containing the split off part (RHS)
        /// </summary>
        /// <param name="run">The run to split</param>
        /// <param name="splitAtCodePoint">To code point position to split at</param>
        /// <returns>The RHS run after splitting</returns>
        private FontRun SplitLTR(int splitAtCodePoint)
        {
            // Check split point is internal to the run
            System.Diagnostics.Debug.Assert(this.Direction == TextDirection.LTR);
            System.Diagnostics.Debug.Assert(splitAtCodePoint > this.Start);
            System.Diagnostics.Debug.Assert(splitAtCodePoint < this.End);

            // Work out the split position
            int codePointSplitPos = splitAtCodePoint - this.Start;

            // Work out the width that we're slicing off
            float sliceLeftWidth = this.RelativeCodePointXCoords[codePointSplitPos];
            float sliceRightWidth = this.Width - sliceLeftWidth;

            // Work out the glyph split position
            int glyphSplitPos = 0;
            for (glyphSplitPos = 0; glyphSplitPos < this.Clusters.Length; glyphSplitPos++)
            {
                if (this.Clusters[glyphSplitPos] >= splitAtCodePoint)
                    break;
            }

            // Create the other run
            var newRun = new FontRun()
            {
                StyledRun = this.StyledRun,
                CodePointBuffer = this.CodePointBuffer,
                Direction = this.Direction,
                Ascent = this.Ascent,
                Descent = this.Descent,
                Style = this.Style,
                Typeface = this.Typeface,
                Start = splitAtCodePoint,
                Length = this.End - splitAtCodePoint,
                Width = sliceRightWidth,
                RelativeCodePointXCoords = this.RelativeCodePointXCoords.SubSlice(codePointSplitPos),
                GlyphPositions = this.GlyphPositions.SubSlice(glyphSplitPos),
                Glyphs = this.Glyphs.SubSlice(glyphSplitPos),
                Clusters = this.Clusters.SubSlice(glyphSplitPos),
            };

            // Adjust code point positions
            for (int i = 0; i < newRun.RelativeCodePointXCoords.Length; i++)
            {
                newRun.RelativeCodePointXCoords[i] -= sliceLeftWidth;
            }

            // Adjust glyph positions
            for (int i = 0; i < newRun.GlyphPositions.Length; i++)
            {
                newRun.GlyphPositions[i].X -= sliceLeftWidth;
            }

            // Update this run
            this.RelativeCodePointXCoords = this.RelativeCodePointXCoords.SubSlice(0, codePointSplitPos);
            this.Glyphs = this.Glyphs.SubSlice(0, glyphSplitPos);
            this.GlyphPositions = this.GlyphPositions.SubSlice(0, glyphSplitPos);
            this.Clusters = this.Clusters.SubSlice(0, glyphSplitPos);
            this.Width = sliceLeftWidth;
            this.Length = codePointSplitPos;

            // Return the new run
            return newRun;
        }

        /// <summary>
        /// Split a RTL typeface run into two separate runs, truncating the passed
        /// run (RHS) and returning a new run containing the split off part (LHS)
        /// </summary>
        /// <param name="run">The run to split</param>
        /// <param name="splitAtCodePoint">To code point position to split at</param>
        /// <returns>The LHS run after splitting</returns>
        private FontRun SplitRTL(int splitAtCodePoint)
        {
            // Check split point is internal to the run
            System.Diagnostics.Debug.Assert(this.Direction == TextDirection.RTL);
            System.Diagnostics.Debug.Assert(splitAtCodePoint > this.Start);
            System.Diagnostics.Debug.Assert(splitAtCodePoint < this.End);

            // Work out the split position
            int codePointSplitPos = splitAtCodePoint - this.Start;

            // Work out the width that we're slicing off
            float sliceLeftWidth = this.RelativeCodePointXCoords[codePointSplitPos];
            float sliceRightWidth = this.Width - sliceLeftWidth;

            // Work out the glyph split position
            int glyphSplitPos = 0;
            for (glyphSplitPos = this.Clusters.Length; glyphSplitPos > 0; glyphSplitPos--)
            {
                if (this.Clusters[glyphSplitPos - 1] >= splitAtCodePoint)
                    break;
            }

            // Create the other run
            var newRun = new FontRun()
            {
                StyledRun = this.StyledRun,
                CodePointBuffer = this.CodePointBuffer,
                Direction = this.Direction,
                Ascent = this.Ascent,
                Descent = this.Descent,
                Style = this.Style,
                Typeface = this.Typeface,
                Start = splitAtCodePoint,
                Length = this.End - splitAtCodePoint,
                Width = sliceLeftWidth,
                RelativeCodePointXCoords = this.RelativeCodePointXCoords.SubSlice(codePointSplitPos),
                GlyphPositions = this.GlyphPositions.SubSlice(0, glyphSplitPos),
                Glyphs = this.Glyphs.SubSlice(0, glyphSplitPos),
                Clusters = this.Clusters.SubSlice(0, glyphSplitPos),
            };

            // Update this run
            this.RelativeCodePointXCoords = this.RelativeCodePointXCoords.SubSlice(0, codePointSplitPos);
            this.Glyphs = this.Glyphs.SubSlice(glyphSplitPos);
            this.GlyphPositions = this.GlyphPositions.SubSlice(glyphSplitPos);
            this.Clusters = this.Clusters.SubSlice(glyphSplitPos);
            this.Width = sliceRightWidth;
            this.Length = codePointSplitPos;

            // Adjust code point positions
            for (int i = 0; i < this.RelativeCodePointXCoords.Length; i++)
            {
                this.RelativeCodePointXCoords[i] -= sliceLeftWidth;
            }

            // Adjust glyph positions
            for (int i = 0; i < this.GlyphPositions.Length; i++)
            {
                this.GlyphPositions[i].X -= sliceLeftWidth;
            }

            // Return the new run
            return newRun;
        }

        /// <summary>
        /// The global list of code points
        /// </summary>
        internal Buffer<int> CodePointBuffer;

        /// <summary>
        /// Calculate how by how much text at the left margin overhangs the margin
        /// </summary>
        /// <returns>The amount of overhang</returns>
        internal float CalculateRequiredLeftMargin()
        {
            if (Glyphs.Length == 0)
                return 0;

            using (var paint = new SKPaint())
            {
                float glyphScale = 1;
                if (Style.FontVariant == FontVariant.SuperScript)
                {
                    glyphScale = 0.65f;
                }
                if (Style.FontVariant == FontVariant.SubScript)
                {
                    glyphScale = 0.65f;
                }

                paint.TextEncoding = SKTextEncoding.GlyphId;
                paint.Typeface = Typeface;
                paint.TextSize = Style.FontSize * glyphScale;
                paint.SubpixelText = true;
                paint.IsAntialias = true;
                paint.LcdRenderText = false;

                unsafe
                {
                    fixed (ushort* pGlyphs = Glyphs.Underlying)
                    {
                        paint.GetGlyphWidths((IntPtr)(pGlyphs + Start), sizeof(ushort), out var bounds);
                        if (bounds != null && bounds.Length >= 1)
                        {
                            var lhs = XCoord + bounds[0].Left;
                            if (lhs < 0)
                                return -lhs;
                        }
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Paint this font run
        /// </summary>
        /// <param name="ctx"></param>
        internal void Paint(PaintTextContext ctx)
        {
            // Paint selection?
            if (ctx.PaintSelectionBackground != null)
            {
                float selStartXCoord;
                if (ctx.SelectionStart < Start)
                    selStartXCoord = Direction == TextDirection.LTR ? 0 : Width;
                else if (ctx.SelectionStart >= End)
                    selStartXCoord = Direction == TextDirection.LTR ? Width : 0;
                else
                    selStartXCoord = RelativeCodePointXCoords[ctx.SelectionStart - this.Start];

                float selEndXCoord;
                if (ctx.SelectionEnd < Start)
                    selEndXCoord = Direction == TextDirection.LTR ? 0 : Width;
                else if (ctx.SelectionEnd >= End)
                    selEndXCoord = Direction == TextDirection.LTR ? Width : 0;
                else
                    selEndXCoord = RelativeCodePointXCoords[ctx.SelectionEnd - this.Start];

                if (selStartXCoord != selEndXCoord)
                {
                    var rect = new SKRect(this.XCoord + selStartXCoord, Line.YPosition, 
                                            this.XCoord + selEndXCoord, Line.YPosition + Line.Height);
                    ctx.Canvas.DrawRect(rect, ctx.PaintSelectionBackground);
                }
            }

            // Don't paint trailing whitespace runs
            if (RunKind == FontRunKind.TrailingWhitespace)
                return;

            // Text 
            using (var paint = new SKPaint())
            {
                // Work out font variant adjustments
                float glyphScale = 1;
                float glyphVOffset = 0;
                if (Style.FontVariant == FontVariant.SuperScript)
                {
                    glyphScale = 0.65f;
                    glyphVOffset = -Style.FontSize * 0.35f;
                }
                if (Style.FontVariant == FontVariant.SubScript)
                {
                    glyphScale = 0.65f;
                    glyphVOffset = Style.FontSize * 0.1f;
                }

                // Setup SKPaint
                paint.Color = Style.TextColor;
                paint.TextEncoding = SKTextEncoding.GlyphId;
                paint.Typeface = Typeface;
                paint.TextSize = Style.FontSize * glyphScale;
                paint.SubpixelText = true;
                paint.IsAntialias = ctx.Options.IsAntialias;
                paint.LcdRenderText = ctx.Options.LcdRenderText;

                unsafe
                {
                    fixed (ushort* pGlyphs = Glyphs.Underlying)
                    {
                        // Get glyph positions
                        var glyphPositions = GlyphPositions.ToArray();

                        // Paint underline
                        if (Style.Underline != UnderlineStyle.None && RunKind == FontRunKind.Normal)
                        {
                            // Work out underline metrics
                            paint.TextSize = Style.FontSize;
                            float underlineYPos = Line.YPosition + Line.BaseLine + (paint.FontMetrics.UnderlinePosition ?? 0);
                            paint.StrokeWidth = paint.FontMetrics.UnderlineThickness ?? 0;
                            paint.TextSize = Style.FontSize * glyphScale;

                            if (Style.Underline == UnderlineStyle.Gapped)
                            {
                                // Get intercept positions
                                var interceptPositions = paint.GetPositionedTextIntercepts(
                                    (IntPtr)(pGlyphs + Glyphs.Start),
                                    Glyphs.Length * sizeof(ushort),
                                    glyphPositions, underlineYPos - paint.StrokeWidth / 2, underlineYPos + paint.StrokeWidth);

                                // Paint gapped underlinline
                                float x = XCoord;
                                for (int i = 0; i < interceptPositions.Length; i += 2)
                                {
                                    float b = interceptPositions[i] - paint.StrokeWidth;
                                    if (x < b)
                                    {
                                        ctx.Canvas.DrawLine(new SKPoint(x, underlineYPos), new SKPoint(b, underlineYPos), paint);
                                    }
                                    x = interceptPositions[i + 1] + paint.StrokeWidth;
                                }
                                if (x < XCoord + Width)
                                {
                                    ctx.Canvas.DrawLine(new SKPoint(x, underlineYPos), new SKPoint(XCoord + Width, underlineYPos), paint);
                                }
                            }
                            else
                            {
                                // Paint solid underline
                                ctx.Canvas.DrawLine(new SKPoint(XCoord, underlineYPos), new SKPoint(XCoord + Width, underlineYPos), paint);
                            }
                        }

                        // Draw the text
                        ctx.Canvas.DrawPositionedText((IntPtr)(pGlyphs + Glyphs.Start), Glyphs.Length * sizeof(ushort), glyphPositions, paint);
                    }
                }

                // Paint strikethrough
                if (Style.StrikeThrough != StrikeThroughStyle.None && RunKind == FontRunKind.Normal)
                {
                    paint.StrokeWidth = paint.FontMetrics.StrikeoutThickness ?? 0;
                    float strikeYPos = Line.YPosition + Line.BaseLine + (paint.FontMetrics.StrikeoutPosition ?? 0) + glyphVOffset;
                    ctx.Canvas.DrawLine(new SKPoint(XCoord, strikeYPos), new SKPoint(XCoord + Width, strikeYPos), paint);
                }
            }
        }
    }
}
