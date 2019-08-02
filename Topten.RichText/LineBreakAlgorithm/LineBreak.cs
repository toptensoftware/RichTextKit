// Ported from: https://github.com/foliojs/linebreak

using System.Diagnostics;

namespace Topten.RichText
{
    /// <summary>
    /// Information about a potential line break position
    /// </summary>
    [DebuggerDisplay("{PositionMeasure}/{PositionWrap} @ {Required}")]
    public class LineBreak
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="positionMeasure">The code point index to measure to</param>
        /// <param name="positionWrap">The code point index to actually break the line at</param>
        /// <param name="required">True if this is a required line break; otherwise false</param>
        public LineBreak(int positionMeasure, int positionWrap, bool required = false)
        {
            this.PositionMeasure = positionMeasure;
            this.PositionWrap = positionWrap;
            this.Required = required;
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
        /// This includes trailing whitespace
        /// </remarks>
        public int PositionWrap;

        /// <summary>
        /// True if there should be a forced line break here
        /// </summary>
        public bool Required;
    }
}
