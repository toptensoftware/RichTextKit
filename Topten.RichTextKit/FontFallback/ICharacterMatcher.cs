using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Provides a mechanism to override the default font fallback character matching
    /// </summary>
    public interface ICharacterMatcer
    {
        //
        // Summary:
        //     Use the system fallback to find a typeface for the given character.
        //
        // Parameters:
        //   familyName:
        //     The family name to use when searching.
        //
        //   weight:
        //     The font weight to use when searching.
        //
        //   width:
        //     The font width to use when searching.
        //
        //   slant:
        //     The font slant to use when searching.
        //
        //   bcp47:
        //     The ISO 639, 15924, and 3166-1 code to use when searching, such as "ja" and "zh".
        //
        //   character:
        //     The character to find a typeface for.
        //
        // Returns:
        //     Returns the SkiaSharp.SKTypeface that contains the given character, or null if
        //     none was found.
        SKTypeface MatchCharacter(string familyName, int weight, int width, SKFontStyleSlant slant, string[] bcp47, int character);
    }
}
