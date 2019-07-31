using System;
using System.Collections.Generic;
using System.Text;

// Ported from: https://github.com/foliojs/linebreak

namespace Topten.RichText
{
    public class LineBreak
    {
        public LineBreak()
        {
        }

        public LineBreak(int positionA, int positionB, bool required = false)
        {
            this.PositionMeasure = positionA;
            this.PositionWrap = positionB;
            this.Required = required;
        }

        public override string ToString()
        {
            return $"{PositionMeasure}/{PositionWrap} @ {Required}";
        }


        /// <summary>
        /// The break position, before any trailing whitespace
        /// </summary>
        /// <remarks>
        /// This doesn't include trailing whitespace
        /// </remarks>
        public int PositionMeasure;

        /// <summary>
        /// The break position, after any trailing whitespace
        /// </summary>
        /// <remarks>
        /// This include trailing whitespace
        /// </remarks>
        public int PositionWrap;

        /// <summary>
        /// True if there should be a forced line break here
        /// </summary>
        public bool Required;
    }
}
