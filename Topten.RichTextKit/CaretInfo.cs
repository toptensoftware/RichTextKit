using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Useful information for caret calculations
    /// </summary>
    public struct CaretInfo
    {
        /// <summary>
        /// Index of the code point this caret info refers to
        /// </summary>
        public int CodePointIndex;

        /// <summary>
        /// The next code point index (same as CodePointIndex if last)
        /// </summary>
        public int NextCodePointIndex;

        /// <summary>
        /// The previous code point index (same as CodePointIndex if first)
        /// </summary>
        public int PreviousCodePointIndex;

        /// <summary>
        /// The number of code points in this cluster
        /// </summary>
        public int CodePointCount => NextCodePointIndex - CodePointIndex;

        /// <summary>
        /// The font run that displays this cluster
        /// </summary>
        public FontRun FontRun;

        /// <summary>
        /// The X-coordinate where the caret should be displayed for this code point
        /// </summary>
        public float CaretXCoord => FontRun.GetXCoordOfCodePointIndex(CodePointIndex);

        /// <summary>
        /// A rectangle describing where the caret should be drawn, relative to the top-left
        /// corner of the text block. The caret should be drawn from the returned rectangle's
        /// top-right to bottom-left.
        /// </summary>
        /// <remarks>
        /// This will be based on the *previous* character on this line (or the same character 
        /// if this is first character in the line). 
        /// 
        /// Usually this will be a zero-width rectangle describing the x, top and bottom 
        /// coordinates of where the caret should be drawn.  The width of the drawn caret
        /// isn't provided and should be determined by the client.
        /// 
        /// When the caret is immediately following an italic character, the returned
        /// rectangle will be sloped to the right and should be drawn from the top-right
        /// coordinate to the bottom-left coordinate.  
        /// 
        /// If you don't want to draw a sloped caret for italics, use the top and bottom 
        /// coordinates of the returned rectangle and get the x-coordinate from the 
        /// <see cref="CaretXCoord"/> property.
        /// </remarks>
        public SKRect CaretRectangle
        {
            get
            {
                // Get the font run to be used for caret metrics
                var fr = GetFontRunForCaretMetrics();

                // Setup the basic rectangle
                var rect = new SKRect();
                rect.Left = CaretXCoord;
                rect.Top = fr.Line.YPosition + fr.Line.BaseLine + fr.Ascent;
                rect.Right = rect.Left;
                rect.Bottom = fr.Line.YPosition + fr.Line.BaseLine + fr.Descent;

                // Apply slant if italic
                if (fr.Style.FontItalic)
                {
                    rect.Left -= rect.Height / 14;
                    rect.Right = rect.Left + rect.Height / 5;
                }

                return rect;
            }
        }

        /// <summary>
        /// Internal helper to get the font run that should
        /// be used for caret metrics.
        /// </summary>
        /// <remarks>
        /// The returned font run is the font run of the previous
        /// character, or the same character if the first font run
        /// on the line.
        /// </remarks>
        /// <returns>The determined font run</returns>
        FontRun GetFontRunForCaretMetrics()
        {
            // Same font run?
            if (CodePointIndex > FontRun.Start)
                return FontRun;

            // Try to get the previous font run in this line
            var lineRuns = FontRun.Line.Runs as List<FontRun>;
            int index = lineRuns.IndexOf(FontRun);
            if (index <= 0)
                return FontRun;

            // Use the previous font run
            return lineRuns[index - 1];
        }

        /*
         * Commented out as untested.
         * 
         * 
        /// <summary>
        /// The base line of this cluster, relative to the top of the text block
        /// </summary>
        public float ClusterBaseLine => FontRun.Line.YPosition + FontRun.Line.BaseLine;

        /// <summary>
        /// The cluster's ascent
        /// </summary>
        public float ClusterAscent => FontRun.Ascent;

        /// <summary>
        /// The cluster's descent
        /// </summary>
        public float ClusterDescent => FontRun.Descent;

        /// <summary>
        /// Get the left x-coord of this cluster
        /// </summary>
        public float ClusterLeftXCoord
        {
            get
            {
                return FontRun.GetCodePointXCoord(Direction == TextDirection.LTR ? CodePointIndex : NextCodePointIndex);
            }
        }

        /// <summary>
        /// Get the right x-coord of this cluster
        /// </summary>
        public float ClusterRightXCoord
        {
            get
            {
                return FontRun.GetCodePointXCoord(Direction == TextDirection.RTL ? CodePointIndex : NextCodePointIndex);
            }
        }
        */
    }
}
