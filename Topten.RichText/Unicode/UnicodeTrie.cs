using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

// Ported from: https://github.com/foliojs/unicode-trie

namespace Topten.RichText
{
    public class UnicodeTrie
    {
        public UnicodeTrie(Stream stream)
        {
            int dataLength;
            using (var bw = new BinaryReader(stream, Encoding.UTF8, true))
            {
                _highStart = bw.ReadInt32BE();
                _errorValue = bw.ReadUInt32BE();
                dataLength = bw.ReadInt32BE() / 4;
            }

            using (var infl1 = new DeflateStream(stream, CompressionMode.Decompress, true))
            using (var infl2 = new DeflateStream(infl1, CompressionMode.Decompress, true))
            using (var bw = new BinaryReader(infl2, Encoding.UTF8, true))
            {
                _data = new int[dataLength];
                for (int i = 0; i < _data.Length; i++)
                {
                    _data[i] = bw.ReadInt32();
                }
            }
        }

        public UnicodeTrie(byte[] buf) : this(new MemoryStream(buf))
        {

        }

        internal UnicodeTrie(int[] data, int highStart, uint errorValue)
        {
            _data = data;
            _highStart = highStart;
            _errorValue = errorValue;
        }

        internal void Save(Stream stream)
        {
            // Write the header info
            using (var bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                bw.WriteBE(_highStart);
                bw.WriteBE(_errorValue);
                bw.WriteBE(_data.Length * 4);
            }

            // Double compress the data
            using (var def1 = new DeflateStream(stream, CompressionLevel.Optimal, true))
            using (var def2 = new DeflateStream(def1, CompressionLevel.Optimal, true))
            using (var bw = new BinaryWriter(def2, Encoding.UTF8, true))
            {
                foreach (var v in _data)
                {
                    bw.Write(v);
                }
                bw.Flush();
                def2.Flush();
                def1.Flush();
            }
        }


        int[] _data;
        int _highStart;
        uint _errorValue;

        public uint Get(int codePoint)
        {
            int index;
            if ((codePoint < 0) || (codePoint > 0x10ffff))
            {
                return _errorValue;
            }

            if ((codePoint < 0xd800) || ((codePoint > 0xdbff) && (codePoint <= 0xffff)))
            {
                // Ordinary BMP code point, excluding leading surrogates.
                // BMP uses a single level lookup.  BMP index starts at offset 0 in the index.
                // data is stored in the index array itself.
                index = (_data[codePoint >> UnicodeTrieBuilder.SHIFT_2] << UnicodeTrieBuilder.INDEX_SHIFT) + (codePoint & UnicodeTrieBuilder.DATA_MASK);
                return (uint)_data[index];
            }

            if (codePoint <= 0xffff)
            {
                // Lead Surrogate Code Point.  A Separate index section is stored for
                // lead surrogate code units and code points.
                //   The main index has the code unit data.
                //   For this function, we need the code point data.
                index = (_data[UnicodeTrieBuilder.LSCP_INDEX_2_OFFSET + ((codePoint - 0xd800) >> UnicodeTrieBuilder.SHIFT_2)] << UnicodeTrieBuilder.INDEX_SHIFT) + (codePoint & UnicodeTrieBuilder.DATA_MASK);
                return (uint)_data[index];
            }

            if (codePoint < _highStart)
            {
                // Supplemental code point, use two-level lookup.
                index = _data[(UnicodeTrieBuilder.INDEX_1_OFFSET - UnicodeTrieBuilder.OMITTED_BMP_INDEX_1_LENGTH) + (codePoint >> UnicodeTrieBuilder.SHIFT_1)];
                index = _data[index + ((codePoint >> UnicodeTrieBuilder.SHIFT_2) & UnicodeTrieBuilder.INDEX_2_MASK)];
                index = (index << UnicodeTrieBuilder.INDEX_SHIFT) + (codePoint & UnicodeTrieBuilder.DATA_MASK);
                return (uint)_data[index];
            }

            return (uint)_data[_data.Length - UnicodeTrieBuilder.DATA_GRANULARITY];
        }
    }
}
