using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichText
{
    public struct HitTestResult
    {
        /// <summary>
        /// The line number of the hit test, or -1 if not on any line
        /// </summary>
        public int OverLine;

        /// <summary>
        /// The closed line of the hit test
        /// </summary>
        public int ClosestLine;

        /// <summary>
        /// The code point index of the character the point is over
        /// </summary>
        public int OverCharacter;

        /// <summary>
        /// The code point index where the cursor should be placed if the
        /// user was to click at that location
        /// </summary>
        public int ClosestCharacter;
    }
}
