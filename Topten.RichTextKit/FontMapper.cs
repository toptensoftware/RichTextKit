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
        /// The default font mapper instance.  Can be replaced to change the default font mapping algorithm
        /// </summary>
        public static FontMapper Default = new FontMapper();
    }
}
