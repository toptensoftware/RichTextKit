using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Represents a unicode string and all associated attributes
    /// for each character required for the Bidi algorithm
    /// </summary>
    class BidiData
    {
        /// <summary>
        /// Construct a new empty BidiData
        /// </summary>
        public BidiData()
        {
            _directionality = new Buffer<Directionality>();
            _pairedBracketTypes= new Buffer<PairedBracketType>();
            _pairedBracketValues = new Buffer<int>();
        }

        List<int> _paragraphPositions = new List<int>();

        byte _paragraphEmbeddingLevel;

        public byte ParagraphEmbeddingLevel => _paragraphEmbeddingLevel;

        /// <summary>
        /// Initialize with an array of Unicode code points
        /// </summary>
        /// <param name="codePoints">The unicode code points to be processed</param>
        /// <param name="paragraphEmbeddingLevel">The paragraph embedding level</param>
        public void Init(Slice<int> codePoints, byte paragraphEmbeddingLevel)
        {
            // Set working buffer sizes
            _directionality.Length = codePoints.Length;
            _pairedBracketTypes.Length = codePoints.Length;
            _pairedBracketValues.Length = codePoints.Length;

            _paragraphPositions.Clear();
            _paragraphEmbeddingLevel = paragraphEmbeddingLevel;

            // Resolve the directionality, paired bracket type and paired bracket values for
            // all code points
            for (int i = 0; i < codePoints.Length; i++)
            {
                var bidiData = UnicodeClasses.BidiData(codePoints[i]);
                _directionality[i] = (Directionality)(bidiData >> 24);
                _pairedBracketTypes[i] = (PairedBracketType)((bidiData >> 16) & 0xFF);
                if (_pairedBracketTypes[i] == PairedBracketType.o)
                {
                    _pairedBracketValues[i] = MapCanon((int)(bidiData & 0xFFFF));
                }
                else
                {
                    _pairedBracketValues[i] = MapCanon(codePoints[i]);
                }

                if (_directionality[i] == RichTextKit.Directionality.B)
                {
                    _directionality[i] = (Directionality)_paragraphEmbeddingLevel;
                    _paragraphPositions.Add(i);
                }
            }

            // Create slices on work buffers
            Directionality = _directionality.AsSlice();
            PairedBracketTypes = _pairedBracketTypes.AsSlice();
            PairedBracketValues = _pairedBracketValues.AsSlice();
        }

        /// <summary>
        /// Map bracket types 0x3008 and 0x3009 to their canonical equivalents
        /// </summary>
        /// <param name="codePoint">The code point to be mapped</param>
        /// <returns>The mapped canonical code point, or the passed code point</returns>
        static int MapCanon(int codePoint)
        {
            if (codePoint == 0x3008)
                return 0x2379;
            if (codePoint == 0x3009)
                return 0x237A;
            else
                return codePoint;
        }

        /// <summary>
        /// Get the length of the data held by the BidiData
        /// </summary>
        public int Length => _directionality.Length;

        Buffer<Directionality> _directionality;
        Buffer<PairedBracketType> _pairedBracketTypes;
        Buffer<int> _pairedBracketValues;

        /// <summary>
        /// The directionality of each code point
        /// </summary>
        public Slice<Directionality> Directionality;

        /// <summary>
        /// The paired bracket type for each code point
        /// </summary>
        public Slice<PairedBracketType> PairedBracketTypes;

        /// <summary>
        /// The paired bracket value for code code point
        /// </summary>
        /// <remarks>
        /// The paired bracket values are the code points
        /// of each character where the opening code point
        /// is replaced with the closing code point for easier
        /// matching.  Also, bracket code points are mapped
        /// to their canonical equivalents
        /// </remarks>
        public Slice<int> PairedBracketValues;
    }
}
