using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Useful information for cursor calculations
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
        /// The X-coordinate where the cursor should be displayed for this code point
        /// </summary>
        public float CaretXCoord => FontRun.GetXCoordOfCodePointIndex(CodePointIndex);

        /// <summary>
        /// Get the recommended rectangle to draw a caret.  
        /// </summary>
        /// <remarks>
        /// This will be based on the *previous* character on this line (or the same character 
        /// if this is first character in the line). The caret should be drawn as a line from 
        /// the rectangle's top-right to bottom-left.  Usually the rectangle will be zero width
        /// (ie: a vertical line), except when over italic text in which case it will be slanted.
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
