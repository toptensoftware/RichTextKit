
// RichTextKit
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
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
using System.Diagnostics;
using System.Linq;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Represents a block of formatted, laid out and measurable text
    /// </summary>
    public class StyledText
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public StyledText()
        {
        }

        /// <summary>
        /// Constructs a styled text block from unstyled text
        /// </summary>
        /// <param name="codePoints"></param>
        public StyledText(Slice<int> codePoints)
        {
            AddText(codePoints, null);
        }

        /// <summary>
        /// Clear the content of this text block
        /// </summary>
        public virtual void Clear()
        {
            // Reset everything
            _codePoints.Clear();
            StyleRun.Pool.Value.ReturnAndClear(_styleRuns);
            _hasTextDirectionOverrides = false;
            OnChanged();
        }

        /// <summary>
        /// The length of the added text in code points
        /// </summary>
        public int Length => _codePoints.Length;

        /// <summary>
        /// Get the code points of this text block
        /// </summary>
        public Utf32Buffer CodePoints => _codePoints;

        /// <summary>
        /// Get the text runs as added by AddText
        /// </summary>
        public IReadOnlyList<StyleRun> StyleRuns
        {
            get
            {
                return _styleRuns;
            }
        }

        /// <summary>
        /// Converts a code point index to a character index
        /// </summary>
        /// <param name="codePointIndex">The code point index to convert</param>
        /// <returns>The converted index</returns>
        public int CodePointToCharacterIndex(int codePointIndex)
        {
            return _codePoints.Utf32OffsetToUtf16Offset(codePointIndex);
        }

        /// <summary>
        /// Converts a character index to a code point index
        /// </summary>
        /// <param name="characterIndex">The character index to convert</param>
        /// <returns>The converted index</returns>
        public int CharacterToCodePointIndex(int characterIndex)
        {
            return _codePoints.Utf16OffsetToUtf32Offset(characterIndex);
        }

        /// <summary>
        /// Add text to this text block
        /// </summary>
        /// <remarks>
        /// The added text will be internally coverted to UTF32.  
        /// 
        /// Note that all text indicies returned by and accepted by this object will 
        /// be UTF32 "code point indicies".  To convert between UTF16 character indicies 
        /// and UTF32 code point indicies use the <see cref="CodePointToCharacterIndex(int)"/> 
        /// and <see cref="CharacterToCodePointIndex(int)"/> methods
        /// </remarks>
        /// <param name="text">The text to add</param>
        /// <param name="style">The style of the text</param>
        public void AddText(ReadOnlySpan<char> text, IStyle style)
        {
            // Quit if redundant
            if (text.Length == 0)
                return;

            // Add to  buffer
            var utf32 = _codePoints.Add(text);

            // Create a run
            var run = StyleRun.Pool.Value.Get();
            run.CodePointBuffer = _codePoints;
            run.Start = utf32.Start;
            run.Length = utf32.Length;
            run.Style = style;
            if (style != null)
                _hasTextDirectionOverrides |= style.TextDirection != TextDirection.Auto;

            // Add run
            _styleRuns.Add(run);

            // Need new layout
            OnChanged();
        }

        /// <summary>
        /// Add text to this paragraph
        /// </summary>
        /// <param name="text">The text to add</param>
        /// <param name="style">The style of the text</param>
        public void AddText(Slice<int> text, IStyle style)
        {
            if (text.Length == 0)
                return;

            // Add to UTF-32 buffer
            var utf32 = _codePoints.Add(text);

            // Create a run
            var run = StyleRun.Pool.Value.Get();
            run.CodePointBuffer = _codePoints;
            run.Start = utf32.Start;
            run.Length = utf32.Length;
            run.Style = style;
            if (style != null)
                _hasTextDirectionOverrides |= style.TextDirection != TextDirection.Auto;

            // Add run
            _styleRuns.Add(run);

            // Need new layout
            OnChanged();
        }

        /// <summary>
        /// Add text to this text block
        /// </summary>
        /// <remarks>
        /// The added text will be internally coverted to UTF32.  
        /// 
        /// Note that all text indicies returned by and accepted by this object will 
        /// be UTF32 "code point indicies".  To convert between UTF16 character indicies 
        /// and UTF32 code point indicies use the <see cref="CodePointToCharacterIndex(int)"/> 
        /// and <see cref="CharacterToCodePointIndex(int)"/> methods
        /// </remarks>
        /// <param name="text">The text to add</param>
        /// <param name="style">The style of the text</param>
        public void AddText(string text, IStyle style)
        {
            AddText(text.AsSpan(), style);
        }

        /// <summary>
        /// Add all the text from another text block to this text block
        /// </summary>
        /// <param name="text">Text to add</param>
        public void AddText(StyledText text)
        {
            foreach (var sr in text.StyleRuns)
            {
                AddText(sr.CodePoints, sr.Style);
            }
        }

        /// <summary>
        /// Add all the text from another text block to this text block
        /// </summary>
        /// <param name="offset">The position at which to insert the text</param>
        /// <param name="text">Text to add</param>
        public void InsertText(int offset, StyledText text)
        {
            foreach (var sr in text.StyleRuns)
            {
                InsertText(offset, sr.CodePoints, sr.Style);
                offset += sr.CodePoints.Length;
            }
        }

        /// <summary>
        /// Add text to this text block
        /// </summary>
        /// <remarks>
        /// If the style is null, the new text will acquire the style of the character
        /// before the insertion point. If the text block is currently empty the style
        /// must be supplied.  If inserting at the start of a non-empty text block the
        /// style will be that of the first existing style run
        /// </remarks>
        /// <param name="position">The position to insert the text</param>
        /// <param name="text">The text to add</param>
        /// <param name="style">The style of the text (optional)</param>
        public void InsertText(int position, Slice<int> text, IStyle style = null)
        {
            // Redundant?
            if (text.Length == 0)
                return;

            if (style == null && _styleRuns.Count == 0)
                throw new InvalidOperationException("Must supply style when inserting into an empty text block");

            // Add to UTF-32 buffer
            var utf32 = _codePoints.Insert(position, text);

            // Update style runs
            FinishInsert(utf32, style);
        }

        /// <summary>
        /// Add text to this text block
        /// </summary>
        /// <remarks>
        /// If the style is null, the new text will acquire the style of the character
        /// before the insertion point. If the text block is currently empty the style
        /// must be supplied.  If inserting at the start of a non-empty text block the
        /// style will be that of the first existing style run
        /// </remarks>
        /// <param name="position">The position to insert the text</param>
        /// <param name="text">The text to add</param>
        /// <param name="style">The style of the text (optional)</param>
        public void InsertText(int position, ReadOnlySpan<char> text, IStyle style = null)
        {
            // Redundant?
            if (text.Length == 0)
                return;

            if (style == null && _styleRuns.Count == 0)
                throw new InvalidOperationException("Must supply style when inserting into an empty text block");

            // Add to UTF-32 buffer
            var utf32 = _codePoints.Insert(position, text);

            // Update style runs
            FinishInsert(utf32, style);
        }

        /// <summary>
        /// Add text to this text block
        /// </summary>
        /// <remarks>
        /// If the style is null, the new text will acquire the style of the character
        /// before the insertion point. If the text block is currently empty the style
        /// must be supplied.  If inserting at the start of a non-empty text block the
        /// style will be that of the first existing style run
        /// </remarks>
        /// <param name="position">The position to insert the text</param>
        /// <param name="text">The text to add</param>
        /// <param name="style">The style of the text (optional)</param>
        public void InsertText(int position, string text, IStyle style = null)
        {
            // Redundant?
            if (text.Length == 0)
                return;

            if (style == null && _styleRuns.Count == 0)
                throw new InvalidOperationException("Must supply style when inserting into an empty text block");

            // Add to UTF-32 buffer
            var utf32 = _codePoints.Insert(position, text);

            // Update style runs
            FinishInsert(utf32, style);
        }

        /// <summary>
        /// Deletes text from this text block
        /// </summary>
        /// <param name="position">The code point index to delete from</param>
        /// <param name="length">The number of code points to delete</param>
        public void DeleteText(int position, int length)
        {
            if (length == 0)
                return;

            // Delete text from the code point buffer
            _codePoints.Delete(position, length);

            // Fix up style runs
            for (int i = 0; i < _styleRuns.Count; i++)
            {
                // Get the run
                var sr = _styleRuns[i];

                // Ignore runs before the deleted range
                if (sr.End <= position)
                    continue;

                // Runs that start before the deleted range
                if (sr.Start < position)
                {
                    if (sr.End <= position + length)
                    {
                        // Truncate runs the overlap with the start of the delete range
                        sr.Length = position - sr.Start;
                        continue;
                    }
                    else
                    {
                        // Shorten runs that completely cover the deleted range
                        sr.Length -= length;
                        continue;
                    }
                }

                // Runs that start within the deleted range
                if (sr.Start < position + length)
                {
                    if (sr.End <= position + length)
                    {
                        // Delete runs that are completely within the deleted range
                        _styleRuns.RemoveAt(i);
                        StyleRun.Pool.Value.Return(sr);
                        i--;
                        continue;
                    }
                    else
                    {
                        // Runs that overlap the end of the deleted range, just
                        // keep the part past the deleted range
                        sr.Length = sr.End - (position + length);
                        sr.Start = position;
                        continue;
                    }
                }

                // Run is after the deleted range, shuffle it back
                sr.Start -= length;
            }

            // coalesc runs
            CoalescStyleRuns();

            // Need new layout
            OnChanged();
        }

        /// <summary>
        /// Overwrites the styles of existing text in the text block
        /// </summary>
        /// <param name="position">The code point index of the start of the text</param>
        /// <param name="length">The length of the text</param>
        /// <param name="style">The new style to be applied</param>
        public void ApplyStyle(int position, int length, IStyle style)
        {
            // Check args
            if (position < 0 || position + length > this.Length)
                throw new ArgumentException("Invalid range");
            if (style == null)
                throw new ArgumentNullException(nameof(style));

            // Redundant?
            if (length == 0)
                return;

            // Easy case when applying same style to entire text block
            if (position == 0 && length == this.Length)
            {
                // Remove excess runs
                while (_styleRuns.Count > 1)
                {
                    StyleRun.Pool.Value.Return(_styleRuns[1]);
                    _styleRuns.RemoveAt(1);
                }

                // Reconfigure the first
                _styleRuns[0].Start = 0;
                _styleRuns[0].Length = length;
                _styleRuns[0].Style = style;

                // Reset text direction overrides flag
                _hasTextDirectionOverrides = style.TextDirection != TextDirection.Auto;

                // Invalidate and done
                OnChanged();
                return;
            }

            // Get all intersecting runs
            int newRunPos = -1;
            foreach (var subRun in _styleRuns.GetIntersectingRunsReverse(position, length))
            {
                if (subRun.Partial)
                {
                    var run = _styleRuns[subRun.Index];


                    if (subRun.Offset == 0)
                    {
                        // Overlaps start of existing run, keep end
                        run.Start += subRun.Length;
                        run.Length -= subRun.Length;
                        newRunPos = subRun.Index;
                    }
                    else if (subRun.Offset + subRun.Length == run.Length)
                    {
                        // Overlaps end of existing run, keep start
                        run.Length = subRun.Offset;
                        newRunPos = subRun.Index + 1;
                    }
                    else
                    {
                        // Internal to existing run, keep start and end

                        // Create new run for end
                        var endRun = StyleRun.Pool.Value.Get();
                        endRun.CodePointBuffer = _codePoints;
                        endRun.Start = run.Start + subRun.Offset + subRun.Length;
                        endRun.Length = run.End - endRun.Start;
                        endRun.Style = run.Style;
                        _styleRuns.Insert(subRun.Index + 1, endRun);

                        // Shorten the existing run to keep start
                        run.Length = subRun.Offset;

                        newRunPos = subRun.Index + 1;
                    }
                }
                else
                {
                    // Remove completely covered style runs
                    StyleRun.Pool.Value.Return(_styleRuns[subRun.Index]);
                    _styleRuns.RemoveAt(subRun.Index);
                    newRunPos = subRun.Index;
                }
            }

            // Create style run for the new style
            var newRun = StyleRun.Pool.Value.Get();
            newRun.CodePointBuffer = _codePoints;
            newRun.Start = position;
            newRun.Length = length;
            newRun.Style = style;

            _hasTextDirectionOverrides |= style.TextDirection != TextDirection.Auto;

            // Insert it
            _styleRuns.Insert(newRunPos, newRun);

            // Coalesc
            CoalescStyleRuns();

            // Need to redo layout
            OnChanged();
        }

        /// <summary>
        /// Extract text from this styled text block
        /// </summary>
        /// <param name="from">The code point offset to extract from</param>
        /// <param name="length">The number of code points to extract</param>
        /// <returns>A new text block with the RHS split part of the text</returns>
        public StyledText Extract(int from, int length)
        {
            // Create a new text block with the same attributes as this one
            var other = new StyledText();

            // Copy text to the new paragraph
            foreach (var subRun in _styleRuns.GetInterectingRuns(from, length))
            {
                var sr = _styleRuns[subRun.Index];
                other.AddText(sr.CodePoints.SubSlice(subRun.Offset, subRun.Length), sr.Style);
            }

            return other;
        }

        /// <summary>
        /// Gets the style of the text at a specified offset
        /// </summary>
        /// <remarks>
        /// When on a style run boundary, returns the style of the preceeding run
        /// </remarks>
        /// <param name="offset">The code point offset in the text</param>
        /// <returns>An IStyle</returns>
        public IStyle GetStyleAtOffset(int offset)
        {
            if (Length == 0 || _styleRuns.Count == 0)
                return null;

            if (offset == 0)
                return _styleRuns[0].Style;

            int runIndex = _styleRuns.BinarySearch(offset, (sr, a) =>
            {
                if (a <= sr.Start)
                    return 1;
                if (a > sr.End)
                    return -1;
                return 0;
            });

            if (runIndex < 0)
                runIndex = ~runIndex;

            if (runIndex >= _styleRuns.Count)
                runIndex = _styleRuns.Count - 1;

            return _styleRuns[runIndex].Style;
        }

        /// <summary>
        /// Completes the insertion of text by inserting it's style run
        /// and updating the offsets of existing style runs.
        /// </summary>
        /// <param name="utf32">The utf32 slice that was inserted</param>
        /// <param name="style">The style of the inserted text</param>
        void FinishInsert(Slice<int> utf32, IStyle style)
        {
            // Update style runs
            int newRunIndex = 0;
            for (int i = 0; i < _styleRuns.Count; i++)
            {
                // Get the style run
                var sr = _styleRuns[i];

                // Before inserted text?
                if (sr.End < utf32.Start)
                    continue;

                // Special case for inserting at very start of text block
                // with no supplied style.
                if (sr.Start == 0 && utf32.Start == 0 && style == null)
                {
                    sr.Length += utf32.Length;
                    continue;
                }

                // After inserted text?
                if (sr.Start >= utf32.Start)
                {
                    sr.Start += utf32.Length;
                    continue;
                }

                // Inserting exactly at the end of a style run?
                if (sr.End == utf32.Start)
                {
                    if (style == null || style == sr.Style)
                    {
                        // Extend the existing run
                        sr.Length += utf32.Length;

                        // Force style to null to suppress later creation
                        // of a style run for it.
                        style = null;
                    }
                    else
                    {
                        // Remember this is where to insert the new
                        // style run
                        newRunIndex = i + 1;
                    }
                    continue;
                }

                Debug.Assert(sr.End > utf32.Start);
                Debug.Assert(sr.Start < utf32.Start);

                // Inserting inside an existing run
                if (style == null || style == sr.Style)
                {
                    // Extend the existing style run to cover
                    // the newly inserted text with the same style
                    sr.Length += utf32.Length;

                    // Force style to null to suppress later creation
                    // of a style run for it.
                    style = null;
                }
                else
                {
                    // Split this run and insert the new style run between

                    // Create the second part
                    var split = StyleRun.Pool.Value.Get();
                    split.CodePointBuffer = _codePoints;
                    split.Start = utf32.Start + utf32.Length;
                    split.Length = sr.End - utf32.Start;
                    split.Style = sr.Style;
                    _styleRuns.Insert(i + 1, split);

                    // Shorten this part
                    sr.Length = utf32.Start - sr.Start;

                    // Insert the newly styled run after this one
                    newRunIndex = i + 1;

                    // Skip the second part of the split in this for loop
                    // as we've already calculated it
                    i++;
                }
            }

            // Create a new style run
            if (style != null)
            {
                var run = StyleRun.Pool.Value.Get();
                run.CodePointBuffer = _codePoints;
                run.Start = utf32.Start;
                run.Length = utf32.Length;
                run.Style = style;
                _hasTextDirectionOverrides |= style.TextDirection != TextDirection.Auto;
                _styleRuns.Insert(newRunIndex, run);
            }

            // Coalesc if necessary
            if ((newRunIndex > 0 && _styleRuns[newRunIndex - 1].Style == style) ||
                (newRunIndex + 1 < _styleRuns.Count && _styleRuns[newRunIndex + 1].Style == style))
            {
                CoalescStyleRuns();
            }


            // Need new layout
            OnChanged();
        }

        /// <summary>
        /// Combines any consecutive style runs with the same style
        /// into a single run
        /// </summary>
        void CoalescStyleRuns()
        {
            // Nothing to do if no style runs
            if (_styleRuns.Count == 0)
            {
                _hasTextDirectionOverrides = false;
                return;
            }

            // Since we're iterating the entire set of style runs, might as 
            // we recalculate this flag while we're at it
            _hasTextDirectionOverrides = _styleRuns[0].Style.TextDirection != TextDirection.Auto;

            // No need to coalesc a single run
            if (_styleRuns.Count == 1)
                return;

            // Coalesc...
            var prev = _styleRuns[0];
            for (int i = 1; i < _styleRuns.Count; i++)
            {
                // Get the run
                var run = _styleRuns[i];

                // Update flag
                _hasTextDirectionOverrides |= run.Style.TextDirection != TextDirection.Auto;

                // Can run be coalesced?
                if (run.Style == prev.Style)
                {
                    // Yes
                    prev.Length += run.Length;
                    StyleRun.Pool.Value.Return(run);
                    _styleRuns.RemoveAt(i);
                    i--;
                }
                else
                {
                    // No, move on..
                    prev = run;
                }
            }
        }


        /// <summary>
        /// Called whenever the content of this styled text block changes
        /// </summary>
        protected virtual void OnChanged()
        {
        }

        /// <summary>
        /// All code points as supplied by user, accumulated into a single buffer
        /// </summary>
        protected Utf32Buffer _codePoints = new Utf32Buffer();

        /// <summary>
        /// A list of style runs, as supplied by user
        /// </summary>
        protected List<StyleRun> _styleRuns = new List<StyleRun>();

        /// <summary>
        /// Set to true if any style runs have a directionality override.
        /// </summary>
        protected bool _hasTextDirectionOverrides = false;

        /// <inheritdoc />
        public override string ToString()
        {
            return Utf32Utils.FromUtf32(CodePoints.AsSlice());
        }
    }
}
