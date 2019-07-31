using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    /// <summary>
    /// Represents a laid out line of text
    /// </summary>
    public class TextLine
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TextLine()
        {
        }

        /// <summary>
        /// List of text runs in this line
        /// </summary>
        public List<FontRun> Runs = new List<FontRun>();

        /// <summary>
        /// Position of this line relative to the paragraph
        /// </summary>
        public float YPosition
        {
            get;
            internal set;
        }

        /// <summary>
        /// The base line for text in this line (relative to YPosition)
        /// </summary>
        public float BaseLine
        {
            get;
            internal set;
        }

        /// <summary>
        /// The maximum ascent of all font runs in this line
        /// </summary>
        public float MaxAscent
        {
            get;
            internal set;
        }


        /// <summary>
        /// The maximum desscent of all font runs in this line
        /// </summary>
        public float MaxDescent
        {
            get;
            internal set;
        }

        /// <summary>
        /// The height of all text elements in this line
        /// </summary>
        public float TextHeight => -MaxAscent + MaxDescent;

        /// <summary>
        /// Total height of this line
        /// </summary>
        public float Height
        {
            get;
            internal set;
        }

        /// <summary>
        /// The width of the content on this line (excluding trailing whitespace)
        /// </summary>
        public float Width
        {
            get;
            internal set;
        }

        /// <summary>
        /// Paint this line
        /// </summary>
        /// <param name="canvas"></param>
        internal void Paint(PaintTextContext ctx)
        {
            foreach (var r in Runs)
            {
                r.Paint(ctx);
            }
        }
    }
}
