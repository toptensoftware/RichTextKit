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
        public IReadOnlyList<FontRun> Runs => RunsInternal;

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
        /// <param name="ctx">The paint context</param>
        internal void Paint(PaintTextContext ctx)
        {
            foreach (var r in Runs)
            {
                r.Paint(ctx);
            }
        }

        /// <summary>
        /// Hit test this line, working out the cluster the x position is over
        /// and closest to.
        /// </summary>
        /// <param name="x">The xcoord relative to the text block</param>
        /// <param name="htr">HitTestResult to be filled out</param>
        internal void HitTest(float x, ref HitTestResult htr)
        {
            // Working variables
            float closestXPosition = 0;
            int closestCodePointIndex = -1;

            // Special handling for clicking after a soft line break in which case
            // the cursor should be positions before the new line, not after it (as this
            // would cause the cursor to appear on the next line).
            if (Runs.Count > 0)
            {
                var lastRun = Runs[Runs.Count - 1];
                if (lastRun.RunKind == FontRunKind.TrailingWhitespace)
                {
                    if ((lastRun.Direction == TextDirection.LTR && x >= lastRun.XCoord + lastRun.Width) ||
                        (lastRun.Direction == TextDirection.RTL && x < lastRun.XCoord))
                    {
                        if (lastRun.CodePoints.Length > 0 && lastRun.CodePoints[lastRun.CodePoints.Length - 1] == '\n')
                        {
                            htr.ClosestCluster = lastRun.End - 1;
                            return;
                        }
                    }
                }
            }

            // Check all runs
            foreach (var r in Runs)
            {
                if (x < r.XCoord)
                {
                    // Before the run...
                    updateClosest(r.XCoord, r.Direction == TextDirection.LTR ? r.Start : r.End, r.Direction);
                }
                else if (x >= r.XCoord + r.Width)
                {
                    // After the run...
                    updateClosest(r.XCoord + r.Width, r.Direction == TextDirection.RTL ? r.Start : r.End, r.Direction);
                }
                else
                {
                    // Inside the run
                    for (int i = 0; i < r.Clusters.Length;)
                    {
                        // Get the xcoord of this cluster
                        var codePointIndex = r.Clusters[i];
                        var xcoord1 = r.GetXCoordOfCodePointIndex(codePointIndex);

                        // Find the code point of the next cluster
                        var j = i;
                        while (j < r.Clusters.Length && r.Clusters[j] == r.Clusters[i])
                            j++;

                        // Get the xcoord of other side of this cluster
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

                        // Gethte xcoord of the other side of the cluster
                        var xcoord2 = r.GetXCoordOfCodePointIndex(codePointIndexOther);

                        // Ensure order correct for easier in-range check
                        if (xcoord1 > xcoord2)
                        {
                            var temp = xcoord1;
                            xcoord1 = xcoord2;
                            xcoord2 = temp;
                        }

                        // On the character?
                        if (x >= xcoord1 && x < xcoord2)
                        {
                            // Store this as the cluster the point is over
                            htr.OverCluster = codePointIndex;

                            // Don't move to the rhs (or lhs) of a line break
                            if (r.CodePoints[codePointIndex - r.Start] == '\n')
                            {
                                htr.ClosestCluster = codePointIndex;
                            }
                            else
                            {
                                // Work out if position is closer to the left or right side of the cluster
                                if (x < (xcoord1 + xcoord2) / 2)
                                {
                                    htr.ClosestCluster = r.Direction == TextDirection.LTR ? codePointIndex : codePointIndexOther;
                                }
                                else
                                {
                                    htr.ClosestCluster = r.Direction == TextDirection.LTR ? codePointIndexOther : codePointIndex;
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
            htr.ClosestCluster = closestCodePointIndex;

            // Helper for updating closest cursor position
            void updateClosest(float xPosition, int codePointIndex, TextDirection dir)
            {
                if (closestCodePointIndex == -1 || Math.Abs(xPosition - x) < Math.Abs(closestXPosition - x))
                {
                    closestXPosition = xPosition;
                    closestCodePointIndex = codePointIndex;
                }
            }
        }

        /// <summary>
        /// Internal List of runs
        /// </summary>
        internal List<FontRun> RunsInternal = new List<FontRun>();
    }
}
