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

namespace Topten.RichTextKit.Editor
{
    /// <summary>
    /// Represents a range of code points in a text document
    /// </summary>
    public struct TextRange
    {
        /// <summary>
        /// Initializes a TextRange
        /// </summary>
        /// <param name="start">The code point index of the start of the range</param>
        /// <param name="end">The code point index of the end of the range</param>
        /// <param name="altPosition">Whether the caret at the end of the range should be displayed in its alternative position</param>
        public TextRange(int start, int end, bool altPosition = false)
        {
            Start = start;
            End = end;
            AltPosition = altPosition;
        }

        /// <summary>
        /// The code point index of the start of the range
        /// </summary>
        public int Start;

        /// <summary>
        /// The code point index of the end of the range
        /// </summary>
        public int End;

        /// <summary>
        /// True if the end of the range should be displayed
        /// with the caret in the alt position
        /// </summary>
        public bool AltPosition;
    }
}
