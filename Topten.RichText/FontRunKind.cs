namespace Topten.RichText
{
    /// <summary>
    /// Indicates the kind of font run
    /// </summary>
    public enum FontRunKind
    {
        /// <summary>
        /// This is a normal text font run
        /// </summary>
        Normal,

        /// <summary>
        /// This font run covers the trailing white space on a line
        /// </summary>
        TrailingWhitespace,

        /// <summary>
        /// This is a special font run created for the truncation ellipsis
        /// </summary>
        Ellipsis,
    }
}
