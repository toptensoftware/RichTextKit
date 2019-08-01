using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichText
{
    /// <summary>
    /// Useful information for cursor calculations
    /// </summary>
    public struct CursorInfo
    {
        /// <summary>
        /// Index of the code point this cluster refers to
        /// </summary>
        public int CodePointIndex;

        /// <summary>
        /// The next code point index (or CodePointIndex if last)
        /// </summary>
        public int NextCodePointIndex;

        /// <summary>
        /// The previous code point index (or 0 if first)
        /// </summary>
        public int PreviousCodePointIndex;

        /// <summary>
        /// The number of code points in this cluster
        /// </summary>
        public int CodePointLength => NextCodePointIndex - CodePointIndex;

        /// <summary>
        /// The font run that displays this cluster
        /// </summary>
        public FontRun FontRun;

        /// <summary>
        /// Get the text direction of this cluster
        /// </summary>
        public TextDirection Direction => FontRun.Direction;

        /// <summary>
        /// The X-coordinate where the cursor should be displayed for this code point
        /// </summary>
        public float CursorXCoord => FontRun.GetCodePointXCoord(CodePointIndex);

        /// <summary>
        /// Get the recommended rectangle to draw a cursor.  
        /// </summary>
        /// <remarks>
        /// This will be based on the *previous* character on this line (or the same character 
        /// if this is first character in the line). The returned rectangle will have zero width 
        /// and it's up to the caller to decide how wide to paint the cursor.
        /// </remarks>
        public SKRect CursorRectangle
        {
            get
            {
                var fr = GetPreviousOrFirstFontRunOnThisLine();
                var rect = new SKRect();
                rect.Left = CursorXCoord;
                rect.Top = fr.Line.YPosition + fr.Line.BaseLine + fr.Ascent;
                rect.Right = rect.Left;
                rect.Bottom = fr.Line.YPosition + fr.Line.BaseLine + fr.Descent;
                return rect;
            }
        }

        /// <summary>
        /// Internal helper to get the previous font run on this line
        /// </summary>
        /// <returns></returns>
        FontRun GetPreviousOrFirstFontRunOnThisLine()
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
    }
}
