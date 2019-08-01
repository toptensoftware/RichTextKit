using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    /// <summary>
    /// Represents a laid out line of text
    /// </summary>
    public class TextLine
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TextLine()
        {
        }

        /// <summary>
        /// List of text runs in this line
        /// </summary>
        public List<FontRun> Runs = new List<FontRun>();

        /// <summary>
        /// Get the text block that owns this line
        /// </summary>
        public TextBlock TextBlock
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the next line in this text block
        /// </summary>
        public TextLine NextLine
        {
            get
            {
                int index = (TextBlock.Lines as List<TextLine>).IndexOf(this);
                if (index < 0 || index + 1 >= TextBlock.Lines.Count)
                    return null;
                return TextBlock.Lines[index + 1];
            }
        }

        /// <summary>
        /// Get the next line in this text block
        /// </summary>
        public TextLine PreviousLine
        {
            get
            {
                int index = (TextBlock.Lines as List<TextLine>).IndexOf(this);
                if (index <= 0)
                    return null;
                return TextBlock.Lines[index - 1];
            }
        }

        /// <summary>
        /// Position of this line relative to the paragraph
        /// </summary>
        public float YPosition
        {
            get;
            internal set;
        }

        /// <summary>
        /// The base line for text in this line (relative to YPosition)
        /// </summary>
        public float BaseLine
        {
            get;
            internal set;
        }

        /// <summary>
        /// The maximum ascent of all font runs in this line
        /// </summary>
        public float MaxAscent
        {
            get;
            internal set;
        }


        /// <summary>
        /// The maximum desscent of all font runs in this line
        /// </summary>
        public float MaxDescent
        {
            get;
            internal set;
        }

        /// <summary>
        /// The height of all text elements in this line
        /// </summary>
        public float TextHeight => -MaxAscent + MaxDescent;

        /// <summary>
        /// Total height of this line
        /// </summary>
        public float Height
        {
            get;
            internal set;
        }

        /// <summary>
        /// The width of the content on this line (excluding trailing whitespace)
        /// </summary>
        public float Width
        {
            get;
            internal set;
        }

        /// <summary>
        /// Paint this line
        /// </summary>
        /// <param name="canvas"></param>
        internal void Paint(PaintTextContext ctx)
        {
            foreach (var r in Runs)
            {
                r.Paint(ctx);
            }
        }

        internal void HitTest(float x, ref HitTestResult htr)
        {
            float closestXPosition = 0;
            float closestXDelta = float.MaxValue;
            int closestCodePointIndex = -1;
            TextDirection closestDirection = TextDirection.LTR;

            void updateClosest(float xPosition, int codePointIndex, TextDirection dir)
            {
                var delta = x - xPosition;

                // First time?
                if (closestCodePointIndex < 0)
                {
                    closestXPosition = xPosition;
                    closestXDelta = delta;
                    closestCodePointIndex = codePointIndex;
                    closestDirection = dir;
                    return;
                }

                // Same direction as current closest?
                if (closestDirection == dir)
                {
                    if (Math.Abs(delta) < Math.Abs(closestXDelta))
                    {
                        closestXPosition = xPosition;
                        closestXDelta = delta;
                        closestCodePointIndex = codePointIndex;
                        closestDirection = dir;
                    }
                    return;
                }

                if (closestDirection == TextDirection.LTR && 
                    closestXPosition > x && 
                    x < xPosition)
                {
                    closestXPosition = xPosition;
                    closestXDelta = delta;
                    closestCodePointIndex = codePointIndex;
                    closestDirection = dir;
                    return;
                }

                if (closestDirection == TextDirection.LTR && 
                    closestXPosition < x && 
                    x > xPosition)
                {
                    closestXPosition = xPosition;
                    closestXDelta = delta;
                    closestCodePointIndex = codePointIndex;
                    closestDirection = dir;
                    return;
                }

            }

            foreach (var r in Runs)
            {
                if (x >= r.XPosition && x < r.XPosition + r.Width)
                {
                    int xx = 3;
                }

                if (r.Direction == TextDirection.LTR)
                {
                    updateClosest(r.XPosition, r.Start, r.Direction);
                    updateClosest(r.XPosition + r.Width, r.End, r.Direction);
                }
                else
                {
                    updateClosest(r.XPosition + r.Width, r.Start, r.Direction);
                    updateClosest(r.XPosition, r.End, r.Direction);
                }

                for (int i = 0; i < r.Clusters.Length;)
                {
                    // Get the xcoord of this cluster
                    var codePointIndex = r.Clusters[i];
                    var xcoord = r.XPosition + r.RelativeCodePointXCoords[codePointIndex - r.Start];
                    updateClosest(xcoord, codePointIndex, r.Direction);

                    // Find the code point of the next cluster
                    var j = i;
                    while (j < r.Clusters.Length && r.Clusters[j] == r.Clusters[i])
                        j++;

                    // Look for exact character we're over
                    if (htr.OverCharacter < 0)
                    {
                        // Get it's xcoord
                        float xcoord2;
                        if (r.Direction == TextDirection.LTR)
                        {
                            if (j == r.Clusters.Length)
                            {
                                xcoord2 = r.XPosition + r.Width;
                            }
                            else
                            {
                                xcoord2 = r.XPosition + r.RelativeCodePointXCoords[r.Clusters[j] - r.Start];
                            }
                        }
                        else
                        {
                            if (i > 0)
                            {
                                xcoord2 = r.XPosition + r.RelativeCodePointXCoords[r.Clusters[i - 1] - r.Start];
                            }
                            else
                            {
                                xcoord2 = r.XPosition;
                            }
                        }

                        if (xcoord2 > xcoord)
                        {
                            if (x >= xcoord && x < xcoord2)
                            {
                                htr.OverCharacter = r.Clusters[i];
                            }
                        }
                        else
                        {
                            if (x >= xcoord2 && x < xcoord)
                            {
                                htr.OverCharacter = r.Clusters[i];
                            }
                        }
                    }

                    // Continue with the next cluster
                    i = j;
                }
            }

            htr.ClosestCharacter = closestCodePointIndex;
        }
    }
}
