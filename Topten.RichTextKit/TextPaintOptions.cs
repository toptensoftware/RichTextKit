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

namespace Topten.RichTextKit
{
    /// <summary>
    /// Options to control how TextBlock is rendered.
    /// </summary>
    public class TextPaintOptions
    {
        /// <summary>
        /// Constructs a new text paint options
        /// </summary>
        public TextPaintOptions()
        { 
        }


        /// <summary>
        /// Creates a clone of this object
        /// </summary>
        /// <returns>The closed object</returns>
        public TextPaintOptions Clone()
        {
            return (TextPaintOptions)this.MemberwiseClone();
        }

        /// <summary>
        /// An optional TextRange to painted as selected.
        /// </summary>
        public TextRange? Selection
        {
            get;
            set;
        }

        /// <summary>
        /// The color to be used for the selection background.
        /// </summary>
        public SKColor SelectionColor
        {
            get;
            set;
        }

        /// <summary>
        /// Controls whether text is rendered with anti-aliasing.
        /// </summary>
        public bool IsAntialias
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Controls whether text is rendered using LCD sub-pixel rendering.
        /// </summary>
        public bool LcdRenderText
        {
            get;
            set;
        } = false;

        /// <summary>
        /// A default set of paint options that renders text blocks without 
        /// a selection range.
        /// </summary>
        public static TextPaintOptions Default = new TextPaintOptions();
    }
}
