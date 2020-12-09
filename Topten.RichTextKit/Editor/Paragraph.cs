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
using System.Collections.Generic;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Editor
{
    /// <summary>
    /// Abstract base class for all TextDocument paragraphs
    /// </summary>
    public abstract class Paragraph : IRun
    {
        /// <summary>
        /// Constructs a new Paragraph
        /// </summary>
        protected Paragraph()
        {
        }

        /// <summary>
        /// Layout the content of this paragraph
        /// </summary>
        /// <param name="owner">The TextDocument that owns this paragraph</param>
        public abstract void Layout(TextDocument owner);

        /// <summary>
        /// Paint this paragraph
        /// </summary>
        /// <param name="canvas">The canvas to paint to</param>
        /// <param name="options">Paint options</param>
        public abstract void Paint(SKCanvas canvas, TextPaintOptions options);

        /// <summary>
        /// Get caret position information
        /// </summary>
        /// <remarks>
        /// The returned caret info should be relative to the paragraph's content
        /// </remarks>
        /// <param name="position">The caret position</param>
        /// <returns>A CaretInfo struct, or CaretInfo.None</returns>
        public abstract CaretInfo GetCaretInfo(CaretPosition position);

        /// <summary>
        /// Hit test this paragraph
        /// </summary>
        /// <param name="x">The x-coordinate relative to top left of the paragraph content</param>
        /// <param name="y">The x-coordinate relative to top left of the paragraph content</param>
        /// <returns>A HitTestResult</returns>
        public abstract HitTestResult HitTest(float x, float y);

        /// <summary>
        /// Hit test a line in this paragraph
        /// </summary>
        /// <remarks>
        /// The number of lines can be determined from LineIndicies.Count.
        /// </remarks>
        /// <param name="lineIndex">The line number to be tested</param>
        /// <param name="x">The x-coordinate relative to left of the paragraph content</param>
        /// <returns>A HitTestResult</returns>
        public abstract HitTestResult HitTestLine(int lineIndex, float x);

        /// <summary>
        /// Retrieves a list of all valid caret positions
        /// </summary>
        public abstract IReadOnlyList<int> CaretIndicies { get; }

        /// <summary>
        /// Retrieves a list of all valid word boundary caret positions
        /// </summary>
        public abstract IReadOnlyList<int> WordBoundaryIndicies { get; }

        /// <summary>
        /// Retrieves a list of code point indicies of the start of each line
        /// </summary>
        public abstract IReadOnlyList<int> LineIndicies { get; }

        /// <summary>
        /// Gets the length of this paragraph in code points
        /// </summary>
        /// <remarks>
        /// All paragraphs must have a non-zero length and text paragraphs
        /// should include the end of paragraph marker in the length.
        /// </remarks>
        public abstract int Length { get; }

        /// <summary>
        /// Qureries the height of this paragraph, excluding margins
        /// </summary>
        public abstract float ContentHeight { get; }

        /// <summary>
        /// Queries the width of this paragraph, excluding margins
        /// </summary>
        public abstract float ContentWidth { get; }

        /// <summary>
        /// Gets the TextBlock associated with this paragraph
        /// </summary>
        /// <remarks>
        /// Non-text paragraphs should return null
        /// </remarks>
        public virtual TextBlock TextBlock { get => null; }

        /// <summary>
        /// Copy all style attributes from this paragraph to another
        /// </summary>
        /// <param name="other">The paragraph to copy style from</param>
        public virtual void CopyStyleFrom(Paragraph other)
        {
            this.MarginLeft = other.MarginLeft;
            this.MarginTop = other.MarginTop;
            this.MarginRight = other.MarginRight;
            this.MarginBottom = other.MarginBottom;
        }

        /// <summary>
        /// The X-coordinate of this paragraph's content (ie: after applying margin)
        /// </summary>
        /// <remarks>
        /// This property is calculated and assigned by the TextDocument
        /// </remarks>
        public float ContentXCoord { get; internal set; }

        /// <summary>
        /// The Y-coordinate of this paragraph's content (ie: after applying margin)
        /// </summary>
        /// <remarks>
        /// This property is calculated and assigned by the TextDocument
        /// </remarks>
        public float ContentYCoord { get; internal set; }

        /// <summary>
        /// This code point index of this paragraph relative to the start
        /// of the document.
        /// </summary>
        /// <remarks>
        /// This property is calculated and assigned by the TextDocument
        /// </remarks>
        public int CodePointIndex { get; internal set; }

        /// <summary>
        /// The left margin
        /// </summary>
        public float MarginLeft { get; internal set; }

        /// <summary>
        /// The right margin
        /// </summary>
        public float MarginRight { get; internal set; }

        /// <summary>
        /// The top margin
        /// </summary>
        public float MarginTop { get; internal set; }

        /// <summary>
        /// The bottom margin
        /// </summary>
        public float MarginBottom { get; internal set; }

        // Explicit implementation of IRun so we can use RunExtensions
        // with the paragraphs collection.
        int IRun.Offset => CodePointIndex;
        int IRun.Length => Length;
    }
}
