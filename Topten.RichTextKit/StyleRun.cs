using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Represets a styled run of text as provided by the client
    /// </summary>
    public class StyleRun
    {
        /// <summary>
        /// The owning text block
        /// </summary>
        public TextBlock TextBlock
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the code points of this run
        /// </summary>
        public Slice<int> CodePoints => CodePointBuffer.SubSlice(Start, Length);

        /// <summary>
        /// Index into _codePoints buffer of the start of this run
        /// </summary>
        public int Start
        {
            get;
            internal set;
        }

        /// <summary>
        /// The length of this run (in codepoints)
        /// </summary>
        public int Length
        {
            get;
            internal set;
        }

        /// <summary>
        /// The index of the first code point after this run
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// The style of this run
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
    }
}
