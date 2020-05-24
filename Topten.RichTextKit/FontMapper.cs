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
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// The FontMapper class is responsible for mapping style typeface information
    /// to an SKTypeface.
    /// </summary>
    public class FontMapper
    {
        /// <summary>
        /// Constructs a new FontMapper instnace
        /// </summary>
        public FontMapper()
        {
        }

        /// <summary>
        /// Maps a given style to a specific typeface
        /// </summary>
        /// <param name="style">The style to be mapped</param>
        /// <param name="ignoreFontVariants">Indicates the mapping should ignore font variants (use to get font for ellipsis)</param>
        /// <returns>A mapped typeface</returns>
        public virtual SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants)
        {
            // Extra weight for superscript/subscript
            int extraWeight = 0;
            if (!ignoreFontVariants && (style.FontVariant == FontVariant.SuperScript || style.FontVariant == FontVariant.SubScript))
            {
                extraWeight += 100;
            }

            // Get the typeface
            return SKTypeface.FromFamilyName(
                style.FontFamily, 
                (SKFontStyleWeight)(style.FontWeight + extraWeight), 
                0, 
                style.FontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
                ) ?? SKTypeface.CreateDefault();
        }

        /// <summary>
        /// The default font mapper instance.  
        /// </summary>
        /// <remarks>
        /// The default font mapper is used by any TextBlocks that don't 
        /// have an explicit font mapper set (see the <see cref="TextBlock.FontMapper"/> property).
        /// 
        /// Replacing the default font mapper allows changing the font mapping
        /// for all text blocks that don't have an explicit mapper assigned.
        /// </remarks>
        public static FontMapper Default = new FontMapper();
    }
}
