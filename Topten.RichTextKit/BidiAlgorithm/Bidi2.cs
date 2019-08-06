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
        Buffer<Directionality> _resultTypesBuffer = new Buffer<Directionality>();
        Slice<int> _levels;
        Buffer<int> _levelsBuffer = new Buffer<int>();
        int _paragraphEmbeddingLevel;
        Stack<Status> _statusStack = new Stack<Status>();
        Buffer<int> _X9Map = new Buffer<int>();
        List<LevelRun> _levelRuns = new List<LevelRun>();

        Slice<PairedBracketType> _pairedBracketTypes;
        Slice<int> _pairedBracketValues;

        public Slice<int> ResultLevels => _levels;

        /// <summary>
        /// Processes Bidi Data
        /// </summary>
        public void Process(Slice<Directionality> directionality, Slice<PairedBracketType> pairedBracketTypes, Slice<int> pairedBracketValues, int paragraphEmbeddingLevel)
        {
            // Reset state
            _isolatePairs.Clear();
            _resultTypesBuffer.Clear();
            _levelRuns.Clear();
            _levelsBuffer.Clear();

            // Setup original types and result types
            _originalTypes = directionality;
            _resultTypes = _resultTypesBuffer.Add(directionality);

            // Capture paired bracket values and types
            _pairedBracketTypes = pairedBracketTypes;
            _pairedBracketValues = pairedBracketValues;

            // Determine isolate pairs
            FindIsolatePairs();

            // Determine the paragraph embedding level (if implicit)
            _paragraphEmbeddingLevel = paragraphEmbeddingLevel;
            if (_paragraphEmbeddingLevel == 2)
            {
                _paragraphEmbeddingLevel = DetermineParagraphEmbeddingLevel(_originalTypes);
            }

            // Create result levels
            _levels = _levelsBuffer.Add(_originalTypes.Length);
            _levels.Fill(_paragraphEmbeddingLevel);

            // Determine explicit embedding levels (X1-X8)
            DetermineExplicitEmbeddingLevels();

            // Build the rule X9 map
            BuildX9Map();

            // Find the level runs
            FindLevelRuns();

            // Process the isolating run sequences
            ProcessIsolatedRunSequences();

            // Reset whitespace levels
            ResetWhitespaceLevels();
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
                            _levels[i] = tos.embeddingLevel;

                            // Apply override
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                _resultTypes[i] = tos.overrideStatus;
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
                            _levels[i] = tos.embeddingLevel;
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                _resultTypes[i] = tos.overrideStatus;
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
                            _levels[i] = tos.embeddingLevel;
                            if (tos.overrideStatus != Directionality.ON)
                            {
                                _resultTypes[i] = tos.overrideStatus;
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
                            _levels[i] = _paragraphEmbeddingLevel;
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
                int level = _levels[mapX9(i)];
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

        Buffer<int> _isolatedRunBuffer = new Buffer<int>();

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
            throw new InvalidOperationException("Internal error");
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
                ProcessIsolatedRunSequence();
            }
        }

        private static Directionality DirectionFromLevel(int level)
        {
            return ((level & 0x1) == 0) ? Directionality.L : Directionality.R;
        }


        int _runLevel;
        Directionality _runDirection;
        MappedSlice<Directionality> _runResultTypes;
        MappedSlice<Directionality> _runOriginalTypes;
        MappedSlice<int> _runLevels;
        MappedSlice<PairedBracketType> _runPairedBracketTypes;
        MappedSlice<int> _runPairedBracketValues;

        /// <summary>
        /// Process a single isolated run sequence, where the character sequence
        /// is currently held in _isolatedRunSequence.
        /// </summary>
        void ProcessIsolatedRunSequence()
        {
            // Create mappings on the underlying buffers
            _runResultTypes = new MappedSlice<Directionality>(_resultTypes, _isolatedRunBuffer.AsSlice());
            _runOriginalTypes = new MappedSlice<Directionality>(_originalTypes, _isolatedRunBuffer.AsSlice());
            _runLevels = new MappedSlice<int>(_levels, _isolatedRunBuffer.AsSlice());
            _runPairedBracketTypes = new MappedSlice<PairedBracketType>(_pairedBracketTypes, _isolatedRunBuffer.AsSlice());
            _runPairedBracketValues = new MappedSlice<int>(_pairedBracketValues, _isolatedRunBuffer.AsSlice());

            // Get original indicies to first and last character in this run
            int firstCharIndex = _isolatedRunBuffer[0];
            int lastCharIndex = _isolatedRunBuffer[_isolatedRunBuffer.Length - 1];

            // Get the level (and direction) of the run
            _runLevel = _levels[firstCharIndex];
            _runDirection = DirectionFromLevel(_runLevel);

            // Work out sos
            int i = firstCharIndex;
            while (i >= 0 && IsRemovedByX9(_originalTypes[i]))
                i--;
            var prevLevel = i < 0 ? _paragraphEmbeddingLevel : _levels[i];
            var sos = DirectionFromLevel(Math.Max(prevLevel, _runLevel));

            // Work out eos
            var lastType = _runResultTypes[_runResultTypes.Length - 1];
            int nextLevel;
            if (lastType == Directionality.LRI || lastType == Directionality.RLI || lastType == Directionality.FSI)
            {
                nextLevel = _paragraphEmbeddingLevel;
            }
            else
            {
                i = lastCharIndex;
                while (i < _originalTypes.Length && IsRemovedByX9(_originalTypes[i]))
                    i++;
                nextLevel = i >= _originalTypes.Length ? _paragraphEmbeddingLevel : _levels[i];
            }
            var eos = DirectionFromLevel(Math.Max(nextLevel, _runLevel));


            // Rule W1
            var prevType = sos;
            for (i = 0; i < _runResultTypes.Length; i++)
            {
                var t = _runResultTypes[i];
                if (t == Directionality.NSM)
                {
                    _runResultTypes[i] = prevType;
                }
                else
                {
                    if (t == Directionality.LRI || t == Directionality.RLI || t == Directionality.FSI || t == Directionality.PDI)
                    {
                        prevType = Directionality.ON;
                    }
                    else
                    {
                        prevType = t;
                    }
                }
            }

            // Rule W2
            for (i = 0; i < _runResultTypes.Length; i++)
            {
                if (_runResultTypes[i] == Directionality.EN)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var t = _runResultTypes[j];
                        if (t == Directionality.L || t == Directionality.R || t == Directionality.AL)
                        {
                            if (t == Directionality.AL)
                            {
                                _runResultTypes[i] = Directionality.AN;
                            }
                            break;
                        }
                    }
                }
            }

            // Rule W3
            for (i = 0; i < _runResultTypes.Length; i++)
            {
                if (_runResultTypes[i] == Directionality.AL)
                {
                    _runResultTypes[i] = Directionality.R;
                }
            }

            // Rule W4
            for (i = 1; i < _runResultTypes.Length - 1; ++i)
            {
                ref var rt = ref _runResultTypes[i];
                if (rt == Directionality.ES)
                {
                    var prevSepType = _runResultTypes[i - 1];
                    var succSepType = _runResultTypes[i + 1];

                    if (prevSepType == Directionality.EN && succSepType == Directionality.EN)
                    {
                        // ES between EN and EN
                        rt = Directionality.EN;
                    }
                }
                else if (rt == Directionality.CS)
                {
                    var prevSepType = _runResultTypes[i - 1];
                    var succSepType = _runResultTypes[i + 1];

                    if ((prevSepType == Directionality.AN && succSepType == Directionality.AN) ||
                         (prevSepType == Directionality.EN && succSepType == Directionality.EN))
                    {
                        // CS between (AN and AN) or (EN and EN)
                        rt = Directionality.AN;
                    }
                }
            }

            // Rule W5
            for (i = 0; i < _runResultTypes.Length; ++i)
            {
                if (_runResultTypes[i] == Directionality.ET)
                {
                    // locate end of sequence
                    int seqStart = i;
                    int seqEnd = i;
                    while (seqEnd < _runResultTypes.Length && _runResultTypes[i] == Directionality.ET)
                        seqEnd++;

                    // Preceeded by EN or followed by EN?
                    if ((seqStart == 0 ? sos : _runResultTypes[seqStart - 1]) == Directionality.EN
                        || (seqEnd == _runResultTypes.Length ? eos : _runResultTypes[seqEnd]) == Directionality.EN)
                    {
                        // Change the entire range
                        for (int j = seqStart; i < seqEnd; ++i)
                        {
                            _runResultTypes[i] = Directionality.EN;
                        }
                    }

                    // continue at end of sequence
                    i = seqEnd;
                }
            }

            // Rule W6.
            for (i = 0; i < _runResultTypes.Length; ++i)
            {
                ref var t = ref _runResultTypes[i];
                if (t == Directionality.ES || t == Directionality.ET || t == Directionality.CS)
                {
                    t = Directionality.ON;
                }
            }

            // Rule W7.
            var prevStrongType = sos;
            for (i = 0; i < _runResultTypes.Length; ++i)
            {
                ref var rt = ref _runResultTypes[i];
                if (rt == Directionality.EN)
                {
                    // If prev strong type was an L change this to L too
                    if (prevStrongType == Directionality.L)
                    {
                        _runResultTypes[i] = Directionality.L;
                    }
                }

                // Remember previous strong type (NB: AL should already be changed to R)
                if (rt == Directionality.L || rt == Directionality.R)
                {
                    prevStrongType = rt;
                }
            }

            // Rule N0 - process bracket pairs
            foreach (var pb in LocatePairedBrackets())
            {
                var dir = InspectPairedBracket(pb);

                // Case "d" - no strong types in the brackets, ignore
                if (dir == Directionality.ON)
                {
                    continue;
                }

                // Case "b" - strong type found that matches the embedding direction
                if ((dir == Directionality.L || dir == Directionality.R) && dir == _runDirection)
                {
                    SetPairedBracketDirection(pb, dir);
                    continue;
                }

                // Case "c" - found opposite strong type found, look before to establish context
                dir = InspectBeforePairedBracket(pb, sos);
                if (dir == _runDirection || dir == Directionality.ON)
                {
                    dir = _runDirection;
                }
                SetPairedBracketDirection(pb, dir);
            }


            // Rules N1 and N2 - resolve neutral types
            for (i = 0; i < _runResultTypes.Length; ++i)
            {
                var t = _runResultTypes[i];
                if (IsNeutralType(t))
                {
                    // locate end of sequence
                    int seqStart = i;
                    int seqEnd = i;
                    while (seqEnd < _runResultTypes.Length && IsNeutralType(_runResultTypes[i]))
                        seqEnd++;

                    // Work out preceding type
                    Directionality typeBefore;
                    if (seqStart == 0)
                    {
                        typeBefore = sos;
                    }
                    else
                    {
                        typeBefore = _runResultTypes[seqStart - 1];
                        if (typeBefore == Directionality.AN || typeBefore == Directionality.EN)
                        {
                            typeBefore = Directionality.R;
                        }
                    }

                    // Work out the following type
                    Directionality typeAfter;
                    if (seqEnd == _runResultTypes.Length)
                    {
                        typeAfter = eos;
                    }
                    else
                    {
                        typeAfter = _runResultTypes[seqEnd];
                        if (typeAfter == Directionality.AN || typeAfter == Directionality.EN)
                        {
                            typeAfter = Directionality.R;
                        }
                    }

                    // Work out the final resolved type
                    Directionality resolvedType;
                    if (typeBefore == typeAfter)
                    {
                        // Rule N1.
                        resolvedType = typeBefore;
                    }
                    else
                    {
                        // Rule N2.
                        resolvedType = _runDirection;
                    }

                    for (int j = seqStart; j < seqEnd; j++)
                    {
                        _runResultTypes[j] = resolvedType;
                    }

                    // continue after this run
                    i = seqEnd;
                }
            }

            // Resolve implicit types
            if ((_runLevel & 0x01) == 0)
            {
                // Rule I1 - even
                for (i = 0; i < _runResultTypes.Length; i++)
                {
                    var t = _runResultTypes[i];
                    ref var l = ref _runLevels[i];
                    if (t == Directionality.R)
                        l++;
                    else if (t == Directionality.AN || t == Directionality.EN)
                        l += 2;
                }
            }
            else
            {
                // Rule I2 - odd
                for (i = 0; i < _runResultTypes.Length; i++)
                {
                    var t = _runResultTypes[i];
                    ref var l = ref _runLevels[i];
                    if (t != Directionality.R)
                        l++;
                }
            }
        }


        /// <summary>
        /// Check if a a directionality is neutral for rules N1 and N2
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsNeutralType(Directionality dir)
        {
            switch (dir)
            {
                case Directionality.B:
                case Directionality.S:
                case Directionality.WS:
                case Directionality.ON:
                case Directionality.RLI:
                case Directionality.LRI:
                case Directionality.FSI:
                case Directionality.PDI:
                    return true;
            }

            return false;
        }

        const int MAX_PAIRING_DEPTH = 63;


        /// <summary>
        /// Locate all pair brackets in the current isolating run
        /// </summary>
        /// <returns>A sorted list of BracketPairs</returns>
        List<BracketPair> LocatePairedBrackets()
        {
            var openers = new List<int>();
            var pairPositions = new SortedSet<BracketPair>();

            for (int ich = 0; ich < _runResultTypes.Length; ich++)
            {
                switch (_runPairedBracketTypes[ich])
                {
                    case PairedBracketType.o:
                        if (openers.Count == MAX_PAIRING_DEPTH)
                            return pairPositions.ToList();

                        openers.Insert(0, ich);
                        break;

                    case PairedBracketType.c:
                        // see if there is a match
                        for (int i = 0; i < openers.Count - 1; i++)
                        {
                            if (_runPairedBracketValues[i] == _runPairedBracketValues[openers[i]])
                            {
                                // Add this paired bracket set
                                pairPositions.Add(new BracketPair(openers[i], ich));

                                // remove up to and including matched opener
                                openers.RemoveRange(0, i + 1);
                                break;
                            }
                        }
                        break;
                }
            }

            return pairPositions.ToList();
        }

        Directionality InspectPairedBracket(BracketPair pb)
        {
            var dirEmbed = DirectionFromLevel(_runLevel);
            var dirOpposite = Directionality.ON;
            for (int ich = pb.Opener + 1; ich < pb.Closer; ich++)
            {
                var dir = GetStrongTypeN0(_runResultTypes[ich]);
                if (dir == Directionality.ON)
                    continue;
                if (dir == dirEmbed)
                    return dir;
                dirOpposite = dir;
            }
            return dirOpposite;
        }

        // Look for a strong type before a paired bracket
        Directionality InspectBeforePairedBracket(BracketPair pb, Directionality sos)
        {
            for (int ich = pb.Opener - 1; ich >= 0; --ich)
            {
                var dir = GetStrongTypeN0(_runResultTypes[ich]);
                if (dir != Directionality.ON)
                    return dir;
            }
            return sos;
        }


        /// <summary>
        /// Sets the direction of a bracket pair, including setting the direction of NSM's inside
        /// the brackets and following
        /// </summary>
        /// <param name="pb">The paired brackets</param>
        /// <param name="dir">The resolved direction for the bracket pair</param>
        void SetPairedBracketDirection(BracketPair pb, Directionality dir)
        {
            // Set the direction of the brackets
            _runResultTypes[pb.Opener] = dir;
            _runResultTypes[pb.Closer] = dir;

            // Set the directionality of NSM's inside the brackets
            for (int i = pb.Opener + 1; i < pb.Closer; i++)
            {
                if (_runOriginalTypes[i] == Directionality.NSM)
                    _runOriginalTypes[i] = dir;
                else
                    break;
            }

            // Set the directionality of NSM's following the brackets
            for (int i = pb.Closer + 1; i < _runResultTypes.Length; i++)
            {
                if (_runOriginalTypes[i] == Directionality.NSM)
                    _runResultTypes[i] = dir;
                else
                    break;
            }
        }

        /// <summary>
        /// Maps a direction to a strong type for rule N0
        /// </summary>
        /// <param name="dir">The direction to map</param>
        /// <returns>A strong direction - R, L or ON</returns>
        Directionality GetStrongTypeN0(Directionality dir)
        {
            switch (dir)
            {
                case Directionality.EN:
                case Directionality.AN:
                case Directionality.AL:
                case Directionality.R:
                    return Directionality.R;
                case Directionality.L:
                    return Directionality.L;
                default:
                    return Directionality.ON;
            }
        }



        /// <summary>
        /// Hold the start and end index of a pair of brackets
        /// </summary>
        struct BracketPair : IEquatable<BracketPair>, IComparable<BracketPair>
        {
            private int ichOpener;
            private int ichCloser;

            public BracketPair(int ichOpener, int ichCloser)
            {
                this.ichOpener = ichOpener;
                this.ichCloser = ichCloser;
            }

            public override string ToString()
            {
                return "(" + ichOpener + ", " + ichCloser + ")";
            }

            public override int GetHashCode()
            {
                return ichOpener.GetHashCode() ^ ichCloser.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is BracketPair && Equals((BracketPair)obj);
            }

            public bool Equals(BracketPair p)
            {
                return ichOpener == p.ichOpener && ichCloser == p.ichCloser;
            }

            public int CompareTo(BracketPair other)
            {
                if (this.ichOpener == other.ichOpener)
                    return 0;
                if (this.ichOpener < other.ichOpener)
                    return -1;
                else
                    return 1;
            }

            public int Opener => ichOpener;
            public int Closer => ichCloser;
        }


        void ResetWhitespaceLevels()
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                var t = _originalTypes[i];
                if (t == Directionality.B || t == Directionality.S)
                {
                    // Rule L1, clauses one and two.
                    _levels[i] = _paragraphEmbeddingLevel;

                    // Rule L1, clause three.
                    for (int j = i - 1; j >= 0; --j)
                    {
                        if (IsWhitespace(_originalTypes[j]))
                        { // including format
                          // codes
                            _levels[j] = _paragraphEmbeddingLevel;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // Rule L1, clause four.
            for (int j = _levels.Length - 1; j >= 0; j--)
            {
                if (IsWhitespace(_originalTypes[j]))
                { // including format codes
                    _levels[j] = _paragraphEmbeddingLevel;
                }
                else
                {
                    break;
                }
            }
        }

        static bool IsWhitespace(Directionality biditype)
        {
            switch (biditype)
            {
                case Directionality.LRE:
                case Directionality.RLE:
                case Directionality.LRO:
                case Directionality.RLO:
                case Directionality.PDF:
                case Directionality.LRI:
                case Directionality.RLI:
                case Directionality.FSI:
                case Directionality.PDI:
                case Directionality.BN:
                case Directionality.WS:
                    return true;
                default:
                    return false;
            }
        }

    }
}
