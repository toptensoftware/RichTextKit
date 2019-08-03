using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Represents the style of a piece of text
    /// </summary>
    public interface IStyle
    {
        /// <summary>
        /// The font family
        /// </summary>
        string FontFamily { get; }

        /// <summary>
        /// The font size
        /// </summary>
        float FontSize { get; }

        /// <summary>
        /// The font weight
        /// </summary>
        int FontWeight { get; }

        /// <summary>
        /// True for italic font; otherwise false
        /// </summary>
        bool FontItalic { get; }

        /// <summary>
        /// The underline style of the text
        /// </summary>
        UnderlineStyle Underline { get; }

        /// <summary>
        /// The strike through style of the text
        /// </summary>
        StrikeThroughStyle StrikeThrough { get; }

        /// <summary>
        /// The line height as a multiplier
        /// </summary>
        float LineHeight { get; }

        /// <summary>
        /// The text color
        /// </summary>
        SKColor TextColor { get; }

        /// <summary>
        /// The font variant (ie: super/sub-script)
        /// </summary>
        FontVariant FontVariant { get; }
    }
}
