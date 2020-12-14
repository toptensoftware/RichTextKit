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

namespace Topten.RichTextKit
{
    /// <summary>
    /// Extension methods for working with IStyle
    /// </summary>
    public static class IStyleExtensions
    {
        /// <summary>
        /// Generates a string key that uniquely identifies the formatting characteristics
        /// of this style.
        /// </summary>
        /// <remarks>
        /// Two styles with the same Key will rendering identically even if different instances.
        /// </remarks>
        /// <param name="This">The style instance to generate the key for</param>
        /// <returns>A key string</returns>
        public static string Key(this IStyle This)
        {
            return $"{This.FontFamily}.{This.FontSize}.{This.FontWeight}.{This.FontItalic}.{This.Underline}.{This.StrikeThrough}.{This.LineHeight}.{This.TextColor}.{This.BackgroundColor}.{This.LetterSpacing}.{This.FontVariant}.{This.TextDirection}.{This.ReplacementCharacter}";
        }

        /// <summary>
        /// Compares this style to another and returns true if both will have the same
        /// layout, but not necessarily the same appearance (eg: color change, underline etc...)
        /// </summary>
        /// <param name="This">The style instance</param>
        /// <param name="other">The other style instance to compare to</param>
        /// <returns>True if both styles will give the same layout</returns>
        public static bool HasSameLayout(this IStyle This, IStyle other)
        {
            if (This.FontFamily != other.FontFamily)
                return false;
            if (This.FontSize != other.FontSize)
                return false;
            if (This.FontWeight != other.FontWeight)
                return false;
            if (This.FontItalic != other.FontItalic)
                return false;
            if (This.LineHeight != other.LineHeight)
                return false;
            if (This.TextDirection != other.TextDirection)
                return false;
            if (This.LetterSpacing != other.LetterSpacing)
                return false;
            if (This.FontVariant != other.FontVariant)
                return false;
            if (This.ReplacementCharacter != other.ReplacementCharacter)
                return false;
            return true;
        }

        /// <summary>
        /// Compares this style to another and returns true if both will have the same
        /// layout, but not necessarily the same appearance (eg: color change, underline etc...)
        /// </summary>
        /// <param name="This">The style instance</param>
        /// <param name="other">The other style instance to compare to</param>
        /// <returns>True if both styles will give the same layout</returns>
        public static bool IsSame(this IStyle This, IStyle other)
        {
            if (This == null && other == null)
                return true;
            if (This == null || other == null)
                return false;
            if (!This.HasSameLayout(other))
                return false;
            if (This.TextColor != other.TextColor)
                return false;
            if (This.BackgroundColor != other.BackgroundColor)
                return false;
            if (This.Underline != other.Underline)
                return false;
            if (This.StrikeThrough != other.StrikeThrough)
                return false;
            return true;
        }



    }
}
