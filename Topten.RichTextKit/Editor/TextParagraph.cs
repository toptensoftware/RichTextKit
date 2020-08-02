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

namespace Topten.RichTextKit.Editor
{
    /// <summary>
    /// Implements a text paragraph
    /// </summary>
    class TextParagraph : Paragraph
    {
        /// <summary>
        /// Constructs a new TextParagraph
        /// </summary>
        public TextParagraph(string text)
        {
            // Temporary code for now to setup some content...
            _textBlock.AddText(text, new Style() { FontFamily = "Open Sans", FontSize = 16 });
            MarginTop = 10;
            MarginBottom = 10;
        }

        /// <inheritdoc />
        public override void Layout(TextDocument doc)
        {
            // For layout just need to set the appropriate layout width on the text block
            if (doc.LineWrap)
            {
                _textBlock.MaxWidth =
                        doc.PageWidth
                        - doc.MarginLeft - doc.MarginRight
                        - this.MarginLeft - this.MarginRight;
            }
            else
                _textBlock.MaxWidth = null;
        }

        /// <inheritdoc />
        public override void Paint(SKCanvas canvas, TextPaintOptions options) => _textBlock.Paint(canvas, new SKPoint(ContentXCoord, ContentYCoord), options);
        
        /// <inheritdoc />
        public override CaretInfo GetCaretInfo(int codePointIndex) => _textBlock.GetCaretInfo(codePointIndex);

        /// <inheritdoc />
        public override HitTestResult HitTest(float x, float y) => _textBlock.HitTest(x, y);

        /// <inheritdoc />
        public override float ContentWidth => _textBlock.MeasuredWidth;

        /// <inheritdoc />
        public override float ContentHeight => _textBlock.MeasuredHeight;

        /// <inheritdoc />
        public override int Length => _textBlock.Length;

        // Private attributes
        TextBlock _textBlock = new TextBlock();
    }
}
