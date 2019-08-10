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
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// A basic implementation of IStyle interface provides styling 
    /// information for a run of text.
    /// </summary>
    public class Style : IStyle
    {
        /// <summary>
        /// The font family for text this text run (defaults to "Arial").
        /// </summary>
        public string FontFamily
        {
            get;
            set;
        } = "Arial";

        /// <summary>
        /// The font size for text in this run (defaults to 16).
        /// </summary>
        public float FontSize
        {
            get;
            set;
        } = 16;

        /// <summary>
        /// The font weight for text in this run (defaults to 400).
        /// </summary>
        public int FontWeight
        {
            get;
            set;
        } = 400;

        /// <summary>
        /// True if the text in this run should be displayed in an italic
        /// font; otherwise False (defaults to false).
        /// </summary>
        public bool FontItalic
        {
            get;
            set;
        }

        /// <summary>
        /// The underline style for text in this run (defaults to None).
        /// </summary>
        public UnderlineStyle Underline
        {
            get;
            set;
        }

        /// <summary>
        /// The strike through style for the text in this run (defaults to None).
        /// </summary>
        public StrikeThroughStyle StrikeThrough
        {
            get;
            set;
        }

        /// <summary>
        /// The line height for text in this run as a multiplier (defaults to 1.0).
        /// </summary>
        public float LineHeight
        {
            get;
            set;
        } = 1.0f;

        /// <summary>
        /// The text color for text in this run (defaults to black).
        /// </summary>
        public SKColor TextColor
        {
            get;
            set;
        } = new SKColor(0xFF000000);

        /// <summary>
        /// The font variant (ie: super/sub-script) for text in this run.
        /// </summary>
        public FontVariant FontVariant
        {
            get;
            set;
        }

        /// <summary>
        /// Text direction override for this span
        /// </summary>
        public TextDirection TextDirection
        {
            get;
            set;
        } = TextDirection.Auto;
    }
}
