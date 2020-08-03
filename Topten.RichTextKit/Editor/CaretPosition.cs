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

namespace Topten.RichTextKit.Editor
{
    /// <summary>
    /// Stores state information about a caret position
    /// </summary>
    /// <remarks>
    /// The caret position is defined primarily by it's code point
    /// index however there are other attributes that can affect
    /// where it's displayed and how it moves.  This structure
    /// encapsulates all the information about the caret required
    /// to position and move it correctly.
    /// </remarks>
    public struct CaretPosition
    {
        /// <summary>
        /// Initializes a CaretPosition
        /// </summary>
        /// <param name="codePointIndex">The code point index of the caret</param>
        /// <param name="altPosition">Whether the caret should be displayed in its alternative position</param>
        /// <param name="ghostXCoord">The ghost x-coord, or null</param>
        public CaretPosition(int codePointIndex, bool altPosition = false, float? ghostXCoord = null)
        {
            CodePointIndex = codePointIndex;
            AltPosition = altPosition;
            GhostXCoord = ghostXCoord;
        }

        /// <summary>
        /// The code point index of the caret insertion point
        /// </summary>
        public int CodePointIndex;

        /// <summary>
        /// True to display the caret at the end of the previous line
        /// rather than the start of the following line when the code
        /// point index is exactly on a line break.
        /// </summary>
        public bool AltPosition;

        /// <summary>
        /// The X-Coord to maintain when moving vertically.
        /// </summary>
        /// <remarks>
        /// This property will be null after any horizontal movement
        /// or edit operations.  During vertical movement (line up/down and
        /// page up/down) it's used to store the x-coord at the start of
        /// the movement and used to maintain that x-position while continuing
        /// to move vertically.
        /// </remarks>
        public float? GhostXCoord;
    }
}
