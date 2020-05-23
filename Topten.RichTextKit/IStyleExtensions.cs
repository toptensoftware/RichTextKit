namespace Topten.RichTextKit
{
    /// <summary>
    /// Extension methods for working with IStyle
    /// </summary>
    public static class IStyleExtensions
    {
        /// <summary>
        /// Generates a string key that uniquely identifies the formatting characteristics
        /// of this style.
        /// </summary>
        /// <remarks>
        /// Two styles with the same Key will rendering identically even if different instances.
        /// </remarks>
        /// <param name="This">The style instance to generate the key for</param>
        /// <returns>A key string</returns>
        public static string Key(this IStyle This)
        {
            return $"{This.FontFamily}.{This.FontSize}.{This.FontWeight}.{This.FontItalic}.{This.Underline}.{This.StrikeThrough}.{This.LineHeight}.{This.TextColor}.{This.FontVariant}.{This.TextDirection}";        
        }

        /// <summary>
        /// Compares this style to another and returns true if both will have the same
        /// layout, but not necessarily the same appearance (eg: color change, underline etc...)
        /// </summary>
        /// <param name="This">The style instance</param>
        /// <param name="other">The other style instance to compare to</param>
        /// <returns>True if both styles will give the same layout</returns>
        public static bool HasSameLayout(this IStyle This, IStyle other)
        {
            if (This.FontFamily != other.FontFamily)
                return false;
            if (This.FontSize != other.FontSize)
                return false;
            if (This.FontWeight != other.FontWeight)
                return false;
            if (This.FontVariant != other.FontVariant)
                return false;
            if (This.FontItalic != other.FontItalic)
                return false;
            if (This.LineHeight != other.LineHeight)
                return false;
            if (This.TextDirection != other.TextDirection)
                return false;
            return true;
        }

    }
}
