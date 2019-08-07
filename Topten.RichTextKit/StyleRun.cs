using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Represets a style run - a logical run of characters all with the same
    /// style.
    /// </summary>
    public class StyleRun
    {
        /// <summary>
        /// Gets the text block that owns this run.
        /// </summary>
        public TextBlock TextBlock
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the code points of this run.
        /// </summary>
        public Slice<int> CodePoints => CodePointBuffer.SubSlice(Start, Length);

        /// <summary>
        /// The index of the first code point in this run (relative to the text block
        /// as a whole).
        /// </summary>
        public int Start
        {
            get;
            internal set;
        }

        /// <summary>
        /// The number of code points this run.
        /// </summary>
        public int Length
        {
            get;
            internal set;
        }

        /// <summary>
        /// The index of the first code point after this run.
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// The style attributes to be applied to text in this run.
        /// </summary>
        public IStyle Style
        {
            get;
            internal set;
        }

        /// <summary>
        /// The global list of code points
        /// </summary>
        internal Buffer<int> CodePointBuffer;

        [ThreadStatic]
        internal static ObjectPool<StyleRun> Pool = new ObjectPool<StyleRun>()
        {
            Cleaner = (r) =>
            {
                r.CodePointBuffer = null;
                r.Style = null;
                r.TextBlock = null;
            }
        };
    }
}
