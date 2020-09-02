using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Provides a mechanism to override the default font fallback character matching
    /// </summary>
    /// <remarks>
    /// To override font fallback selection, assign an implementation of this interface
    /// to the <see cref="FontFallback.CharacterMatcher"/> property.
    /// </remarks>
    public interface ICharacterMatcher
    {
        /// <summary>
        /// Provide a fallback typeface for a specified code point index
        /// </summary>
        /// <param name="familyName">The family name to use when searching.</param>
        /// <param name="weight">The font weight to use when searching.</param>
        /// <param name="width">The font width to use when searching.</param>
        /// <param name="slant">The font slant to use when searching.</param>
        /// <param name="bcp47">The ISO 639, 15924, and 3166-1 code to use when searching, such as "ja" and "zh".</param>
        /// <param name="character">The character to find a typeface for.</param>
        /// <returns>Returns the SkiaSharp.SKTypeface that contains the given character, or null if none was found.</returns>
        SKTypeface MatchCharacter(string familyName, int weight, int width, SKFontStyleSlant slant, string[] bcp47, int character);
    }
}
