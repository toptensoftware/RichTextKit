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
        /// <param name="options">Options controlling the paint operation</param>
        public void Paint(
            SKCanvas canvas,
            TextPaintOptions options = null)
        {
            // Make sure layout up to date
            Layout();

            // Paint all paragraphs      
            // TODO: Optimize to only paint visible paragraphs 
            foreach (var p in _paragraphs)
            {
                // Adjust the selection range to local code point offset
                if (options != null)
                {
                    options.SelectionStart -= p.CodePointIndex;
                    options.SelectionEnd -= p.CodePointIndex;
                }

                // Paint the paragraph
                p.Paint(canvas, options);

                // Revert selection range
                if (options != null)
                {
                    options.SelectionStart += p.CodePointIndex;
                    options.SelectionEnd += p.CodePointIndex;
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
        /// <returns>A CaretInfo struct, or CaretInfo.None</returns>
        public CaretInfo GetCaretInfo(int codePointIndex)
        {
            // Make sure layout up to date
            Layout();

            // Find the paragraph
            var paraIndex = GetParagraphForCodePointIndex(codePointIndex, out var indexInParagraph);
            var para = _paragraphs[paraIndex];

            // Get caret info
            var ci = para.GetCaretInfo(indexInParagraph);

            // Adjust caret info to be relative to document
            ci.CodePointIndex += para.CodePointIndex;
            ci.CaretXCoord += para.ContentXCoord;
            ci.CaretRectangle.Offset(new SKPoint(para.ContentXCoord, para.ContentYCoord));

            // Done
            return ci;
        }

        /// <summary>
        /// Given a code point index relative to the document, return which
        /// paragraph contains that code point and the offset within the paragraph
        /// </summary>
        /// <param name="codePointIndex"></param>
        /// <param name="indexInParagraph"></param>
        /// <returns>The index of the paragraph</returns>
        int GetParagraphForCodePointIndex(int codePointIndex, out int indexInParagraph)
        {
            // Ensure layout is valid
            Layout();

            // Search paragraphs
            int paraIndex = _paragraphs.BinarySearch(codePointIndex, (para, a) => 
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
            indexInParagraph = codePointIndex - _paragraphs[paraIndex].CodePointIndex;

            // Clamp to end of paragraph
            if (indexInParagraph >= _paragraphs[paraIndex].Length)
                indexInParagraph = _paragraphs[paraIndex].Length - 1;

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
