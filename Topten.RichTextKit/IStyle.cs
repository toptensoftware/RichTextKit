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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Provides styling information for a run of text.
    /// </summary>
    public interface IStyle
    {
        /// <summary>
        /// The font family for text this text run.
        /// </summary>
        string FontFamily { get; }

        /// <summary>
        /// The font size for text in this run.
        /// </summary>
        float FontSize { get; }

        /// <summary>
        /// The font weight for text in this run.
        /// </summary>
        int FontWeight { get; }

        /// <summary>
        /// The font weight for text in this run.
        /// </summary>
        SKFontStyleWidth FontWidth { get; }

        /// <summary>
        /// True if the text in this run should be displayed in an italic
        /// font; otherwise False.
        /// </summary>
        bool FontItalic { get; }

        /// <summary>
        /// The underline style for text in this run.
        /// </summary>
        UnderlineStyle Underline { get; }

        /// <summary>
        /// The strike through style for the text in this run
        /// </summary>
        StrikeThroughStyle StrikeThrough { get; }

        /// <summary>
        /// The line height for text in this run as a multiplier (defaults to 1)
        /// </summary>
        float LineHeight { get; }

        /// <summary>
        /// The text color for text in this run.
        /// </summary>
        SKColor TextColor { get; }
        
        /// <summary>
        /// The background color of this run.
        /// </summary>
        SKColor BackgroundColor { get; }

        /// <summary>
        /// Color of the halo
        /// </summary>
        SKColor HaloColor { get; }

        /// <summary>
        /// Width of halo
        /// </summary>
        float HaloWidth { get; }

        /// <summary>
        /// Blur of halo
        /// </summary>
        float HaloBlur { get; }

        /// <summary>
        /// Extra spacing between each character
        /// </summary>
        float LetterSpacing { get; }

        /// <summary>
        /// The font variant (ie: super/sub-script) for text in this run.
        /// </summary>
        FontVariant FontVariant { get; }

        /// <summary>
        /// Text direction override for this span
        /// </summary>
        TextDirection TextDirection { get; }

        /// <summary>
        /// Specifies a replacement character to be displayed (password mode)
        /// </summary>
        char ReplacementCharacter { get; }
    }
}
