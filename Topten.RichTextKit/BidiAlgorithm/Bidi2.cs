using System;
using System.Collections.Generic;
using System.Linq;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Implementation of Unicode Bidirection Algorithm (UAX #9)
    /// https://unicode.org/reports/tr9/
    /// </summary>
    class Bidi2
    {
        /// <summary>
        /// Constructs a new instance of Bidi algorithm processor
        /// </summary>
        public Bidi2()
        {
        }


        /// <summary>
        /// Processes Bidi Data
        /// </summary>
        /// <param name="data">The data to be processed</param>
        public void Process(BidiData data)
        {
            // Determine isolate pairs
            var _isolatePairs = FindIsolatePairs(data.Directionality);

            // Determine the paragraph embedding level (if implicit)
            var _paragraphEmbeddingLevel = data.ParagraphEmbeddingLevel;
            if (_paragraphEmbeddingLevel == 2)
            {
                _paragraphEmbeddingLevel = DetermineParagraphEmbeddingLevel(data.Directionality, _isolatePairs);
            }

            // Take a copy of the data to work with
            var resultTypes = new Slice<Directionality>(data.Directionality.ToArray());

            // Determine explicit embedding levels (X1-X8)
            var resultLevels = DetermineExplicitEmbeddingLevels(resultTypes, _isolatePairs, _paragraphEmbeddingLevel);


        }


        /// <summary>
        /// Determine explicit result levels
        /// </summary>
        /// <param name="data">The directionality data, will be modified</param>
        /// <param name="isolatePairs">The determined isolate pairs</param>
        /// <param name="paragraphEmbeddingLevel">The paragraph embedding level</param>
        /// <returns>Array of integer result levels</returns>
        private static int[] DetermineExplicitEmbeddingLevels(Slice<Directionality> data, BiDictionary<int, int> isolatePairs, int paragraphEmbeddingLevel)
        {
            // Work variables
            var stack = new Stack<Status>();
            int overflowIsolateCount = 0;
            int overflowEmbeddingCount = 0;
            int validIsolateCount = 0;

            // Constants
            const int maxStackDepth = 125;

            // Create result levels
            var resultLevels = new int[data.Length];
            for (int i = 0; i < resultLevels.Length; i++)
            {
                resultLevels[i] = paragraphEmbeddingLevel;
            }

            // Rule X1 - setup initial state
            stack.Clear();
            stack.Push(new Status()
            {
                embeddingLevel = paragraphEmbeddingLevel,
                overrideStatus = Directionality.ON,         // Neutral
                isolateStatus = false,
            });


            // Process all characters
            for (int i = 0; i < data.Length; i++)
            {
                switch (data[i])
                {
                    case Directionality.RLE:
                        {
                            // Rule X2
                            var newLevel = (stack.Peek().embeddingLevel + 1) | 1;
                            if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                stack.Push(new Status()
                                {
                                    embeddingLevel = newLevel,
                                    overrideStatus = Directionality.ON,
                                    isolateStatus = false,
                                });
                            }
                            else
                            {
                                if (overflowIsolateCount == 0)
                                    overflowEmbeddingCount++;
                            }
                            break;
                        }

                    case Directionality.LRE:
                        {
                            // Rule X3
                            var newLevel = (stack.Peek().embeddingLevel + 2) & ~1;
                            if (newLevel < maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                stack.Push(new Status()
                                {
                                    embeddingLevel = newLevel,
                                    overrideStatus = Directionality.ON,
                                    isolateStatus = false,
                                });
                            }
                            else
                            {
                                if (overflowIsolateCount == 0)
                                    overflowEmbeddingCount++;
                            }
                            break;
                        }

                    case Directionality.RLO:
                        {
                            // Rule X4
                            var newLevel = (stack.Peek().embeddingLevel + 1) | 1;
                            if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                stack.Push(new Status()
                                {
                                    embeddingLevel = newLevel,
                                    overrideStatus = Directionality.R,
                                    isolateStatus = false,
                                });
                            }
                            else
                            {
                                if (overflowIsolateCount == 0)
                                    overflowEmbeddingCount++;
                            }
                            break;
                        }

                    case Directionality.LRO:
                        {
                            // Rule X5
                            var newLevel = (stack.Peek().embeddingLevel + 2) & ~1;
                            if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                stack.Push(new Status()
                                {
                                    embeddingLevel = newLevel,
                                    overrideStatus = Directionality.L,
                                    isolateStatus = false,
                                });
                            }
                            else
                            {
                                if (overflowIsolateCount == 0)
                                    overflowEmbeddingCount++;
                            }
                            break;
                        }

                    case Directionality.RLI:
                    case Directionality.LRI:
                    case Directionality.FSI:
                        {
                            // Rule X5a, X5b and X5c
                            var resolvedIsolate = data[i];

                            if (resolvedIsolate == Directionality.FSI)
                            {
                                // Rule X5c
                                if (DetermineParagraphEmbeddingLevel(data.SubSlice(i + 1), isolatePairs) == 1)
                                    resolvedIsolate = Directionality.RLI;
                                else
                                    resolvedIsolate = Directionality.LRI;
                            }

                            // Replace RLI's level with current embedding level
                            var tos = stack.Peek();
                            resultLevels[i] = tos.embeddingLevel;

                            // Apply override
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                data[i] = tos.overrideStatus;
                            }

                            // Work out new level
                            int newLevel;
                            if (resolvedIsolate == Directionality.RLI)
                                newLevel = (tos.embeddingLevel + 1) | 1;
                            else
                                newLevel = (tos.embeddingLevel + 2) & ~1;

                            // Valid?
                            if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                validIsolateCount++;
                                stack.Push(new Status()
                                {
                                    embeddingLevel = newLevel,
                                    overrideStatus = Directionality.ON,
                                    isolateStatus = true,
                                });
                            }
                            else
                            {
                                overflowIsolateCount++;
                            }
                            break;
                        }

                    case Directionality.BN:
                        {
                            // Mentioned in rule X6 - "for all types besides ..., BN, ..."
                            // no-op
                            break;
                        }

                    default:
                        {
                            // Rule X6
                            var tos = stack.Peek();
                            resultLevels[i] = tos.embeddingLevel;
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                data[i] = tos.overrideStatus;
                            }
                            break;
                        }

                    case Directionality.PDI:
                        {
                            // Rule X6a
                            if (overflowIsolateCount > 0)
                            {
                                overflowIsolateCount--;
                            }
                            else if (validIsolateCount != 0)
                            {
                                overflowEmbeddingCount = 0;
                                while (!stack.Peek().isolateStatus)
                                    stack.Pop();
                                stack.Pop();
                                validIsolateCount--;
                            }

                            var tos = stack.Peek();
                            resultLevels[i] = tos.embeddingLevel;
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                data[i] = tos.overrideStatus;
                            }
                            break;
                        }

                    case Directionality.PDF:
                        {
                            // Rule X7
                            if (overflowIsolateCount == 0)
                            {
                                if (overflowEmbeddingCount > 0)
                                {
                                    overflowEmbeddingCount--;
                                }
                                else
                                {
                                    if (!stack.Peek().isolateStatus && stack.Count >= 2)
                                    {
                                        stack.Pop();
                                    }
                                }
                            }
                            break;
                        }

                    case Directionality.B:
                        {
                            // Rule X8
                            resultLevels[i] = paragraphEmbeddingLevel;
                            break;
                        }


                }
            }

            return resultLevels;
        }

        class Status
        {
            public int embeddingLevel;
            public Directionality overrideStatus;
            public bool isolateStatus;
        }

        /// <summary>
        /// Build a list of matching isolates for a directionality slice 
        /// Implements BD9
        /// </summary>
        /// <param name="data">The directionality data to be processed</param>
        /// <returns>A BiDirectional dictionary mapping isolate start index => isolate end index</returns>
        static BiDictionary<int, int> FindIsolatePairs(Slice<Directionality> data)
        {
            var result = new BiDictionary<int, int>();
            var pendingOpens = new Stack<int>();

            for (int i = 0; i < data.Length; i++)
            {
                var t = data[i];
                if (t == Directionality.LRI || t == Directionality.RLI || t == Directionality.FSI)
                {
                    pendingOpens.Push(i);
                }
                else if (t == Directionality.PDI)
                {
                    if (pendingOpens.Count > 0)
                    {
                        result.Add(pendingOpens.Pop(), i);
                    }
                }
            }

            return result;
        }



        private static int DetermineParagraphEmbeddingLevel(Slice<Directionality> data, BiDictionary<int, int> isolatePairs)
        {
            // P2
            for (var i = 0; i < data.Length; ++i)
            {
                switch (data[i])
                {
                    case Directionality.L:
                        // P3
                        return 0;

                    case Directionality.AL:
                    case Directionality.R:
                        // P3
                        return 1;

                    case Directionality.FSI:
                    case Directionality.LRI:
                    case Directionality.RLI:

                        // Skip isolate pairs
                        if (!isolatePairs.TryGetValue(i, out i))
                        {
                            // "or, if it has no matching PDI, the end of the paragraph."
                            i = data.Length;
                        }
                        break;
                }
            }

            // P3
            return 0;
        }
    }
}
