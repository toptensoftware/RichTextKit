using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Describes the results of hit testing a <see cref="TextBlock"/>
    /// </summary>
    public struct HitTestResult
    {
        /// <summary>
        /// The zero based index of the line number the y-coordinate is directly 
        /// over, or -1 if the y-coordinate is above the first line, or below the 
        /// last line.
        /// </summary>
        /// <remarks>
        /// The x-coordinate isn't used in calculating which line the point is over
        /// and the left/right limits aren't checked.
        /// </remarks>
        public int OverLine;

        /// <summary>
        /// The closest line to the passed y-coordinate.  
        /// </summary>
        /// <remarks>
        /// If the point is directly over
        /// a line this value will be the same as the `OverLine` property.  If the point is 
        /// above the first line, this value will be 0.  If the point is below the last 
        /// line this value will be the index of the last line. 
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
