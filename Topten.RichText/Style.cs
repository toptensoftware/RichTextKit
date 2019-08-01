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
        public string FontFamily
        {
            get;
            set;
        } = "Arial";

        public float FontSize
        {
            get;
            set;
        } = 12;

        public int FontWeight
        {
            get;
            set;
        } = 400;

        public bool FontItalic
        {
            get;
            set;
        }

        public UnderlineStyle Underline
        {
            get;
            set;
        }

        public StrikeThroughStyle StrikeThrough
        {
            get;
            set;
        }

        public float LineHeight
        {
            get;
            set;
        } = 1.0f;

        public SKColor TextColor
        {
            get;
            set;
        } = new SKColor(0xFF000000);

        public FontVariant FontVariant
        {
            get;
            set;
        }
    }
}
