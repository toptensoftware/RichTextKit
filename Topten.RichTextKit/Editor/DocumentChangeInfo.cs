using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit.Editor
{
    /// <summary>
    /// Provides information about the range of changes to a document
    /// </summary>
    public struct DocumentChangeInfo
    {
        /// <summary>
        /// The index of the code point index at which the change was made
        /// </summary>
        public int CodePointIndex;

        /// <summary>
        /// Length of the text that was replaced
        /// </summary>
        public int OldLength;

        /// <summary>
        /// Length of the replacement text
        /// </summary>
        public int NewLength;

        /// <summary>
        /// True if the current edit operation is the result of an
        /// undo operation.
        /// </summary>
        public bool IsUndoing;

        /// <summary>
        /// Semantics of the edit operation
        /// </summary>
        public EditSemantics Semantics;

        /// <summary>
        /// Offset of the IME caret from the code point index
        /// </summary>
        public int ImeCaretOffset;

    }
}
