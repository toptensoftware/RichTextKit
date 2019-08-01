using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Topten.RichText
{
    /// <summary>
    /// Helper to split a run of code points based on a particular typeface
    /// into a series of runs where unsupported code points are mapped to a
    /// fallback font.
    /// </summary>
    class FontFallback
    {
        public struct Run
        {
            public int Start;
            public int Length;
            public SKTypeface Typeface;
        }

        public static IEnumerable<Run> GetFontRuns(Slice<int> codePoints, SKTypeface typeface)
        {
            var fontManager = SKFontManager.Default;

            int currentRunPos = 0;
            SKTypeface currentRunTypeface = null;
            int pos = 0;
            List<Run> runs = new List<Run>();

            unsafe
            {
                fixed (int* pCodePoints = codePoints.Underlying)
                {
                    int* pch = pCodePoints + codePoints.Start;
                    int length = codePoints.Length;
                    while (pos < length)
                    {
                        var RunFace = typeface;

                        int count = 0;
                        if (pch[pos] <= 32)
                        {
                            // Control characters and space always map to current typeface
                            count = 1;
                            RunFace = currentRunTypeface ?? typeface;
                        }
                        else
                        {
                            // Consume as many characters as possible using the requested type face
                            count = typeface.GetGlyphs((IntPtr)(pch + pos), length - pos, SKEncoding.Utf32, out var glyphs);
                        }

                        // Couldn't be mapped to current font, try to find a replacement
                        if (count == 0)
                        {
                            // Find fallback font
                            RunFace = fontManager.MatchCharacter(typeface.FamilyName, typeface.FontWeight, typeface.FontWidth, typeface.FontSlant, null, pch[pos]);

                            // If couldn't use the specified font
                            if (RunFace == null)
                                RunFace = typeface;

                            // Consume as many characters as possible using the requested type face
                            count = 1;// RunFace.GetGlyphs((IntPtr)(pch + pos), length - pos, SKEncoding.Utf32, out var glyphs);
                        }

                        // Do we need to start a new Run?
                        if (currentRunTypeface != RunFace)
                        {
                            flushCurrentRun();
                            currentRunTypeface = RunFace;
                            currentRunPos = pos;
                        }

                        // Move on
                        pos += count;
                    }
                }
            }

            // Flush the final Run
            flushCurrentRun();

            // Done
            return runs;

            void flushCurrentRun()
            {
                if (currentRunTypeface != null)
                {
                    runs.Add(new Run()
                    {
                        Start = currentRunPos,
                        Length = pos - currentRunPos,
                        Typeface = currentRunTypeface,
                    });
                }
            }
        }
    }
}
