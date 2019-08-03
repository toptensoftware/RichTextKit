using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Options controlling how TextBlock is rendered
    /// </summary>
    public class TextPaintOptions
    {
        /// <summary>
        /// Optional start selection position
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
        /// Option end selection position
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
        /// Color to draw selection background with
        /// </summary>
        public SKColor SelectionColor
        {
            get;
            set;
        }

        /// <summary>
        /// Render with anti aliasing
        /// </summary>
        public bool IsAntialias
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Use LCD text rendering
        /// </summary>
        public bool LcdRenderText
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Default paint options (no selection)
        /// </summary>
        public static TextPaintOptions Default = new TextPaintOptions();
    }
}
