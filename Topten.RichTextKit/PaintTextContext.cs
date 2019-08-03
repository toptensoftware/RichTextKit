using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Resolved, internal class used to pass paint context info
    /// </summary>
    class PaintTextContext
    {
        public SKCanvas Canvas;
        public int SelectionStart;
        public int SelectionEnd;
        public SKPaint PaintSelectionBackground;
        public TextPaintOptions Options;
    }
}
