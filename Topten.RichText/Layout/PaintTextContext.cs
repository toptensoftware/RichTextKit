using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    class PaintTextContext
    {
        public SKCanvas Canvas;
        public int SelectionStart;
        public int SelectionEnd;
        public SKPaint PaintSelectionBackground;
        public TextPaintOptions Options;
    }
}
