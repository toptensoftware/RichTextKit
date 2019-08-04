namespace Topten.RichTextKit
{
    /// <summary>
    /// Species the alignment of text within a text block
    /// </summary>
    public enum TextAlignment
    {
        /// <summary>
        /// Use base direction of the text block.
        /// </summary>
        Auto,

        /// <summary>
        /// Left-aligns text to a x-coord of 0.
        /// </summary>
        Left,

        /// <summary>
        /// Center aligns text between 0 and <see cref="TextBlock.MaxWidth"/> unless not
        /// specified in which case it uses the widest line in the text block.
        /// </summary>
        Center,

        /// <summary>
        /// Right aligns text <see cref="TextBlock.MaxWidth"/> unless not
        /// specified in which case it right aligns to the widest line in the text block.
        /// </summary>
        Right,
    }
}
