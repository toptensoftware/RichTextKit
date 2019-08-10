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
                );
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
