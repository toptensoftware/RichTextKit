// RichTextKit
// Copyright © 2019 Topten Software. All Rights Reserved.
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

using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Used to return hit test information from the
    /// <see cref="TextBlock.HitTest(float, float)"/> method.
    /// </summary>
    public struct HitTestResult
    {
        /// <summary>
        /// The zero based index of the line number the y-coordinate is directly 
        /// over, or -1 if the y-coordinate is before the first line, or after the 
        /// last line.
        /// </summary>
        /// <remarks>
        /// The x-coordinate isn't used in calculating this value and the left/right 
        /// limits aren't checked.
        /// </remarks>
        public int OverLine;

        /// <summary>
        /// The zero based index of the closest line to the passed y-coordinate.  
        /// </summary>
        /// <remarks>
        /// If the point is directly over a line this value will be the same as the 
        /// <see cref="OverLine"/> property.  If the point is before the first line, 
        /// this property will be 0.  If the point is after the last line this value 
        /// will be the index of the last line. 
        /// </remarks>
        public int ClosestLine;

        /// <summary>
        /// The code point index of the first code point in the cluster that the 
        /// point is actually over, or -1 if not over a cluster.
        /// </summary>
        public int OverCodePointIndex;

        /// <summary>
        /// The code point index of the first code point in the cluster that the
        /// point is closest to.
        /// </summary>
        /// <remarks>
        /// If the point is over a cluster, the returned code point index will vary
        /// depending whether the point is in the left or right half of the cluster
        /// and the text direction of that cluster.
        /// 
        /// This value represents the code point index that the caret should be moved to 
        /// if the user clicked the mouse at this position.  To determine the co-ordinates
        /// and shape of the caret, see [Caret Information](/caret).
        /// </remarks>
        public int ClosestCodePointIndex;
    }
}
