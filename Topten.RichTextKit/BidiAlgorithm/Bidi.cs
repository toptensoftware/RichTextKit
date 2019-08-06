/* 
 * Ported from https://www.unicode.org/Public/PROGRAMS/BidiReferenceJava/BidiReference.java
 * /

/*
 * Last Revised: 2016-09-21
 *
 * Credits:
 * Originally written by Doug Felt
 * 
 * Updated for Unicode 6.3 by Roozbeh Pournader, with feedback by Aharon Lanin
 * 
 * Updated by Asmus Freytag to implement the Paired Bracket Algorithm (PBA)
 *
 * Updated for Unicode 8.0 by Deepak Jois, with feedback from Ken Whistler
 *
 * Disclaimer and legal rights:
 * (C) Copyright IBM Corp. 1999, All Rights Reserved
 * (C) Copyright Google Inc. 2013, All Rights Reserved
 * (C) Copyright ASMUS, Inc. 2013. All Rights Reserved
 * (C) Copyright Deepak Jois 2016, All Rights Reserved
 *
 * Distributed under the Terms of Use in http://www.unicode.org/copyright.html.
 */


/*
 * Revision info (2016-09-21):
 * Changes to support updated rules X5a,X5b and X6a in Unicode 8.0
 *
 * Revision info (2013-09-16):
 * Changed MAX_DEPTH to 125
 * 
 * Revision info (2013-06-02):
 * <p>
 * The core part of the Unicode Paired Bracket Algorithm (PBA) 
 * is implemented in a new BidiPBAReference class.
 * <p>
 * Changed convention for default paragraph embedding level from -1 to 2.
 */


/*
 * Reference implementation of the Unicode Bidirectional Algorithm (UAX #9).
 *
 * <p>
 * This implementation is not optimized for performance. It is intended as a
 * reference implementation that closely follows the specification of the
 * Bidirectional Algorithm in The Unicode Standard version 6.3.
 * <p>
 * <b>Input:</b><br>
 * There are two levels of input to the algorithm, since clients may prefer to
 * supply some information from out-of-band sources rather than relying on the
 * default behavior.
 * <ol>
 * <li>Bidi class array
 * <li>Bidi class array, with externally supplied base line direction
 * </ol>
 * <p>
 * <b>Output:</b><br>
 * Output is separated into several stages as well, to better enable clients to
 * evaluate various aspects of implementation conformance.
 * <ol>
 * <li>levels array over entire paragraph
 * <li>reordering array over entire paragraph
 * <li>levels array over line
 * <li>reordering array over line
 * </ol>
 * Note that for conformance to the Unicode Bidirectional Algorithm,
 * implementations are only required to generate correct reordering and
 * character directionality (odd or even levels) over a line. Generating
 * identical level arrays over a line is not required. Bidi explicit format
 * codes (LRE, RLE, LRO, RLO, PDF) and BN can be assigned arbitrary levels and
 * positions as long as the rest of the input is properly reordered.
 * <p>
 * As the algorithm is defined to operate on a single paragraph at a time, this
 * implementation is written to handle single paragraphs. Thus rule P1 is
 * presumed by this implementation-- the data provided to the implementation is
 * assumed to be a single paragraph, and either contains no 'B' codes, or a
 * single 'B' code at the end of the input. 'B' is allowed as input to
 * illustrate how the algorithm assigns it a level.
 * <p>
 * Also note that rules L3 and L4 depend on the rendering engine that uses the
 * result of the bidi algorithm. This implementation assumes that the rendering
 * engine expects combining marks in visual order (e.g. to the left of their
 * base character in RTL runs) and that it adjusts the glyphs used to render
 * mirrored characters that are in RTL runs so that they render appropriately.
 *
 * @author Doug Felt
 * @author Roozbeh Pournader
 * @author Asmus Freytag
 * @author Deepak Jois
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    class Bidi
    {
        //
        // Input
        //

        public Bidi(BidiData data) : 
            this(data.Directionality, data.PairedBracketTypes, data.PairedBracketValues, (byte)data.ParagraphEmbeddingLevel)
        {
        }

        /*
         * Initialize using several arrays, then run the algorithm
         * @param types
         *            Array of types ranging from TYPE_MIN to TYPE_MAX inclusive 
         *            and representing the direction codes of the characters in the text.
         * @param pairTypes
         * 			  Array of paired bracket types ranging from 0 (none) to 2 (closing)
         * 			  of the characters
         * @param pairValues
         * 			  Array identifying which set of matching bracket characters
         * 			  as defined in BidiPBAReference (note, both opening and closing
         * 			  bracket get the same value if they are part of the same canonical "set"
         * 			  or pair)
         */
        public Bidi(Slice<Directionality> types, Slice<PairedBracketType> pairTypes, Slice<int> pairValues)
        {
            validateTypes(types);
            validatePbTypes(pairTypes);
            validatePbValues(pairValues, pairTypes);

            _initialTypes = types.ToArray(); // client type array remains unchanged
            this._pairTypes = pairTypes;
            this._pairValues = pairValues;

            runAlgorithm();
        }

        /*
         * Initialize using several arrays of direction and other types and an externally supplied
         * paragraph embedding level. The embedding level may be  0, 1 or 2.
         * <p>
         * 2 means to apply the default algorithm (rules P2 and P3), 0 is for LTR
         * paragraphs, and 1 is for RTL paragraphs.
         *
         * @param types
         *            the types array
         * @param pairTypes
         *           the paired bracket types array
         * @param pairValues
         * 			 the paired bracket values array
         * @param paragraphEmbeddingLevel
         *            the externally supplied paragraph embedding level.
         */
        public Bidi(Slice<Directionality> types, Slice<PairedBracketType> pairTypes, Slice<int> pairValues, byte paragraphEmbeddingLevel)
        {
            validateTypes(types);
            validatePbTypes(pairTypes);
            validatePbValues(pairValues, pairTypes);
            validateParagraphEmbeddingLevel(paragraphEmbeddingLevel);

            _initialTypes = types.ToArray(); // client type array remains unchanged
            this._paragraphEmbeddingLevel = paragraphEmbeddingLevel;
            this._pairTypes = pairTypes;
            this._pairValues = pairValues;

            runAlgorithm();
        }


        private const byte implicitEmbeddingLevel = 2; // level will be determined implicitly
        private Directionality[] _initialTypes;
        private byte _paragraphEmbeddingLevel = implicitEmbeddingLevel;
        private int _textLength; // for convenience
        private Directionality[] _resultTypes; // for paragraph, not lines
        private byte[] _resultLevels; // for paragraph, not lines

        // Get the final computed directionality of each character
        public Directionality[] Result => _resultTypes;

        public byte[] ResultLevels => _resultLevels;

        public struct Run
        {
            public Directionality Direction;
            public int Start;
            public int Length;
            public int End => Start + Length;

            public override string ToString()
            {
                return $"{Start} - {End} - {Direction}";
            }
        }

        public IEnumerable<Run> Runs
        {
            get
            {
                if (_resultTypes.Length == 0)
                    yield break;

                int startRun = 0;
                Directionality runDir = _resultTypes[0];
                for (int i = 1; i < _resultTypes.Length; i++)
                {
                    if (_resultTypes[i] == runDir)
                        continue;

                    // End of this run
                    yield return new Run()
                    {
                        Direction = runDir,
                        Start = startRun,
                        Length = i - startRun,
                    };

                    // Move to next run
                    startRun = i;
                    runDir = _resultTypes[i];
                }

                yield return new Run()
                {
                    Direction = runDir,
                    Start = startRun,
                    Length = _resultTypes.Length - startRun,
                };
            }
        }

        /*
         * Index of matching PDI for isolate initiator characters. For other
         * characters, the value of matchingPDI will be set to -1. For isolate
         * initiators with no matching PDI, matchingPDI will be set to the length of
         * the input string.
         */
        private int[] _matchingPDI;

        /*
         * Index of matching isolate initiator for PDI characters. For other
         * characters, and for PDIs with no matching isolate initiator, the value of
         * matchingIsolateInitiator will be set to -1.
         */
        private int[] _matchingIsolateInitiator;

        /*
         * Arrays of properties needed for paired bracket evaluation in N0
         */
        private Slice<PairedBracketType> _pairTypes; // paired Bracket types for paragraph
        private Slice<int> _pairValues; // paired Bracket values for paragraph

        public BidiPBA _pba; // to allow access to internal pba state for diagnostics


        /* Shorthand names of bidi type values, for error reporting. */
        public static string[] typenames = {
            "L",
            "LRE",
            "LRO",
            "R",
            "AL",
            "RLE",
            "RLO",
            "PDF",
            "EN",
            "ES",
            "ET",
            "AN",
            "CS",
            "NSM",
            "BN",
            "B",
            "S",
            "WS",
            "ON",
            "LRI",
            "RLI",
            "FSI",
            "PDI"
        };

        /*
         * The algorithm. Does not include line-based processing (Rules L1, L2).
         * These are applied later in the line-based phase of the algorithm.
         */
        private void runAlgorithm()
        {
            _textLength = _initialTypes.Length;

            // Initialize output types.
            // Result types initialized to input types.
            _resultTypes = _initialTypes.ToArray();

            // Preprocessing to find the matching isolates
            determineMatchingIsolates();

            // 1) determining the paragraph level
            // Rule P1 is the requirement for entering this algorithm.
            // Rules P2, P3.
            // If no externally supplied paragraph embedding level, use default.
            if (_paragraphEmbeddingLevel == implicitEmbeddingLevel)
            {
                _paragraphEmbeddingLevel = determineParagraphEmbeddingLevel(0, _textLength);
            }

            // Initialize result levels to paragraph embedding level.
            _resultLevels = new byte[_textLength];
            setLevels(_resultLevels, 0, _textLength, _paragraphEmbeddingLevel);

            // 2) Explicit levels and directions
            // Rules X1-X8.
            determineExplicitEmbeddingLevels();

            // Rule X9.
            // We do not remove the embeddings, the overrides, the PDFs, and the BNs
            // from the string explicitly. But they are not copied into isolating run
            // sequences when they are created, so they are removed for all
            // practical purposes.

            // Rule X10.
            // Run remainder of algorithm one isolating run sequence at a time
            IsolatingRunSequence[] sequences = determineIsolatingRunSequences();

            for (int i = 0; i < sequences.Length; ++i)
            {
                IsolatingRunSequence sequence = sequences[i];
                // 3) resolving weak types
                // Rules W1-W7.
                sequence.resolveWeakTypes();

                // 4a) resolving paired brackets
                // Rule N0
                sequence.resolvePairedBrackets();

                // 4b) resolving neutral types
                // Rules N1-N3.
                sequence.resolveNeutralTypes();

                // 5) resolving implicit embedding levels
                // Rules I1, I2.
                sequence.resolveImplicitLevels();

                // Apply the computed levels and types
                sequence.applyLevelsAndTypes();
            }

            // Assign appropriate levels to 'hide' LREs, RLEs, LROs, RLOs, PDFs, and
            // BNs. This is for convenience, so the resulting level array will have
            // a value for every character.
            assignLevelsToCharactersRemovedByX9();
        }

        /*
         * Determine the matching PDI for each isolate initiator and vice versa.
         * <p>
         * Definition BD9.
         * <p>
         * At the end of this function:
         * <ul>
         * <li>The member variable matchingPDI is set to point to the index of the
         * matching PDI character for each isolate initiator character. If there is
         * no matching PDI, it is set to the length of the input text. For other
         * characters, it is set to -1.
         * <li>The member variable matchingIsolateInitiator is set to point to the
         * index of the matching isolate initiator character for each PDI character.
         * If there is no matching isolate initiator, or the character is not a PDI,
         * it is set to -1.
         * </ul>
         */
        private void determineMatchingIsolates()
        {
            _matchingPDI = new int[_textLength];
            _matchingIsolateInitiator = new int[_textLength];

            for (int i = 0; i < _textLength; ++i)
            {
                _matchingIsolateInitiator[i] = -1;
            }

            for (int i = 0; i < _textLength; ++i)
            {
                _matchingPDI[i] = -1;

                var t = _resultTypes[i];
                if (t == Directionality.LRI || t == Directionality.RLI || t == Directionality.FSI)
                {
                    int depthCounter = 1;
                    for (int j = i + 1; j < _textLength; ++j)
                    {
                        var u = _resultTypes[j];
                        if (u == Directionality.LRI || u == Directionality.RLI || u == Directionality.FSI)
                        {
                            ++depthCounter;
                        }
                        else if (u == Directionality.PDI)
                        {
                            --depthCounter;
                            if (depthCounter == 0)
                            {
                                _matchingPDI[i] = j;
                                _matchingIsolateInitiator[j] = i;
                                break;
                            }
                        }
                    }
                    if (_matchingPDI[i] == -1)
                    {
                        _matchingPDI[i] = _textLength;
                    }
                }
            }
        }

        /*
         * Determines the paragraph level based on rules P2, P3. This is also used
         * in rule X5c to find if an FSI should resolve to LRI or RLI.
         *
         * @param startIndex
         *            the index of the beginning of the substring
         * @param endIndex
         *            the index of the character after the end of the string
         *
         * @return the resolved paragraph direction of the substring limited by
         *         startIndex and endIndex
         */
        private byte determineParagraphEmbeddingLevel(int startIndex, int endIndex)
        {
            var strongType = Directionality.Unknown; // unknown

            // Rule P2.
            for (int i = startIndex; i < endIndex; ++i)
            {
                var t = _resultTypes[i];
                if (t == Directionality.L || t == Directionality.AL || t == Directionality.R)
                {
                    strongType = t;
                    break;
                }
                else if (t == Directionality.FSI || t == Directionality.LRI || t == Directionality.RLI)
                {
                    i = _matchingPDI[i]; // skip over to the matching PDI
                    System.Diagnostics.Debug.Assert(i <= endIndex);
                }
            }

            // Rule P3.
            if (strongType == Directionality.Unknown)
            { // none found
              // default embedding level when no strong types found is 0.
                return 0;
            }
            else if (strongType == Directionality.L)
            {
                return 0;
            }
            else
            { // AL, R
                return 1;
            }
        }

        public const int MAX_DEPTH = 125;

        // This stack will store the embedding levels and override and isolated
        // statuses
        private class directionalStatusStack
        {
            private int stackCounter = 0;
            private byte[] embeddingLevelStack = new byte[MAX_DEPTH + 1];
            private Directionality[] overrideStatusStack = new Directionality[MAX_DEPTH + 1];
            private bool[] isolateStatusStack = new bool[MAX_DEPTH + 1];

            public void empty()
            {
                stackCounter = 0;
            }

            public void push(byte level, Directionality overrideStatus, bool isolateStatus)
            {
                embeddingLevelStack[stackCounter] = level;
                overrideStatusStack[stackCounter] = overrideStatus;
                isolateStatusStack[stackCounter] = isolateStatus;
                ++stackCounter;
            }

            public void pop()
            {
                --stackCounter;
            }

            public int depth()
            {
                return stackCounter;
            }

            public byte lastEmbeddingLevel()
            {
                return embeddingLevelStack[stackCounter - 1];
            }

            public Directionality lastDirectionalOverrideStatus()
            {
                return overrideStatusStack[stackCounter - 1];
            }

            public bool lastDirectionalIsolateStatus()
            {
                return isolateStatusStack[stackCounter - 1];
            }
        }

        /*
         * Determine explicit levels using rules X1 - X8
         */
        private void determineExplicitEmbeddingLevels()
        {
            directionalStatusStack stack = new directionalStatusStack();
            int overflowIsolateCount, overflowEmbeddingCount, validIsolateCount;

            // Rule X1.
            stack.empty();
            stack.push(_paragraphEmbeddingLevel, Directionality.ON, false);
            overflowIsolateCount = 0;
            overflowEmbeddingCount = 0;
            validIsolateCount = 0;
            for (int i = 0; i < _textLength; ++i)
            {
                var t = _resultTypes[i];

                // Rules X2, X3, X4, X5, X5a, X5b, X5c
                switch (t)
                {
                    case Directionality.RLE:
                    case Directionality.LRE:
                    case Directionality.RLO:
                    case Directionality.LRO:
                    case Directionality.RLI:
                    case Directionality.LRI:
                    case Directionality.FSI:
                        bool isIsolate = (t == Directionality.RLI || t == Directionality.LRI || t == Directionality.FSI);
                        bool isRTL = (t == Directionality.RLE || t == Directionality.RLO || t == Directionality.RLI);
                        // override if this is an FSI that resolves to RLI
                        if (t == Directionality.FSI)
                        {
                            isRTL = (determineParagraphEmbeddingLevel(i + 1, _matchingPDI[i]) == 1);
                        }

                        if (isIsolate)
                        {
                            _resultLevels[i] = stack.lastEmbeddingLevel();
                            if (stack.lastDirectionalOverrideStatus() != Directionality.ON)
                            {
                                _resultTypes[i] = stack.lastDirectionalOverrideStatus();
                            }
                        }

                        byte newLevel;
                        if (isRTL)
                        {
                            // least greater odd
                            newLevel = (byte)((stack.lastEmbeddingLevel() + 1) | 1);
                        }
                        else
                        {
                            // least greater even
                            newLevel = (byte)((stack.lastEmbeddingLevel() + 2) & ~1);
                        }

                        if (newLevel <= MAX_DEPTH && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            if (isIsolate)
                            {
                                ++validIsolateCount;
                            }
                            // Push new embedding level, override status, and isolated
                            // status.
                            // No check for valid stack counter, since the level check
                            // suffices.
                            stack.push(
                                    newLevel,
                                    t == Directionality.LRO ? Directionality.L : t == Directionality.RLO ? Directionality.R : Directionality.ON,
                                    isIsolate);

                            // Not really part of the spec
                            if (!isIsolate)
                            {
                                _resultLevels[i] = newLevel;
                            }
                        }
                        else
                        {
                            // This is an invalid explicit formatting character,
                            // so apply the "Otherwise" part of rules X2-X5b.
                            if (isIsolate)
                            {
                                ++overflowIsolateCount;
                            }
                            else
                            { // !isIsolate
                                if (overflowIsolateCount == 0)
                                {
                                    ++overflowEmbeddingCount;
                                }
                            }
                        }
                        break;

                    // Rule X6a
                    case Directionality.PDI:
                        if (overflowIsolateCount > 0)
                        {
                            --overflowIsolateCount;
                        }
                        else if (validIsolateCount == 0)
                        {
                            // do nothing
                        }
                        else
                        {
                            overflowEmbeddingCount = 0;
                            while (!stack.lastDirectionalIsolateStatus())
                            {
                                stack.pop();
                            }
                            stack.pop();
                            --validIsolateCount;
                        }
                        _resultLevels[i] = stack.lastEmbeddingLevel();
                        break;

                    // Rule X7
                    case Directionality.PDF:
                        // Not really part of the spec
                        _resultLevels[i] = stack.lastEmbeddingLevel();

                        if (overflowIsolateCount > 0)
                        {
                            // do nothing
                        }
                        else if (overflowEmbeddingCount > 0)
                        {
                            --overflowEmbeddingCount;
                        }
                        else if (!stack.lastDirectionalIsolateStatus() && stack.depth() >= 2)
                        {
                            stack.pop();
                        }
                        else
                        {
                            // do nothing
                        }
                        break;

                    case Directionality.B:
                        // Rule X8.

                        // These values are reset for clarity, in this implementation B
                        // can only occur as the last code in the array.
                        stack.empty();
                        overflowIsolateCount = 0;
                        overflowEmbeddingCount = 0;
                        validIsolateCount = 0;
                        _resultLevels[i] = _paragraphEmbeddingLevel;
                        break;

                    default:
                        _resultLevels[i] = stack.lastEmbeddingLevel();
                        if (stack.lastDirectionalOverrideStatus() != Directionality.ON)
                        {
                            _resultTypes[i] = stack.lastDirectionalOverrideStatus();
                        }
                        break;
                }
            }
        }

        private class IsolatingRunSequence
        {
            private int[] indexes; // indexes to the original string
            private Directionality[] types; // type of each character using the index
            private byte[] resolvedLevels; // resolved levels after application of
                                           // rules
            private int length; // length of isolating run sequence in
                                // characters
            private byte level;
            private Directionality sos, eos;
            private Bidi owner;

            /*
             * Rule X10, second bullet: Determine the start-of-sequence (sos) and end-of-sequence (eos) types,
             * 			 either L or R, for each isolating run sequence.
             * @param inputIndexes
             */
            public IsolatingRunSequence(Bidi owner, int[] inputIndexes)
            {
                this.owner = owner;
                indexes = inputIndexes;
                length = indexes.Length;

                types = new Directionality[length];
                for (int i = 0; i < length; ++i)
                {
                    types[i] = owner._resultTypes[indexes[i]];
                }

                // assign level, sos and eos
                level = owner._resultLevels[indexes[0]];

                int prevChar = indexes[0] - 1;
                while (prevChar >= 0 && isRemovedByX9(owner._initialTypes[prevChar]))
                {
                    --prevChar;
                }
                byte prevLevel = prevChar >= 0 ? owner._resultLevels[prevChar] : owner._paragraphEmbeddingLevel;
                sos = typeForLevel(Math.Max(prevLevel, level));

                var lastType = types[length - 1];
                byte succLevel;
                if (lastType == Directionality.LRI || lastType == Directionality.RLI || lastType == Directionality.FSI)
                {
                    succLevel = owner._paragraphEmbeddingLevel;
                }
                else
                {
                    int limit = indexes[length - 1] + 1; // the first character
                                                         // after the end of
                                                         // run sequence
                    while (limit < owner._textLength && isRemovedByX9(owner._initialTypes[limit]))
                    {
                        ++limit;
                    }
                    succLevel = limit < owner._textLength ? owner._resultLevels[limit] : owner._paragraphEmbeddingLevel;
                }
                eos = typeForLevel(Math.Max(succLevel, level));
            }

            /*
             * Resolving bidi paired brackets  Rule N0
             */

            public void resolvePairedBrackets()
            {
                owner._pba = new BidiPBA();
                owner._pba.resolvePairedBrackets(
                    new Slice<int>(indexes), 
                    new Slice<Directionality>(owner._initialTypes), 
                    new Slice<Directionality>(types), 
                    owner._pairTypes,
                    owner._pairValues,
                    sos, 
                    level
                    );
            }


            /*
             * Resolving weak types Rules W1-W7.
             *
             * Note that some weak types (EN, AN) remain after this processing is
             * complete.
             */
            public void resolveWeakTypes()
            {

                // on entry, only these types remain
                assertOnly(new Directionality[] {
                Directionality.L,
                Directionality.R,
                Directionality.AL,
                Directionality.EN,
                Directionality.ES,
                Directionality.ET,
                Directionality.AN,
                Directionality.CS,
                Directionality.B,
                Directionality.S,
                Directionality.WS,
                Directionality.ON,
                Directionality.NSM,
                Directionality.LRI,
                Directionality.RLI,
                Directionality.FSI,
                Directionality.PDI
            });

                // Rule W1.
                // Changes all NSMs.
                var preceedingCharacterType = sos;
                for (int i = 0; i < length; ++i)
                {
                    var t = types[i];
                    if (t == Directionality.NSM)
                    {
                        types[i] = preceedingCharacterType;
                    }
                    else
                    {
                        if (t == Directionality.LRI || t == Directionality.RLI || t == Directionality.FSI || t == Directionality.PDI)
                        {
                            preceedingCharacterType = Directionality.ON;
                        }
                        preceedingCharacterType = t;
                    }
                }

                // Rule W2.
                // EN does not change at the start of the run, because sos != AL.
                for (int i = 0; i < length; ++i)
                {
                    if (types[i] == Directionality.EN)
                    {
                        for (int j = i - 1; j >= 0; --j)
                        {
                            var t = types[j];
                            if (t == Directionality.L || t == Directionality.R || t == Directionality.AL)
                            {
                                if (t == Directionality.AL)
                                {
                                    types[i] = Directionality.AN;
                                }
                                break;
                            }
                        }
                    }
                }

                // Rule W3.
                for (int i = 0; i < length; ++i)
                {
                    if (types[i] == Directionality.AL)
                    {
                        types[i] = Directionality.R;
                    }
                }

                // Rule W4.
                // Since there must be values on both sides for this rule to have an
                // effect, the scan skips the first and last value.
                //
                // Although the scan proceeds left to right, and changes the type
                // values in a way that would appear to affect the computations
                // later in the scan, there is actually no problem. A change in the
                // current value can only affect the value to its immediate right,
                // and only affect it if it is ES or CS. But the current value can
                // only change if the value to its right is not ES or CS. Thus
                // either the current value will not change, or its change will have
                // no effect on the remainder of the analysis.

                for (int i = 1; i < length - 1; ++i)
                {
                    if (types[i] == Directionality.ES || types[i] == Directionality.CS)
                    {
                        var prevSepType = types[i - 1];
                        var succSepType = types[i + 1];
                        if (prevSepType == Directionality.EN && succSepType == Directionality.EN)
                        {
                            types[i] = Directionality.EN;
                        }
                        else if (types[i] == Directionality.CS && prevSepType == Directionality.AN && succSepType == Directionality.AN)
                        {
                            types[i] = Directionality.AN;
                        }
                    }
                }

                // Rule W5.
                for (int i = 0; i < length; ++i)
                {
                    if (types[i] == Directionality.ET)
                    {
                        // locate end of sequence
                        int runstart = i;
                        int runlimit = findRunLimit(runstart, length, new HashSet<Directionality>(new Directionality[] { Directionality.ET }));

                        // check values at ends of sequence
                        var t = runstart == 0 ? sos : types[runstart - 1];

                        if (t != Directionality.EN)
                        {
                            t = runlimit == length ? eos : types[runlimit];
                        }

                        if (t == Directionality.EN)
                        {
                            setTypes(runstart, runlimit, Directionality.EN);
                        }

                        // continue at end of sequence
                        i = runlimit;
                    }
                }

                // Rule W6.
                for (int i = 0; i < length; ++i)
                {
                    var t = types[i];
                    if (t == Directionality.ES || t == Directionality.ET || t == Directionality.CS)
                    {
                        types[i] = Directionality.ON;
                    }
                }

                // Rule W7.
                for (int i = 0; i < length; ++i)
                {
                    if (types[i] == Directionality.EN)
                    {
                        // set default if we reach start of run
                        var prevStrongType = sos;
                        for (int j = i - 1; j >= 0; --j)
                        {
                            var t = types[j];
                            if (t == Directionality.L || t == Directionality.R)
                            { // AL's have been changed to R
                                prevStrongType = t;
                                break;
                            }
                        }
                        if (prevStrongType == Directionality.L)
                        {
                            types[i] = Directionality.L;
                        }
                    }
                }
            }

            /*
             * 6) resolving neutral types Rules N1-N2.
             */
            public void resolveNeutralTypes()
            {

                // on entry, only these types can be in resultTypes
                assertOnly(new Directionality[] {
                Directionality.L,
                Directionality.R,
                Directionality.EN,
                Directionality.AN,
                Directionality.B,
                Directionality.S,
                Directionality.WS,
                Directionality.ON,
                Directionality.RLI,
                Directionality.LRI,
                Directionality.FSI,
                Directionality.PDI });

                for (int i = 0; i < length; ++i)
                {
                    var t = types[i];
                    if (t == Directionality.WS || t == Directionality.ON || t == Directionality.B
                            || t == Directionality.S || t == Directionality.RLI || t == Directionality.LRI
                            || t == Directionality.FSI || t == Directionality.PDI)
                    {
                        // find bounds of run of neutrals
                        int runstart = i;
                        int runlimit = findRunLimit(runstart, length, new HashSet<Directionality>(new Directionality[] {
                            Directionality.B,
                            Directionality.S,
                            Directionality.WS,
                            Directionality.ON,
                            Directionality.RLI,
                            Directionality.LRI,
                            Directionality.FSI,
                            Directionality.PDI
                        }));

                        // determine effective types at ends of run
                        Directionality leadingType;
                        Directionality trailingType;

                        // Note that the character found can only be L, R, AN, or
                        // EN.
                        if (runstart == 0)
                        {
                            leadingType = sos;
                        }
                        else
                        {
                            leadingType = types[runstart - 1];
                            if (leadingType == Directionality.AN || leadingType == Directionality.EN)
                            {
                                leadingType = Directionality.R;
                            }
                        }

                        if (runlimit == length)
                        {
                            trailingType = eos;
                        }
                        else
                        {
                            trailingType = types[runlimit];
                            if (trailingType == Directionality.AN || trailingType == Directionality.EN)
                            {
                                trailingType = Directionality.R;
                            }
                        }

                        Directionality resolvedType;
                        if (leadingType == trailingType)
                        {
                            // Rule N1.
                            resolvedType = leadingType;
                        }
                        else
                        {
                            // Rule N2.
                            // Notice the embedding level of the run is used, not
                            // the paragraph embedding level.
                            resolvedType = typeForLevel(level);
                        }

                        setTypes(runstart, runlimit, resolvedType);

                        // skip over run of (former) neutrals
                        i = runlimit;
                    }
                }
            }

            /*
             * 7) resolving implicit embedding levels Rules I1, I2.
             */
            public void resolveImplicitLevels()
            {

                // on entry, only these types can be in resultTypes
                assertOnly(new Directionality[] {
                Directionality.L,
                Directionality.R,
                Directionality.EN,
                Directionality.AN
            });

                resolvedLevels = new byte[length];
                owner.setLevels(resolvedLevels, 0, length, level);

                if ((level & 1) == 0)
                { // even level
                    for (int i = 0; i < length; ++i)
                    {
                        var t = types[i];
                        // Rule I1.
                        if (t == Directionality.L)
                        {
                            // no change
                        }
                        else if (t == Directionality.R)
                        {
                            resolvedLevels[i] += 1;
                        }
                        else
                        { // t == AN || t == EN
                            resolvedLevels[i] += 2;
                        }
                    }
                }
                else
                { // odd level
                    for (int i = 0; i < length; ++i)
                    {
                        var t = types[i];
                        // Rule I2.
                        if (t == Directionality.R)
                        {
                            // no change
                        }
                        else
                        { // t == L || t == AN || t == EN
                            resolvedLevels[i] += 1;
                        }
                    }
                }
            }

            /*
             * Applies the levels and types resolved in rules W1-I2 to the
             * resultLevels array.
             */
            public void applyLevelsAndTypes()
            {
                for (int i = 0; i < length; ++i)
                {
                    int originalIndex = indexes[i];
                    owner._resultTypes[originalIndex] = types[i];
                    owner._resultLevels[originalIndex] = resolvedLevels[i];
                }
            }

            /*
             * Return the limit of the run consisting only of the types in validSet
             * starting at index. This checks the value at index, and will return
             * index if that value is not in validSet.
             */
            private int findRunLimit(int index, int limit, HashSet<Directionality> validSet)
            {
                while (index < limit && validSet.Contains(types[index]))
                    index++;

                return index;

                /*
                loop: while (index < limit)
                {
                    var t = types[index];
                    if (validSet.Contains(t))
                    {
                        ++index;
                        goto loop;
                    }

                    // didn't find a match in validSet
                    return index;
                }
                return limit;
                */
            }

            /*
             * Set types from start up to (but not including) limit to newType.
             */
            private void setTypes(int start, int limit, Directionality newType)
            {
                for (int i = start; i < limit; ++i)
                {
                    types[i] = newType;
                }
            }

            /*
             * Algorithm validation. Assert that all values in types are in the
             * provided set.
             */
            private void assertOnly(Directionality[] codes)
            {
                for (int i = 0; i < length; ++i)
                {
                    var t = types[i];
                    if (!codes.Contains(t))
                        throw new InvalidOperationException("invalid bidi code " + typenames[(int)t] + " present in assertOnly at position " + indexes[i]);
                }
            }
        }

        static T[] copyArray<T>(T[] original, int length)
        {
            var newArray = new T[length];
            Array.Copy(original, newArray, Math.Min(length, original.Length));
            return newArray;
        }

        /*
         * Determines the level runs. Rule X9 will be applied in determining the
         * runs, in the way that makes sure the characters that are supposed to be
         * removed are not included in the runs.
         *
         * @return an array of level runs. Each level run is described as an array
         *         of indexes into the input string.
         */
        private int[][] determineLevelRuns()
        {
            // temporary array to hold the run
            int[] temporaryRun = new int[_textLength];
            // temporary array to hold the list of runs
            int[][] allRuns = new int[_textLength][];
            int numRuns = 0;

            byte currentLevel = (byte)0xFF;
            int runLength = 0;
            for (int i = 0; i < _textLength; ++i)
            {
                if (!isRemovedByX9(_initialTypes[i]))
                {
                    if (_resultLevels[i] != currentLevel)
                    { // we just encountered a
                      // new run
                      // Wrap up last run
                        if (currentLevel != 0xFF)
                        { // only wrap it up if there was a run
                            int[] run = copyArray(temporaryRun, runLength);
                            allRuns[numRuns] = run;
                            ++numRuns;
                        }
                        // Start new run
                        currentLevel = _resultLevels[i];
                        runLength = 0;
                    }
                    temporaryRun[runLength] = i;
                    ++runLength;
                }
            }
            // Wrap up the final run, if any
            if (runLength != 0)
            {
                int[] run = copyArray(temporaryRun, runLength);
                allRuns[numRuns] = run;
                ++numRuns;
            }

            return copyArray(allRuns, numRuns);
        }

        /*
         * Definition BD13. Determine isolating run sequences.
         *
         * @return an array of isolating run sequences.
         */
        private IsolatingRunSequence[] determineIsolatingRunSequences()
        {
            int[][] levelRuns = determineLevelRuns();
            int numRuns = levelRuns.Length;

            // Compute the run that each character belongs to
            int[] runForCharacter = new int[_textLength];
            for (int runNumber = 0; runNumber < numRuns; ++runNumber)
            {
                for (int i = 0; i < levelRuns[runNumber].Length; ++i)
                {
                    int characterIndex = levelRuns[runNumber][i];
                    runForCharacter[characterIndex] = runNumber;
                }
            }

            IsolatingRunSequence[] sequences = new IsolatingRunSequence[numRuns];
            int numSequences = 0;
            int[] currentRunSequence = new int[_textLength];
            for (int i = 0; i < levelRuns.Length; ++i)
            {
                int firstCharacter = levelRuns[i][0];
                if (_initialTypes[firstCharacter] != Directionality.PDI || _matchingIsolateInitiator[firstCharacter] == -1)
                {
                    int currentRunSequenceLength = 0;
                    int run = i;
                    do
                    {
                        // Copy this level run into currentRunSequence
                        Array.Copy(levelRuns[run], 0, currentRunSequence, currentRunSequenceLength, levelRuns[run].Length);
                        currentRunSequenceLength += levelRuns[run].Length;

                        int lastCharacter = currentRunSequence[currentRunSequenceLength - 1];
                        var lastType = _initialTypes[lastCharacter];
                        if ((lastType == Directionality.LRI || lastType == Directionality.RLI || lastType == Directionality.FSI) &&
                                _matchingPDI[lastCharacter] != _textLength)
                        {
                            run = runForCharacter[_matchingPDI[lastCharacter]];
                        }
                        else
                        {
                            break;
                        }
                    } while (true);

                    sequences[numSequences] = new IsolatingRunSequence(this,
                            copyArray(currentRunSequence, currentRunSequenceLength));
                    ++numSequences;
                }
            }
            return copyArray(sequences, numSequences);
        }

        /*
         * Assign level information to characters removed by rule X9. This is for
         * ease of relating the level information to the original input data. Note
         * that the levels assigned to these codes are arbitrary, they're chosen so
         * as to avoid breaking level runs.
         *
         * @return the length of the data (original length of types array supplied
         *         to constructor)
         */
        private int assignLevelsToCharactersRemovedByX9()
        {
            for (int i = 0; i < _initialTypes.Length; ++i)
            {
                var t = _initialTypes[i];
                if (t == Directionality.LRE || t == Directionality.RLE || t == Directionality.LRO
                        || t == Directionality.RLO || t == Directionality.PDF || t == Directionality.BN)
                {
                    _resultTypes[i] = t;
                    _resultLevels[i] = 0xFF;
                }
            }

            // now propagate forward the levels information (could have
            // propagated backward, the main thing is not to introduce a level
            // break where one doesn't already exist).

            if (_resultLevels[0] == 0xFF)
            {
                _resultLevels[0] = _paragraphEmbeddingLevel;
            }
            for (int i = 1; i < _initialTypes.Length; ++i)
            {
                if (_resultLevels[i] == 0xFF)
                {
                    _resultLevels[i] = _resultLevels[i - 1];
                }
            }

            // Embedding information is for informational purposes only
            // so need not be adjusted.

            return _initialTypes.Length;
        }

        //
        // Output
        //

        /*
         * Return levels array breaking lines at offsets in linebreaks. <br>
         * Rule L1.
         * <p>
         * The returned levels array contains the resolved level for each bidi code
         * passed to the constructor.
         * <p>
         * The linebreaks array must include at least one value. The values must be
         * in strictly increasing order (no duplicates) between 1 and the length of
         * the text, inclusive. The last value must be the length of the text.
         *
         * @param linebreaks
         *            the offsets at which to break the paragraph
         * @return the resolved levels of the text
         */
        public byte[] getLevels(int[] linebreaks)
        {

            // Note that since the previous processing has removed all
            // P, S, and WS values from resultTypes, the values referred to
            // in these rules are the initial types, before any processing
            // has been applied (including processing of overrides).
            //
            // This example implementation has reinserted explicit format codes
            // and BN, in order that the levels array correspond to the
            // initial text. Their final placement is not normative.
            // These codes are treated like WS in this implementation,
            // so they don't interrupt sequences of WS.

            validateLineBreaks(linebreaks, _textLength);

            byte[] result = _resultLevels.ToArray(); // will be returned to
                                                    // caller

            // don't worry about linebreaks since if there is a break within
            // a series of WS values preceding S, the linebreak itself
            // causes the reset.
            for (int i = 0; i < result.Length; ++i)
            {
                var t = _initialTypes[i];
                if (t == Directionality.B || t == Directionality.S)
                {
                    // Rule L1, clauses one and two.
                    result[i] = _paragraphEmbeddingLevel;

                    // Rule L1, clause three.
                    for (int j = i - 1; j >= 0; --j)
                    {
                        if (isWhitespace(_initialTypes[j]))
                        { // including format
                          // codes
                            result[j] = _paragraphEmbeddingLevel;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // Rule L1, clause four.
            int start = 0;
            for (int i = 0; i < linebreaks.Length; ++i)
            {
                int limit = linebreaks[i];
                for (int j = limit - 1; j >= start; --j)
                {
                    if (isWhitespace(_initialTypes[j]))
                    { // including format codes
                        result[j] = _paragraphEmbeddingLevel;
                    }
                    else
                    {
                        break;
                    }
                }

                start = limit;
            }

            return result;
        }

        /*
         * Return reordering array breaking lines at offsets in linebreaks.
         * <p>
         * The reordering array maps from a visual index to a logical index. Lines
         * are concatenated from left to right. So for example, the fifth character
         * from the left on the third line is
         *
         * <pre>
         * getReordering(linebreaks)[linebreaks[1] + 4]
         * </pre>
         *
         * (linebreaks[1] is the position after the last character of the second
         * line, which is also the index of the first character on the third line,
         * and adding four gets the fifth character from the left).
         * <p>
         * The linebreaks array must include at least one value. The values must be
         * in strictly increasing order (no duplicates) between 1 and the length of
         * the text, inclusive. The last value must be the length of the text.
         *
         * @param linebreaks
         *            the offsets at which to break the paragraph.
         */
        public int[] getReordering(int[] linebreaks)
        {
            validateLineBreaks(linebreaks, _textLength);

            byte[] levels = getLevels(linebreaks);

            return computeMultilineReordering(levels, linebreaks);
        }

        /*
         * Return multiline reordering array for a given level array. Reordering
         * does not occur across a line break.
         */
        private static int[] computeMultilineReordering(byte[] levels, int[] linebreaks)
        {
            int[] result = new int[levels.Length];

            int start = 0;
            for (int i = 0; i < linebreaks.Length; ++i)
            {
                int limit = linebreaks[i];

                byte[] templevels = new byte[limit - start];
                Array.Copy(levels, start, templevels, 0, templevels.Length);

                int[] temporder = computeReordering(templevels);
                for (int j = 0; j < temporder.Length; ++j)
                {
                    result[start + j] = temporder[j] + start;
                }

                start = limit;
            }

            return result;
        }

        /*
         * Return reordering array for a given level array. This reorders a single
         * line. The reordering is a visual to logical map. For example, the
         * leftmost char is string.charAt(order[0]). Rule L2.
         */
        private static int[] computeReordering(byte[] levels)
        {
            int lineLength = levels.Length;

            int[] result = new int[lineLength];

            // initialize order
            for (int i = 0; i < lineLength; ++i)
            {
                result[i] = i;
            }

            // locate highest level found on line.
            // Note the rules say text, but no reordering across line bounds is
            // performed, so this is sufficient.
            byte highestLevel = 0;
            byte lowestOddLevel = MAX_DEPTH + 2;
            for (int i = 0; i < lineLength; ++i)
            {
                byte level = levels[i];
                if (level > highestLevel)
                {
                    highestLevel = level;
                }
                if (((level & 1) != 0) && level < lowestOddLevel)
                {
                    lowestOddLevel = level;
                }
            }

            for (int level = highestLevel; level >= lowestOddLevel; --level)
            {
                for (int i = 0; i < lineLength; ++i)
                {
                    if (levels[i] >= level)
                    {
                        // find range of text at or above this level
                        int start = i;
                        int limit = i + 1;
                        while (limit < lineLength && levels[limit] >= level)
                        {
                            ++limit;
                        }

                        // reverse run
                        for (int j = start, k = limit - 1; j < k; ++j, --k)
                        {
                            int temp = result[j];
                            result[j] = result[k];
                            result[k] = temp;
                        }

                        // skip to end of level run
                        i = limit;
                    }
                }
            }

            return result;
        }

        /*
         * Return the base level of the paragraph.
         */
        public byte getBaseLevel()
        {
            return _paragraphEmbeddingLevel;
        }

        // --- internal utilities -------------------------------------------------

        /*
         * Return true if the type is considered a whitespace type for the line
         * break rules.
         */
        private static bool isWhitespace(Directionality biditype)
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

        /*
         * Return true if the type is one of the types removed in X9.
         * Made public so callers can duplicate the effect.
         */
        public static bool isRemovedByX9(Directionality biditype)
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

        /*
         * Return the strong type (L or R) corresponding to the level.
         */
        private static Directionality typeForLevel(int level)
        {
            return ((level & 0x1) == 0) ? Directionality.L : Directionality.R;
        }

        /*
         * Set levels from start up to (but not including) limit to newLevel.
         */
        private void setLevels(byte[] levels, int start, int limit, byte newLevel)
        {
            for (int i = start; i < limit; ++i)
            {
                levels[i] = newLevel;
            }
        }

        // --- input validation ---------------------------------------------------

        /*
         * Throw exception if type array is invalid.
         */
        private static void validateTypes(Slice<Directionality> types)
        {
            for (int i = 0; i < types.Length; ++i)
            {
                if (types[i] < Directionality.TYPE_MIN || types[i] > Directionality.TYPE_MAX)
                {
                    throw new ArgumentException("illegal type value at " + i + ": " + types[i]);
                }
            }
            for (int i = 0; i < types.Length - 1; ++i)
            {
                if (types[i] == Directionality.B)
                {
                    throw new ArgumentException("B type before end of paragraph at index: " + i);
                }
            }
        }

        /*
         * Throw exception if paragraph embedding level is invalid. Special
         * allowance for implicitEmbeddinglevel so that default processing of the
         * paragraph embedding level as implicit can still be performed when
         * using this API.
         */
        private static void validateParagraphEmbeddingLevel(byte paragraphEmbeddingLevel)
        {
            if (paragraphEmbeddingLevel != implicitEmbeddingLevel &&
                    paragraphEmbeddingLevel != 0 &&
                    paragraphEmbeddingLevel != 1)
            {
                throw new ArgumentException("illegal paragraph embedding level: " + paragraphEmbeddingLevel);
            }
        }

        /*
         * Throw exception if line breaks array is invalid.
         */
        private static void validateLineBreaks(int[] linebreaks, int textLength)
        {
            int prev = 0;
            for (int i = 0; i < linebreaks.Length; ++i)
            {
                int next = linebreaks[i];
                if (next <= prev)
                {
                    throw new ArgumentException("bad linebreak: " + next + " at index: " + i);
                }
                prev = next;
            }
            if (prev != textLength)
            {
                throw new ArgumentException("last linebreak must be at " + textLength);
            }
        }

        /*
         * Throw exception if pairTypes array is invalid
         */
        private static void validatePbTypes(Slice<PairedBracketType> pairTypes)
        {
            for (int i = 0; i < pairTypes.Length; ++i)
            {
                if (pairTypes[i] < PairedBracketType.n || pairTypes[i] > PairedBracketType.c)
                {
                    throw new ArgumentException("illegal pairType value at " + i + ": " + pairTypes[i]);
                }
            }
        }

        /*
         * Throw exception if pairValues array is invalid or doesn't match pairTypes in length
         * Unfortunately there's little we can do in terms of validating the values themselves
         */
        private static void validatePbValues(Slice<int> pairValues, Slice<PairedBracketType> pairTypes)
        {
            if (pairTypes.Length != pairValues.Length)
            {
                throw new ArgumentException("pairTypes is different length from pairValues");
            }
        }

        /*
         * static entry point for testing using several arrays of direction and other types and an externally supplied
         * paragraph embedding level. The embedding level may be 0, 1 or 2.
         * <p>
         * 2 means to apply the default algorithm (rules P2 and P3), 0 is for LTR
         * paragraphs, and 1 is for RTL paragraphs.
         *
         * @param types
         *            the directional types array
         * @param pairTypes
         *           the paired bracket types array
         * @param pairValues
         * 			 the paired bracket values array
         * @param paragraphEmbeddingLevel
         *            the externally supplied paragraph embedding level.
         */
        public static Bidi analyzeInput(
            Slice<Directionality> types, 
            Slice<PairedBracketType> pairTypes, 
            Slice<int> pairValues, 
            byte paragraphEmbeddingLevel
            )
        {
            Bidi bidi = new Bidi(types, pairTypes, pairValues, paragraphEmbeddingLevel);
            return bidi;
        }

    }
}
