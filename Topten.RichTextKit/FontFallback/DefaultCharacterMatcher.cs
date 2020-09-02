using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    class DefaultCharacterMatcher : ICharacterMatcher
    {
        public DefaultCharacterMatcher()
        {

        }

        SKFontManager _fontManager = SKFontManager.Default;

        /// <inheritdoc />
        public SKTypeface MatchCharacter(string familyName, int weight, int width, SKFontStyleSlant slant, string[] bcp47, int character)
        {
            return _fontManager.MatchCharacter(familyName, weight, width, slant, bcp47, character);
        }
    }
}
