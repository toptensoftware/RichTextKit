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
            int closestCodePointIndex = -1;
            TextDirection closestDirection = TextDirection.LTR;

            // Special handling for clicking after a soft line break
            if (Runs.Count > 0)
            {
                var lastRun = Runs[Runs.Count - 1];
                if (lastRun.RunKind == FontRunKind.TrailingWhitespace)
                {
                    if ((lastRun.Direction == TextDirection.LTR && x >= lastRun.XPosition + lastRun.Width) ||
                        (lastRun.Direction == TextDirection.RTL && x < lastRun.XPosition))
                    {
                        if (lastRun.CodePoints.Length > 0 && lastRun.CodePoints[lastRun.CodePoints.Length - 1] == '\n')
                        {
                            htr.ClosestCharacter = lastRun.End - 1;
                            return;
                        }
                    }
                }
            }

            foreach (var r in Runs)
            {
                if (x < r.XPosition)
                {
                    updateClosest(r.XPosition, r.Direction == TextDirection.LTR ? r.Start : r.End, r.Direction);
                }
                else if (x >= r.XPosition + r.Width)
                {
                    updateClosest(r.XPosition + r.Width, r.Direction == TextDirection.RTL ? r.Start : r.End, r.Direction);
                }
                else
                {
                    for (int i = 0; i < r.Clusters.Length;)
                    {
                        // Get the xcoord of this cluster
                        var codePointIndex = r.Clusters[i];
                        var xcoord1 = r.GetCodePointXCoord(codePointIndex);

                        // Find the code point of the next cluster
                        var j = i;
                        while (j < r.Clusters.Length && r.Clusters[j] == r.Clusters[i])
                            j++;

                        // Get the xcoord of other side
                        int codePointIndexOther;
                        if (r.Direction == TextDirection.LTR)
                        {
                            if (j == r.Clusters.Length)
                            {
                                codePointIndexOther = r.End;
                            }
                            else
                            {
                                codePointIndexOther = r.Clusters[j];
                            }
                        }
                        else
                        {
                            if (i > 0)
                            {
                                codePointIndexOther = r.Clusters[i - 1];
                            }
                            else
                            {
                                codePointIndexOther = r.End;
                            }
                        }

                        var xcoord2 = r.GetCodePointXCoord(codePointIndexOther);

                        // Ensure order
                        if (xcoord1 > xcoord2)
                        {
                            var temp = xcoord1;
                            xcoord1 = xcoord2;
                            xcoord2 = temp;
                        }

                        // On the character?
                        if (x >= xcoord1 && x < xcoord2)
                        {
                            htr.OverCharacter = codePointIndex;

                            // Don't move to the rhs (or lhs) of a line break
                            if (r.CodePoints[codePointIndex - r.Start] == '\n')
                            {
                                htr.ClosestCharacter = codePointIndex;
                            }
                            else
                            {
                                if (x < (xcoord1 + xcoord2) / 2)
                                {
                                    htr.ClosestCharacter = r.Direction == TextDirection.LTR ? codePointIndex : codePointIndexOther;
                                }
                                else
                                {
                                    htr.ClosestCharacter = r.Direction == TextDirection.LTR ? codePointIndexOther : codePointIndex;
                                }
                            }
                            return;
                        }

                        // Move to the next cluster
                        i = j;
                    }
                }
            }

            // Store closest character
            htr.ClosestCharacter = closestCodePointIndex;



            void updateClosest(float xPosition, int codePointIndex, TextDirection dir)
            {
                bool closest = false;

                if (closestCodePointIndex == -1)
                {
                    closest = true;
                }
                else if (Math.Abs(xPosition - x) < Math.Abs(closestXPosition - x))
                {
                    closest = true;
                }

                if (closest)
                {
                    closestXPosition = xPosition;
                    closestCodePointIndex = codePointIndex;
                    closestDirection = dir;
                }
            }
        }
    }
}
