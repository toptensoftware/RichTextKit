using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        BiDictionary<int, int> _isolatePairs = new BiDictionary<int, int>();
        Slice<Directionality> _originalTypes;
        Slice<Directionality> _resultTypes;
        Slice<int> _resultLevels;
        Buffer<Directionality> _resultTypesBuffer = new Buffer<Directionality>();
        Buffer<int> _resultLevelsBuffer = new Buffer<int>();
        int _paragraphEmbeddingLevel;
        Stack<Status> _statusStack = new Stack<Status>();
        Buffer<int> _X9Map = new Buffer<int>();
        List<LevelRun> _levelRuns = new List<LevelRun>();


        /// <summary>
        /// Processes Bidi Data
        /// </summary>
        /// <param name="data">The data to be processed</param>
        public void Process(BidiData data)
        {
            // Reset state
            _isolatePairs.Clear();
            _resultTypesBuffer.Clear();
            _levelRuns.Clear();

            // Setup original types and result types
            _originalTypes = data.Directionality;
            _resultTypes = _resultTypesBuffer.Add(data.Directionality);

            // Determine isolate pairs
            FindIsolatePairs();

            // Determine the paragraph embedding level (if implicit)
            _paragraphEmbeddingLevel = data.ParagraphEmbeddingLevel;
            if (_paragraphEmbeddingLevel == 2)
            {
                _paragraphEmbeddingLevel = DetermineParagraphEmbeddingLevel(_originalTypes);
            }

            // Create result levels
            _resultLevels = _resultLevelsBuffer.Add(_originalTypes.Length);
            _resultLevels.Fill(_paragraphEmbeddingLevel);

            // Determine explicit embedding levels (X1-X8)
            DetermineExplicitEmbeddingLevels();

            // Build the rule X9 map
            BuildX9Map();

            // Find the level runs
            FindLevelRuns();

            // Process the isolating run sequences
            ProcessIsolatedRunSequences();
        }


        /// <summary>
        /// Determine explicit result levels
        /// </summary>
        private void DetermineExplicitEmbeddingLevels()
        {
            // Work variables
            _statusStack.Clear();
            int overflowIsolateCount = 0;
            int overflowEmbeddingCount = 0;
            int validIsolateCount = 0;

            // Constants
            const int maxStackDepth = 125;

            // Rule X1 - setup initial state
            _statusStack.Clear();
            _statusStack.Push(new Status()
            {
                embeddingLevel = _paragraphEmbeddingLevel,
                overrideStatus = Directionality.ON,         // Neutral
                isolateStatus = false,
            });


            // Process all characters
            for (int i = 0; i < _originalTypes.Length; i++)
            {
                switch (_originalTypes[i])
                {
                    case Directionality.RLE:
                        {
                            // Rule X2
                            var newLevel = (_statusStack.Peek().embeddingLevel + 1) | 1;
                            if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                _statusStack.Push(new Status()
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
                            var newLevel = (_statusStack.Peek().embeddingLevel + 2) & ~1;
                            if (newLevel < maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                _statusStack.Push(new Status()
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
                            var newLevel = (_statusStack.Peek().embeddingLevel + 1) | 1;
                            if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                _statusStack.Push(new Status()
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
                            var newLevel = (_statusStack.Peek().embeddingLevel + 2) & ~1;
                            if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                            {
                                _statusStack.Push(new Status()
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
                            var resolvedIsolate = _originalTypes[i];

                            if (resolvedIsolate == Directionality.FSI)
                            {
                                // Rule X5c
                                if (DetermineParagraphEmbeddingLevel(_originalTypes.SubSlice(i + 1)) == 1)
                                    resolvedIsolate = Directionality.RLI;
                                else
                                    resolvedIsolate = Directionality.LRI;
                            }

                            // Replace RLI's level with current embedding level
                            var tos = _statusStack.Peek();
                            _resultLevels[i] = tos.embeddingLevel;

                            // Apply override
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                _originalTypes[i] = tos.overrideStatus;
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
                                _statusStack.Push(new Status()
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
                            var tos = _statusStack.Peek();
                            _resultLevels[i] = tos.embeddingLevel;
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                _originalTypes[i] = tos.overrideStatus;
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
                                while (!_statusStack.Peek().isolateStatus)
                                    _statusStack.Pop();
                                _statusStack.Pop();
                                validIsolateCount--;
                            }

                            var tos = _statusStack.Peek();
                            _resultLevels[i] = tos.embeddingLevel;
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                _originalTypes[i] = tos.overrideStatus;
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
                                    if (!_statusStack.Peek().isolateStatus && _statusStack.Count >= 2)
                                    {
                                        _statusStack.Pop();
                                    }
                                }
                            }
                            break;
                        }

                    case Directionality.B:
                        {
                            // Rule X8
                            _resultLevels[i] = _paragraphEmbeddingLevel;
                            break;
                        }


                }
            }
        }

        struct Status
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
        void FindIsolatePairs()
        {
            var pendingOpens = new Stack<int>();

            for (int i = 0; i < _originalTypes.Length; i++)
            {
                var t = _originalTypes[i];
                if (t == Directionality.LRI || t == Directionality.RLI || t == Directionality.FSI)
                {
                    pendingOpens.Push(i);
                }
                else if (t == Directionality.PDI)
                {
                    if (pendingOpens.Count > 0)
                    {
                        _isolatePairs.Add(pendingOpens.Pop(), i);
                    }
                }
            }
        }



        int DetermineParagraphEmbeddingLevel(Slice<Directionality> data)
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
                        // (Because we're working with a slice, we need to adjust the indicies
                        //  we're using for the isolatePairs map)
                        if (_isolatePairs.TryGetValue(data.Start + i, out i))
                        {
                            i -= data.Start;
                        }
                        else
                        {
                            i = data.Length;
                        }
                        break;
                }
            }

            // P3
            return 0;
        }

        /// <summary>
        /// Build a map to the original data positions that excludes all
        /// the types defined by rule X9
        /// </summary>
        void BuildX9Map()
        {
            // Reserve room for the x9 map
            _X9Map.Length = _originalTypes.Length;

            // Build a map the removes all x9 characters
            var j = 0;
            for (int i = 0; i < _originalTypes.Length; i++)
            {
                if (!IsRemovedByX9(_originalTypes[i]))
                {
                    _X9Map[j++] = i;
                }
            }

            // Set the final length
            _X9Map.Length = j;
        }


        /// <summary>
        /// Find the original character index for an entry in the X9 map
        /// </summary>
        /// <param name="index">Index in the x9 map</param>
        /// <returns>Index to the original data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int mapX9(int index)
        {
            return _X9Map[index];
        }

        /// <summary>
        /// Helper to check if a directionality is removed by rule X9
        /// </summary>
        /// <param name="biditype">The bidi type to check</param>
        /// <returns>True if rule X9 would remove this character; otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRemovedByX9(Directionality biditype)
        {
            switch (biditype)
            {
                case Directionality.LRE:
                case Directionality.RLE:
                case Directionality.LRO:
                case Directionality.RLO:
                case Directionality.PDF:
                case Directionality.BN:
                    return true;

                default:
                    return false;
            }
        }


        struct LevelRun
        {
            public LevelRun(int start, int length, int level)
            {
                this.start = start;
                this.length = length;
                this.level = level;
            }
            public int start;
            public int length;
            public int level;
        }

        void FindLevelRuns()
        {
            int currentLevel = -1;
            int runStartIndex = 0;
            for (int i = 0; i < _X9Map.Length; ++i)
            {
                int level = _resultLevels[mapX9(i)];
                if (level != currentLevel)
                {
                    if (currentLevel != -1)
                    {
                        _levelRuns.Add(new LevelRun(runStartIndex, i - runStartIndex, currentLevel));
                    }
                    currentLevel = level;
                    runStartIndex = i;
                }
            }

            // Don't forget the final level run
            if (currentLevel != -1)
            {
                _levelRuns.Add(new LevelRun(runStartIndex, _X9Map.Length - runStartIndex, currentLevel));
            }
        }

        Buffer<int> _isolatedRunBuffer;

        /// <summary>
        /// Given a x9 map character index, find the level run that starts at that position
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        int FindRunForIndex(int index)
        {
            for (int i = 0; i < _levelRuns.Count; i++)
            {
                if (mapX9(_levelRuns[i].start) == index)
                    return i;
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Process all isolated run sequence
        /// </summary>
        void ProcessIsolatedRunSequences()
        {
            // Process all runs
            while (_levelRuns.Count > 0)
            {
                // Get the index of the first character in this run
                var firstCharacterIndex = mapX9(_levelRuns[0].start);

                // Clear the buffer
                _isolatedRunBuffer.Clear();

                // Process all runs that continue on from this run
                var runIndex = 0;
                while (true)
                {
                    // Get the run
                    var r = _levelRuns[runIndex];

                    // Remove this run as we've now processed it
                    _levelRuns.RemoveAt(runIndex);

                    // Add the x9 map indicies for the run range
                    _isolatedRunBuffer.Add(_X9Map.SubSlice(r.start, r.length));

                    // Get the last character and see if it's an isolating run with a matching
                    // PDI and move to that run
                    int lastCharacterIndex = _isolatedRunBuffer[_isolatedRunBuffer.Length - 1];
                    var lastType = _originalTypes[lastCharacterIndex];
                    if ((lastType == Directionality.LRI || lastType == Directionality.RLI || lastType == Directionality.FSI) &&
                            _isolatePairs.TryGetValue(lastCharacterIndex, out var nextRunIndex)) 
                    {
                        // Find the continuing run index
                        runIndex = FindRunForIndex(nextRunIndex);
                    }
                    else
                    {
                        break;
                    }
                }

                // Process an isolated run comprising the character indices it _isolatedRunBuffer
                ProcessIsolatedRunSequences()
            }
        }

        /// <summary>
        /// Process a single isolated run sequence, where the character sequence
        /// is currently held in _isolatedRunSequence.
        /// </summary>
        void ProcessIsolatedRunSequence()
        {

        }
    }
}
