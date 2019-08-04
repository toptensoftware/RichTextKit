using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Describes the underline style for a run of text
    /// </summary>
    public enum UnderlineStyle
    {
        /// <summary>
        /// No underline.
        /// </summary>
        None,

        /// <summary>
        /// Underline with gaps over descenders.
        /// </summary>
        Gapped,

        /// <summary>
        /// Underline with no gaps over descenders.
        /// </summary>
        Solid,
    }
}
