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
using System.Collections.Generic;
using System.Threading;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Helper class for managing RichTextKit styles.
    /// </summary>
    /// <remarks>
    /// The StyleManager can be used to simplify the creation of styles by
    /// maintaining a current style that incremental changes can be made to.
    ///
    /// eg: turn bold on, underline off, change font family etc...
    /// 
    /// The StyleManager also implements an internal stack to simplify applying
    /// a particular style and then popping back to the previous style.
    /// </remarks>
    public class StyleManager
    {
        /// <summary>
        /// A per-thread style manager
        /// </summary>
        public static ThreadLocal<StyleManager> Default = new ThreadLocal<StyleManager>(() => new StyleManager());

        /// <summary>
        /// Constructs a new StyleManager
        /// </summary>
        public StyleManager()
        {
            _currentStyle = FromStyle(new Style());
            _defaultStyle = _currentStyle;
        }

        /// <summary>
        /// The current style
        /// </summary>
        public IStyle CurrentStyle
        {
            get => _currentStyle;
            set => _currentStyle = FromStyle(value);
        }

        /// <summary>
        /// The default style to be be used when Reset is called
        /// </summary>
        public IStyle DefaultStyle
        {
            get => _defaultStyle;
            set => _defaultStyle = FromStyle(value);
        }

        /// <summary>
        /// Get a style that matches all the style attributes of the supplied style
        /// </summary>
        /// <remarks>
        /// This method creates a style owned by this style manager with all the same 
        /// attributes as the passed style.
        /// </remarks>
        /// <param name="value">The style to copy</param>
        /// <returns></returns>
        public IStyle FromStyle(IStyle value)
        {
            // Is it a style we already own? Just re-use it.
            if (IsOwned(value))
                return value;

            return Update(value.FontFamily, value.FontSize, value.FontWeight, value.FontWidth, value.FontItalic,
                            value.Underline, value.StrikeThrough, value.LineHeight, value.TextColor, value.BackgroundColor,
                            value.HaloColor, value.HaloWidth, value.HaloBlur,
                            value.LetterSpacing, value.FontVariant, value.TextDirection, value.ReplacementCharacter);
        }

        /// <summary>
        /// Resets the current style to the default style and resets the internal
        /// Push/Pop style stack to empty.
        /// </summary>
        public void Reset()
        {
            _currentStyle = _defaultStyle;
            _userStack.Clear();
        }

        /// <summary>
        /// Saves the current state on an internal stack 
        /// </summary>
        public void Push()
        {
            _userStack.Push(_currentStyle);
        }

        /// <summary>
        /// Restores the current state on an internal stack 
        /// </summary>
        public void Pop()
        {
            _currentStyle = _userStack.Pop();
        }

        /// <summary>
        /// Changes the font family and returns an updated IStyle
        /// </summary>
        /// <param name="fontFamily">The new font family</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle FontFamily(string fontFamily) => Update(fontFamily: fontFamily);

        /// <summary>
        /// Changes the font size and returns an updated IStyle
        /// </summary>
        /// <param name="fontSize">The new font size</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle FontSize(float fontSize) => Update(fontSize: fontSize);

        /// <summary>
        /// Changes the font weight and returns an updated IStyle
        /// </summary>
        /// <param name="fontWeight">The new font weight</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle FontWeight(int fontWeight) => Update(fontWeight: fontWeight);

        /// <summary>
        /// Changes the font weight and returns an update IStyle (short cut to FontWeight)
        /// </summary>
        /// <param name="bold">The new font weight</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle Bold(bool bold) => Update(fontWeight: bold ? 700 : 400);

        /// <summary>
        /// Changes the font width and returns an updated IStyle
        /// </summary>
        /// <param name="fontWidth">The new font width</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle FontWidth(SKFontStyleWidth fontWidth) => Update(fontWidth: fontWidth);

        /// <summary>
        /// Changes the font italic setting and returns an updated IStyle
        /// </summary>
        /// <param name="fontItalic">The new font italic setting</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle FontItalic(bool fontItalic) => Update(fontItalic: fontItalic);

        /// <summary>
        /// Changes the underline style and returns an updated IStyle
        /// </summary>
        /// <param name="underline">The new underline style</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle Underline(UnderlineStyle underline) => Update(underline: underline);

        /// <summary>
        /// Changes the strikethrough style and returns an updated IStyle
        /// </summary>
        /// <param name="strikeThrough">The new strikethrough style</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle StrikeThrough(StrikeThroughStyle strikeThrough) => Update(strikeThrough: strikeThrough);

        /// <summary>
        /// Changes the line height and returns an updated IStyle
        /// </summary>
        /// <param name="lineHeight">The new line height</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle LineHeight(float lineHeight) => Update(lineHeight: lineHeight);

        /// <summary>
        /// Changes the text color and returns an updated IStyle
        /// </summary>
        /// <param name="textColor">The new text color</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle TextColor(SKColor textColor) => Update(textColor: textColor);

        /// <summary>
        /// Changes the background color and returns an updated IStyle
        /// </summary>
        /// <param name="backgroundColor">The new background color</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle BackgroundColor(SKColor backgroundColor) => Update(backgroundColor: backgroundColor);

        /// <summary>
        /// Changes the halo color and returns an updated IStyle
        /// </summary>
        /// <param name="haloColor">The new halo color</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle HaloColor(SKColor haloColor) => Update(haloColor: haloColor);

        /// <summary>
        /// Changes the halo width and returns an updated IStyle
        /// </summary>
        /// <param name="haloWidth">The new halo width</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle HaloWidth(float haloWidth) => Update(haloWidth: haloWidth);

        /// <summary>
        /// Changes the halo blur width and returns an updated IStyle
        /// </summary>
        /// <param name="haloBlur">The new halo blur width</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle HaloBlur(float haloBlur) => Update(haloBlur: haloBlur);

        /// <summary>
        /// Changes the character spacing and returns an updated IStyle
        /// </summary>
        /// <param name="letterSpacing">The new character spacing</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle LetterSpacing(float letterSpacing) => Update(letterSpacing: letterSpacing);

        /// <summary>
        /// Changes the font variant and returns an updated IStyle
        /// </summary>
        /// <param name="fontVariant">The new font variant</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle FontVariant(FontVariant fontVariant) => Update(fontVariant: fontVariant);

        /// <summary>
        /// Changes the text direction and returns an updated IStyle
        /// </summary>
        /// <param name="textDirection">The new text direction</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle TextDirection(TextDirection textDirection) => Update(textDirection: textDirection);

        /// <summary>
        /// Changes the text direction and returns an updated IStyle
        /// </summary>
        /// <param name="character">The new replacement character</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle ReplacementCharacter(char character) => Update(replacementCharacter: character);


        /// <summary>
        /// Update the current style by applying one or more changes to the current
        /// style.
        /// </summary>
        /// <param name="fontFamily">The new font family</param>
        /// <param name="fontSize">The new font size</param>
        /// <param name="fontWeight">The new font weight</param>
        /// <param name="fontWidth">The new font width</param>
        /// <param name="fontItalic">The new font italic</param>
        /// <param name="underline">The new underline style</param>
        /// <param name="strikeThrough">The new strike-through style</param>
        /// <param name="lineHeight">The new line height</param>
        /// <param name="textColor">The new text color</param>
        /// <param name="backgroundColor">The new text color</param>
        /// <param name="haloColor">The new text color</param>
        /// <param name="haloWidth">The new halo width</param>
        /// <param name="haloBlur">The new halo blur width</param>
        /// <param name="letterSpacing">The new letterSpacing</param>
        /// <param name="fontVariant">The new font variant</param>
        /// <param name="textDirection">The new text direction</param>
        /// <param name="replacementCharacter">The new replacement character</param>
        /// <returns>An IStyle for the new style</returns>
        public IStyle Update(
               string fontFamily = null,
               float? fontSize = null,
               int? fontWeight = null,
               SKFontStyleWidth? fontWidth = null,
               bool? fontItalic = null,
               UnderlineStyle? underline = null,
               StrikeThroughStyle? strikeThrough = null,
               float? lineHeight = null,
               SKColor? textColor = null,
               SKColor? backgroundColor = null,
               SKColor? haloColor = null,
               float? haloWidth = null,
               float? haloBlur = null,
               float? letterSpacing = null,
               FontVariant? fontVariant = null,
               TextDirection? textDirection = null,
               char? replacementCharacter = null
            )
        {
            // Resolve new style against current style
            var rFontFamily = fontFamily ?? _currentStyle.FontFamily;
            var rFontSize = fontSize ?? _currentStyle.FontSize;
            var rFontWeight = fontWeight ?? _currentStyle.FontWeight;
            var rFontWidth = fontWidth ?? _currentStyle.FontWidth;
            var rFontItalic = fontItalic ?? _currentStyle.FontItalic;
            var rUnderline = underline ?? _currentStyle.Underline;
            var rStrikeThrough = strikeThrough ?? _currentStyle.StrikeThrough;
            var rLineHeight = lineHeight ?? _currentStyle.LineHeight;
            var rTextColor = textColor ?? _currentStyle.TextColor;
            var rBackgroundColor = backgroundColor ?? _currentStyle.BackgroundColor;
            var rHaloColor = haloColor ?? _currentStyle.HaloColor;
            var rHaloWidth = haloWidth ?? _currentStyle.HaloWidth;
            var rHaloBlur = haloBlur ?? _currentStyle.HaloBlur;
            var rLetterSpacing = letterSpacing ?? _currentStyle.LetterSpacing;
            var rFontVariant = fontVariant ?? _currentStyle.FontVariant;
            var rTextDirection = textDirection ?? _currentStyle.TextDirection;
            var rReplacementCharacter = replacementCharacter ?? _currentStyle.ReplacementCharacter;

            // Format key
            var key = $"{rFontFamily}.{rFontSize}.{rFontWeight}.{fontWidth}.{rFontItalic}.{rUnderline}.{rStrikeThrough}.{rLineHeight}.{rTextColor}.{rBackgroundColor}.{rHaloColor}.{rHaloWidth}.{rHaloBlur}.{rLetterSpacing}.{rFontVariant}.{rTextDirection}.{rReplacementCharacter}";

            // Look up...
            if (!_styleMap.TryGetValue(key, out var style))
            {
                // Create a new style
                style = new StyleManagerStyle()
                {
                    Owner = this,
                    FontFamily = rFontFamily,
                    FontSize = rFontSize,
                    FontWeight = rFontWeight,
                    FontWidth = rFontWidth,
                    FontItalic = rFontItalic,
                    Underline = rUnderline,
                    StrikeThrough = rStrikeThrough,
                    LineHeight = rLineHeight,
                    TextColor = rTextColor,
                    BackgroundColor = rBackgroundColor,
                    HaloColor = rHaloColor,
                    HaloWidth = rHaloWidth,
                    HaloBlur = rHaloBlur,
                    LetterSpacing = rLetterSpacing,
                    FontVariant = rFontVariant,
                    TextDirection = rTextDirection,
                    ReplacementCharacter = rReplacementCharacter,
                };

                // Seal it
                style.Seal();

                // Add to map
                _styleMap.Add(key, style);
            }

            // Set the new current style and return it
            return _currentStyle = style;
        }

        // Check is a style is owned by this style manager
        bool IsOwned(IStyle style)
        {
            return style is StyleManagerStyle sms && sms.Owner == this;
        }

        /// <summary>
        /// Internal wrapper around Style to attach our owner reference check
        /// </summary>
        class StyleManagerStyle : Style
        {
            public StyleManager Owner;
        }

        Dictionary<string, Style> _styleMap = new Dictionary<string, Style>();
        Stack<IStyle> _userStack = new Stack<IStyle>();
        IStyle _defaultStyle = new Style();
        IStyle _currentStyle;
    }
}
