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
using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Used to return caret positioning information from the 
    /// <see cref="TextBlock.GetCaretInfo(CaretPosition)"/> method.
    /// </summary>
    public struct CaretInfo
    {
        /// <summary>
        /// Returns the index of the code point that this caret info refers to.
        /// </summary>
        public int CodePointIndex;

        /// <summary>
        /// Returns the line number that contains the caret
        /// </summary>
        public int LineIndex;

        /// <summary>
        /// The X-coordinate where the caret should be displayed for this code point.
        /// </summary>
        public float CaretXCoord;

        /// <summary>
        /// A rectangle describing where the caret should be drawn, relative to the top-left
        /// corner of the text block. The caret should be drawn from the returned rectangle's
        /// top-right to bottom-left.
        /// </summary>
        /// <remarks>
        /// This will be based on the *previous* character on this line (or the same character 
        /// if this is first character in the line). 
        /// 
        /// Usually this will be a zero-width rectangle describing the x, top and bottom 
        /// coordinates of where the caret should be drawn.  The width of the drawn caret
        /// isn't provided and should be determined by the client.
        /// 
        /// When the caret is immediately following an italic character, the returned
        /// rectangle will be sloped to the right and should be drawn from the top-right
        /// coordinate to the bottom-left coordinate.  
        /// 
        /// If you don't want to draw a sloped caret for italics, use the top and bottom 
        /// coordinates of the returned rectangle and get the x-coordinate from the 
        /// <see cref="CaretXCoord"/> property.
        /// </remarks>
        public SKRect CaretRectangle;

        /// <summary>
        /// Checks if this caret info represents a caret position of none, or not found
        /// </summary>
        public bool IsNone => CodePointIndex < 0;

        /// <summary>
        /// Place holder caret info structure for no caret
        /// </summary>
        public static CaretInfo None = new CaretInfo()
        {
            CodePointIndex = -1,
        };


        /*
         * Commented out as untested.
         * 
         * 
        /// <summary>
        /// The base line of this cluster, relative to the top of the text block
        /// </summary>
        public float ClusterBaseLine => FontRun.Line.YPosition + FontRun.Line.BaseLine;

        /// <summary>
        /// The cluster's ascent
        /// </summary>
        public float ClusterAscent => FontRun.Ascent;

        /// <summary>
        /// The cluster's descent
        /// </summary>
        public float ClusterDescent => FontRun.Descent;

        /// <summary>
        /// Get the left x-coord of this cluster
        /// </summary>
        public float ClusterLeftXCoord
        {
            get
            {
                return FontRun.GetCodePointXCoord(Direction == TextDirection.LTR ? CodePointIndex : NextCodePointIndex);
            }
        }

        /// <summary>
        /// Get the right x-coord of this cluster
        /// </summary>
        public float ClusterRightXCoord
        {
            get
            {
                return FontRun.GetCodePointXCoord(Direction == TextDirection.RTL ? CodePointIndex : NextCodePointIndex);
            }
        }
        /// <summary>
        /// The code point index of the next cluster.
        /// </summary>
        /// <remarks>
        /// If the code point index refers to the last code point in the
        /// text block then this property returns the current code point index.
        /// </remarks>
        public int NextCodePointIndex;

        /// <summary>
        /// The code point index of the previous cluster.
        /// </summary>
        /// <remarks>
        /// If the code point index refers to the first code point in the
        /// text block then this property returns 0.
        /// </remarks>
        public int PreviousCodePointIndex;

        /// <summary>
        /// The number of code points in this cluster.
        /// </summary>
        public int CodePointCount => NextCodePointIndex - CodePointIndex;

        /// <summary>
        /// The font run that contains the code point.
        /// </summary>
        public FontRun FontRun;

        /// <summary>
        /// The style run that contains the code point.
        /// </summary>
        public StyleRun StyleRun;

        */
    }
}
