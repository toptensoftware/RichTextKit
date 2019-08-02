using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichText
{
    /// <summary>
    /// A standard implementation of IStyle
    /// </summary>
    public class Style : IStyle
    {
        /// <inheritdoc />
        public string FontFamily
        {
            get;
            set;
        } = "Arial";

        /// <inheritdoc />
        public float FontSize
        {
            get;
            set;
        } = 12;

        /// <inheritdoc />
        public int FontWeight
        {
            get;
            set;
        } = 400;

        /// <inheritdoc />
        public bool FontItalic
        {
            get;
            set;
        }

        /// <inheritdoc />
        public UnderlineStyle Underline
        {
            get;
            set;
        }

        /// <inheritdoc />
        public StrikeThroughStyle StrikeThrough
        {
            get;
            set;
        }

        /// <inheritdoc />
        public float LineHeight
        {
            get;
            set;
        } = 1.0f;

        /// <inheritdoc />
        public SKColor TextColor
        {
            get;
            set;
        } = new SKColor(0xFF000000);

        /// <inheritdoc />
        public FontVariant FontVariant
        {
            get;
            set;
        }
    }
}
