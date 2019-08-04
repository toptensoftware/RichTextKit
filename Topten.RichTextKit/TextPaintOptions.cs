using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Options to control how TextBlock is rendered.
    /// </summary>
    public class TextPaintOptions
    {
        /// <summary>
        /// An optional code point index of the start of a selection range.
        /// </summary>
        /// <remarks>
        /// Both start and end selection need to be set for selection
        /// painting to occur.  Coordinates are in utf-16 characters
        /// </remarks>
        public int? SelectionStart
        {
            get;
            set;
        }

        /// <summary>
        /// An optional code point index of the end of a selection range.
        /// </summary>
        /// <remarks>
        /// Both start and end selection need to be set for selection
        /// painting to occur.  Coordinates are in utf-16 characters
        /// </remarks>
        public int? SelectionEnd
        {
            get;
            set;
        }

        /// <summary>
        /// The color to draw selection background with.
        /// </summary>
        public SKColor SelectionColor
        {
            get;
            set;
        }

        /// <summary>
        /// Controls whether text is rendered with anti-aliasing.
        /// </summary>
        public bool IsAntialias
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Controls whether text is rendered using LCD sub-pixel rendering.
        /// </summary>
        public bool LcdRenderText
        {
            get;
            set;
        } = false;

        /// <summary>
        /// A default set of paint options that renders text blocks without 
        /// a selection range.
        /// </summary>
        public static TextPaintOptions Default = new TextPaintOptions();
    }
}
