// RichTextKit
// Copyright © 2019 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.

using SkiaSharp;
using System;
using System.Collections.Generic;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
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
                            count = 1;
                            if (RunFace == null)
                            {
                                RunFace = typeface;
                                count = 1;
                            }
                            else
                            {
                                // Consume as many as possible
                                count = RunFace.GetGlyphs((IntPtr)(pch + pos), length - pos, SKEncoding.Utf32, out var glyphs);

                                // But don't take control characters or spaces...
                                for (int i = 1; i < count; i++)
                                {
                                    if (pch[pos] <= 32)
                                    {
                                        count = i;
                                        break;
                                    }
                                }
                            }
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
