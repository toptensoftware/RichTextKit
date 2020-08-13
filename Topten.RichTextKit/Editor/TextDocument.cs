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
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor
{
    /// <summary>
    /// Represents a the document part of a Document/View editor
    /// </summary>
    public partial class TextDocument
    {
        /// <summary>
        /// Constructs a new TextDocument
        /// </summary>
        public TextDocument()
        {
            // Create paragraph list
            _paragraphs = new List<Paragraph>();

            // Default margins
            MarginLeft = 10;
            MarginRight = 10;
            MarginTop = 10;
            MarginBottom = 10;

            // Temporary... add some text to work with
            _paragraphs.Add(new TextParagraph("The quick brown fox jumps over the lazy dog.\n"));
            _paragraphs.Add(new TextParagraph("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec pellentesque non ante ut luctus. Donec vitae augue vel augue hendrerit gravida. Fusce imperdiet nunc at.\n"));
            _paragraphs.Add(new TextParagraph("Vestibulum condimentum quam et neque facilisis venenatis. Nunc dictum lobortis.\n"));
        }


        /// <summary>
        /// Paint this text block
        /// </summary>
        /// <param name="canvas">The Skia canvas to paint to</param>
        /// <param name="fromYCoord">The top Y-Coord of the visible part of the document</param>
        /// <param name="toYCoord">The bottom Y-Coord of the visible part of the document</param>
        /// <param name="options">Options controlling the paint operation</param>
        public void Paint(SKCanvas canvas, float fromYCoord, float toYCoord, TextPaintOptions options = null)
        {
            // Make sure layout up to date
            Layout();

            // Find the starting paragraph
            int startParaIndex = _paragraphs.BinarySearch(fromYCoord, (para, a) =>
            {
                if (para.ContentYCoord > a)
                    return 1;
                if (para.ContentYCoord + para.ContentHeight < a)
                    return -1;
                return 0;
            });
            if (startParaIndex < 0)
                startParaIndex = ~startParaIndex;

            // Offset the selection range to be relative to the first paragraph
            // that will be painted
            if (options?.Selection != null) 
            {
                options.Selection = options.Selection.Value.Offset(-_paragraphs[startParaIndex].CodePointIndex);
            }

            // Paint...  
            for (int i=startParaIndex; i < _paragraphs.Count; i++)
            {
                // Get the paragraph
                var p = _paragraphs[i];

                // Quit if past the region to be painted?
                if (p.ContentYCoord > toYCoord)
                    break;

                // Paint it
                p.Paint(canvas, options);

                // Update the selection range for the next paragraph
                if (options?.Selection != null)
                {
                    options.Selection = options.Selection.Value.Offset(-p.Length);
                }
            }
        }


        /// <summary>
        /// Indicates if text should be wrapped
        /// </summary>
        public bool LineWrap
        {
            get => _lineWrap;
            set
            {
                if (_lineWrap != value)
                {
                    _lineWrap = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Specifies the page width of the document
        /// </summary>
        /// <remarks>
        /// This value is ignored for single line editor
        /// </remarks>
        public float PageWidth
        {
            get => _pageWidth;
            set
            {
                if (_pageWidth != value)
                {
                    _pageWidth = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// The document's left margin
        /// </summary>
        public float MarginLeft { get; private set; }

        /// <summary>
        /// The document's right margin
        /// </summary>
        public float MarginRight { get; private set; }

        /// <summary>
        /// The document's top margin
        /// </summary>
        public float MarginTop { get; private set; }

        /// <summary>
        /// The document's bottom margin
        /// </summary>
        public float MarginBottom { get; private set; }

        /// <summary>
        /// The total height of the document
        /// </summary>
        public float MeasuredHeight
        {
            get
            {
                Layout();
                return _measuredHeight;
            }
        }

        /// <summary>
        /// The total width of the document
        /// </summary>
        /// <remarks>
        /// For line-wrap documents this is the page width.
        /// For non-line-wrap documents this is the width of the widest paragraph.
        /// </remarks>
        public float MeasuredWidth
        {
            get
            {
                Layout();
                if (LineWrap)
                {
                    return _pageWidth;
                }
                else
                {
                    throw new NotImplementedException("MeasuredWidth for non-line-wrap documents not implemented");
                }
            }
        }

        /// <summary>
        /// Gets the total length of the document in code points
        /// </summary>
        public int Length
        {
            get
            {
                Layout();
                return _totalLength;
            }
        }

        /// <summary>
        /// Hit test this string
        /// </summary>
        /// <param name="x">The x-coordinate relative to top-left of the document</param>
        /// <param name="y">The y-coordinate relative to top-left of the document</param>
        /// <returns>A HitTestResult</returns>
        public HitTestResult HitTest(float x, float y)
        {
            // Find the closest paragraph
            var para = FindClosestParagraph(y);

            // Hit test the paragraph
            var htr = para.HitTest(x - para.ContentXCoord, y - para.ContentYCoord);

            // Text document doesn't support line indicies
            htr.ClosestLine = -1;
            htr.OverLine = -1;

            // Convert paragraph relative indicies to document relative indicies
            htr.ClosestCodePointIndex += para.CodePointIndex;
            if (htr.OverCodePointIndex >= 0)
                htr.OverCodePointIndex += para.CodePointIndex;

            // Done
            return htr;
        }

        /// <summary>
        /// Calculates useful information for displaying a caret
        /// </summary>
        /// <param name="codePointIndex">The code point index of the caret</param>
        /// <param name="altPosition">Returns the alternate caret position for the code point index</param>
        /// <returns>A CaretInfo struct, or CaretInfo.None</returns>
        public CaretInfo GetCaretInfo(CaretPosition position)
        {
            // Make sure layout up to date
            Layout();

            // Find the paragraph
            var paraIndex = GetParagraphForCodePointIndex(position, out var indexInParagraph);
            var para = _paragraphs[paraIndex];

            // Get caret info
            var ci = para.GetCaretInfo(new CaretPosition(indexInParagraph, position.AltPosition));

            // Adjust caret info to be relative to document
            ci.CodePointIndex += para.CodePointIndex;
            ci.CaretXCoord += para.ContentXCoord;
            ci.CaretRectangle.Offset(new SKPoint(para.ContentXCoord, para.ContentYCoord));
            ci.LineIndex = -1;      // Line numbers not supported on TextDocument

            // Done
            return ci;
        }


        /// <summary>
        /// Handles keyboard navigation events
        /// </summary>
        /// <param name="position">The current caret position</param>
        /// <param name="kind">The direction and type of caret movement</param>
        /// <param name="pageSize">Specifies the page size for page up/down navigation</param>
        /// <param name="ghostXCoord">Transient storage for XCoord of caret during vertical navigation</param>
        /// <returns>The new caret position</returns>
        public CaretPosition Navigate(CaretPosition position, NavigationKind kind, float pageSize, ref float? ghostXCoord)
        {
            switch (kind)
            {
                case NavigationKind.None:
                    ghostXCoord = null;
                    return position;

                case NavigationKind.CharacterLeft:
                    ghostXCoord = null;
                    return navigateIndicies(-1, p => p.CaretIndicies);

                case NavigationKind.CharacterRight:
                    ghostXCoord = null;
                    return navigateIndicies(1, p => p.CaretIndicies);

                case NavigationKind.LineUp:
                    return navigateLine(-1, ref ghostXCoord);

                case NavigationKind.LineDown:
                    return navigateLine(1, ref ghostXCoord);

                case NavigationKind.WordLeft:
                    ghostXCoord = null;
                    return navigateIndicies(-1, p => p.WordBoundaryIndicies);

                case NavigationKind.WordRight:
                    ghostXCoord = null;
                    return navigateIndicies(1, p => p.WordBoundaryIndicies);

                case NavigationKind.PageUp:
                    return navigatePage(-1, ref ghostXCoord);

                case NavigationKind.PageDown:
                    return navigatePage(1, ref ghostXCoord);

                case NavigationKind.LineHome:
                    ghostXCoord = null;
                    return navigateLineEnd(-1);

                case NavigationKind.LineEnd:
                    ghostXCoord = null;
                    return navigateLineEnd(1);

                case NavigationKind.DocumentHome:
                    ghostXCoord = null;
                    return new CaretPosition(0);

                case NavigationKind.DocumentEnd:
                    ghostXCoord = null;
                    return new CaretPosition(Length, true);

                default:
                    throw new ArgumentException("Unknown navigation kind");
            }

            // Helper for character/word left/right
            CaretPosition navigateIndicies(int direction, Func<Paragraph, IReadOnlyList<int>> getIndicies)
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Find the current caret index
                var indicies = getIndicies(para);
                var ii = indicies.BinarySearch(paraCodePointIndex);

                // Work out the new position
                if (ii < 0)
                {
                    ii = (~ii);
                    if (direction > 0)
                        ii--;
                }
                ii += direction;


                if (ii < 0)
                {
                    // Move to end of previous paragraph
                    if (paraIndex > 0)
                        return new CaretPosition(_paragraphs[paraIndex - 1].CodePointIndex + _paragraphs[paraIndex - 1].Length - 1);
                    else
                        return new CaretPosition(0);
                }

                if (ii >= indicies.Count)
                {
                    // Move to start of next paragraph
                    if (paraIndex + 1 < _paragraphs.Count)
                        return new CaretPosition(_paragraphs[paraIndex + 1].CodePointIndex);
                    else
                        return new CaretPosition(Length);
                }

                // Move to new position in this paragraph
                return new CaretPosition(para.CodePointIndex + indicies[ii]);
            }

            // Helper for line up/down
            CaretPosition navigateLine(int direction, ref float? xCoord)
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Get the line number the caret is on
                var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

                // Resolve the xcoord
                if (xCoord == null)
                    xCoord = ci.CaretXCoord + MarginLeft + para.MarginLeft;

                // Work out which line to hit test
                var lineIndex = ci.LineIndex + direction;

                // Exceed paragraph?
                if (lineIndex < 0)
                {
                    // Top of document?
                    if (paraIndex == 0)
                        return position;

                    // Move to last line of previous paragraph
                    para = _paragraphs[paraIndex - 1];
                    lineIndex = para.LineIndicies.Count - 1;
                }
                else if (lineIndex >= para.LineIndicies.Count)
                {
                    // End of document?
                    if (paraIndex + 1 >= _paragraphs.Count)
                        return position;

                    // Move to first line of next paragraph
                    para = _paragraphs[paraIndex + 1];
                    lineIndex = 0;
                }

                // Hit test the line
                var htr = para.HitTestLine(lineIndex, xCoord.Value - MarginLeft - para.MarginLeft);
                return new CaretPosition(para.CodePointIndex + htr.ClosestCodePointIndex, htr.AltCaretPosition);
            }

            // Helper for line home/end
            CaretPosition navigateLineEnd(int direction)
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Get the line number the caret is on
                var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

                // Get the line indicies
                var lineIndicies = para.LineIndicies;

                // Handle out of range
                if (ci.LineIndex < 0)
                    return new CaretPosition(para.CodePointIndex);

                if (direction < 0)
                {
                    // Return code point index of this line
                    return new CaretPosition(para.CodePointIndex + lineIndicies[ci.LineIndex]);
                }
                else
                {
                    // Last unwrapped line?
                    if (ci.LineIndex + 1 >= lineIndicies.Count)
                        return new CaretPosition(para.CodePointIndex + para.Length - 1);

                    // Return code point index of the next line, but with alternate caret position
                    // so caret appears at the end of this line
                    return new CaretPosition(para.CodePointIndex + lineIndicies[ci.LineIndex + 1], altPosition: true);
                }
            }

            // Helper for page up/down
            CaretPosition navigatePage(int direction, ref float? xCoord)
            {
                // Get current caret position
                var ci = this.GetCaretInfo(position);

                // Work out which XCoord to use
                if (xCoord == null)
                    xCoord = ci.CaretXCoord;

                // Hit test one page up
                var htr = this.HitTest(xCoord.Value, ci.CaretRectangle.MidY + pageSize * direction);

                // Convert to caret position
                return new CaretPosition(htr.ClosestCodePointIndex, htr.AltCaretPosition);
            }

        }

        /// <summary>
        /// Given a caret position, find an enclosing selection range for the
        /// current word, line, paragraph or document
        /// </summary>
        /// <param name="position">The caret position to select from</param>
        /// <param name="kind">The kind of selection to return</param>
        /// <returns></returns>
        public TextRange GetSelectionRange(CaretPosition position, SelectionKind kind)
        {
            switch (kind)
            {
                case SelectionKind.None:
                    return new TextRange(position.CodePointIndex, position.CodePointIndex, position.AltPosition);

                case SelectionKind.Word:
                    return getWordRange();

                case SelectionKind.Line:
                    return getLineRange();

                case SelectionKind.Paragraph:
                    return getParagraphRange();

                case SelectionKind.Document:
                    return new TextRange(0, Length, true);

                default:
                    throw new ArgumentException("Unknown navigation kind");
            }

            // Helper to get a word range
            TextRange getWordRange()
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Find the word boundaries for this paragraph and find 
                // the current word
                var indicies = para.WordBoundaryIndicies;
                var ii = indicies.BinarySearch(paraCodePointIndex);
                if (ii < 0)
                    ii = (~ii - 1);
                if (ii >= indicies.Count)
                    ii = indicies.Count - 1;

                // Create text range covering the entire word
                return new TextRange(
                    para.CodePointIndex + indicies[ii],
                    para.CodePointIndex + indicies[ii + 1],
                    true
                );
            }

            // Helper to get a line range
            TextRange getLineRange()
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Get the line number the caret is on
                var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

                // Get the line indicies
                var lineIndicies = para.LineIndicies;

                // Handle out of range (should never happen)
                if (ci.LineIndex < 0)
                    ci.LineIndex = 0;
                if (ci.LineIndex >= lineIndicies.Count)
                    ci.LineIndex = lineIndicies.Count - 1;

                // Return the line range
                if (ci.LineIndex + 1 < lineIndicies.Count)
                {
                    // Line within the paragraph
                    return new TextRange(
                        para.CodePointIndex + lineIndicies[ci.LineIndex],
                        para.CodePointIndex + lineIndicies[ci.LineIndex + 1],
                        true
                    );
                }
                else
                {
                    // Last line
                    return new TextRange(
                        para.CodePointIndex + lineIndicies[ci.LineIndex],
                        para.CodePointIndex + para.Length,
                        true
                    );
                }
            }

            // Helper to get a paragraph range
            TextRange getParagraphRange()
            {
                // Get the paragraph and position in paragraph
                int paraIndex = GetParagraphForCodePointIndex(position, out var paraCodePointIndex);
                var para = _paragraphs[paraIndex];

                // Create text range covering the entire paragraph
                return new TextRange(
                    para.CodePointIndex,
                    para.CodePointIndex + para.Length,
                    true
                );
            }
        }


        /// <summary>
        /// Given a code point index relative to the document, return which
        /// paragraph contains that code point and the offset within the paragraph
        /// </summary>
        /// <param name="position">The caret position to locate the paragraph for</param>
        /// <returns>The index of the paragraph</returns>
        int GetParagraphForCodePointIndex(CaretPosition pos, out int indexInParagraph)
        {
            // Ensure layout is valid
            Layout();

            // Search paragraphs
            int paraIndex = _paragraphs.BinarySearch(pos.CodePointIndex, (para, a) => 
            {
                if (a < para.CodePointIndex)
                    return 1;
                if (a >= para.CodePointIndex + para.Length)
                    return -1;
                return 0;
            });
            if (paraIndex < 0)
                paraIndex = ~paraIndex;

            // Clamp to end of document
            if (paraIndex >= _paragraphs.Count)
                paraIndex = _paragraphs.Count - 1;

            // Work out offset within paragraph
            indexInParagraph = pos.CodePointIndex - _paragraphs[paraIndex].CodePointIndex;

            if (indexInParagraph == 0 && pos.AltPosition && paraIndex > 0)
            {
                paraIndex--;
                indexInParagraph = _paragraphs[paraIndex].Length;
            }

            // Clamp to end of paragraph
            if (indexInParagraph > _paragraphs[paraIndex].Length)
                indexInParagraph = _paragraphs[paraIndex].Length;

            System.Diagnostics.Debug.Assert(indexInParagraph >= 0);

            // Done
            return paraIndex;
        }

        /// <summary>
        /// Helper to find the closest paragraph to a y-coordinate 
        /// </summary>
        /// <param name="y">Y-Coord to hit test</param>
        /// <returns>A reference to the closest paragraph</returns>
        Paragraph FindClosestParagraph(float y)
        {
            // Ensure layout is valid
            Layout();

            // Search paragraphs
            int paraIndex = _paragraphs.BinarySearch(y, (para, a) =>
            {
                if (para.ContentYCoord > a)
                    return 1;
                if (para.ContentYCoord + para.ContentHeight < a)
                    return -1;
                return 0;
            });

            // If in the vertical margin space between paragraphs, find the 
            // paragraph whose content is closest
            if (paraIndex < 0)
            {
                // Convert the paragraph index
                paraIndex = ~paraIndex;

                // Is it between paragraphs? 
                // (ie: not above the first or below the last paragraph)
                if (paraIndex > 0 && paraIndex < _paragraphs.Count)
                {
                    // Yes, find which paragraph's content the position is closer too
                    var paraPrev = _paragraphs[paraIndex - 1];
                    var paraNext = _paragraphs[paraIndex];
                    if (Math.Abs(y - (paraPrev.ContentYCoord + paraPrev.ContentHeight)) <
                        Math.Abs(y - paraNext.ContentYCoord))
                    {
                        return paraPrev;
                    }
                    else
                    {
                        return paraNext;
                    }
                }
            }

            // Clamp to last paragraph
            if (paraIndex >= _paragraphs.Count)
                paraIndex = _paragraphs.Count - 1;

            // Return the paragraph
            return _paragraphs[paraIndex];
        }


        /// <summary>
        /// Mark the document as needing layout update
        /// </summary>
        void InvalidateLayout()
        {
            _layoutValid = false;
        }

        /// <summary>
        /// Update the layout of the document
        /// </summary>
        void Layout()
        {
            // Already valid?
            if (_layoutValid)
                return;
            _layoutValid = true;

            // Work out the starting code point index and y-coord and starting margin
            float yCoord = 0;
            float prevYMargin = MarginTop;
            int codePointIndex = 0;

            // Layout paragraphs
            for (int i = 0; i < _paragraphs.Count; i++)
            {
                // Get the paragraph
                var para = _paragraphs[i];

                // Layout
                para.Layout(this);

                // Position
                para.ContentXCoord = MarginLeft + para.MarginLeft;
                para.ContentYCoord = yCoord + Math.Max(para.MarginTop, prevYMargin);
                para.CodePointIndex = codePointIndex;

                // Update positions
                yCoord = para.ContentYCoord + para.ContentHeight;
                prevYMargin = para.MarginBottom;
                codePointIndex += para.Length;
            }

            // Update the totals
            _measuredHeight = yCoord + Math.Max(prevYMargin, MarginBottom);
            _totalLength = codePointIndex;
        }

        /// Private members
        float _pageWidth = 1000;            // Arbitary default
        float _measuredHeight = 0;
        int _totalLength = 0;
        bool _layoutValid = false;
        bool _lineWrap = true;
        List<Paragraph> _paragraphs;
    }
}
