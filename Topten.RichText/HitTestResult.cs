using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichText
{
    /// <summary>
    /// Describes the results of hit testing a <see cref="TextBlock"/>
    /// </summary>
    public struct HitTestResult
    {
        /// <summary>
        /// The line number of the hit test, or -1 if not on any line
        /// </summary>
        public int OverLine;

        /// <summary>
        /// The closest line of the hit test
        /// </summary>
        public int ClosestLine;

        /// <summary>
        /// The code point index of the cluster the point is over, or -1 if not over
        /// any cluster
        /// </summary>
        public int OverCluster;

        /// <summary>
        /// The code point index where the caret should be placed if the
        /// user was to click at that location
        /// </summary>
        public int ClosestCluster;
    }
}
