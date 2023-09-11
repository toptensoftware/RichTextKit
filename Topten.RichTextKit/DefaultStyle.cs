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
    /// A basic implementation of IStyle interface provides styling 
    /// information for a run of text.
    /// </summary>
    public class DefaultStyle : Style
    {
        public DefaultStyle() 
        {
            _sealed = default(bool);
            _fontFamily = "sans-serif";
            _fontSize = 16;
            _fontWeight = 400;
            _fontWidth = SKFontStyleWidth.Normal;
            _fontItalic = false;
            _underlineStyle = UnderlineStyle.None;
            _strikeThrough = StrikeThroughStyle.None;
            _lineHeight = 1.0f;
            _textColor = new SKColor(0xFF000000);
            _backgroundColor = SKColor.Empty;
            _haloColor = SKColor.Empty;
            _haloWidth = default(float);
            _haloBlur = default(float);
            _letterSpacing = default(float);
            _fontVariant = Topten.RichTextKit.FontVariant.Normal;
            _textDirection = Topten.RichTextKit.TextDirection.Auto;
            _replacementCharacter = '\0';
            
        }
    }
}