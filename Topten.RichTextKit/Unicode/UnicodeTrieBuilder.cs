// RichTextKit
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

// Ported from: https://github.com/foliojs/unicode-trie

namespace Topten.RichTextKit
{
    internal class UnicodeTrieBuilder
    {
        // Shift size for getting the index-1 table offset.
        internal const int SHIFT_1 = 6 + 5;

        // Shift size for getting the index-2 table offset.
        internal const int SHIFT_2 = 5;

        // Difference between the two shift sizes,
        // for getting an index-1 offset from an index-2 offset. 6=11-5
        const int SHIFT_1_2 = SHIFT_1 - SHIFT_2;

        // Number of index-1 entries for the BMP. 32=0x20
        // This part of the index-1 table is omitted from the serialized form.
        internal const int OMITTED_BMP_INDEX_1_LENGTH = 0x10000 >> SHIFT_1;

        // Number of code points per index-1 table entry. 2048=0x800
        const int CP_PER_INDEX_1_ENTRY = 1 << SHIFT_1;

        // Number of entries in an index-2 block. 64=0x40
        const int INDEX_2_BLOCK_LENGTH = 1 << SHIFT_1_2;

        // Mask for getting the lower bits for the in-index-2-block offset. */
        internal const int INDEX_2_MASK = INDEX_2_BLOCK_LENGTH - 1;

        // Number of entries in a data block. 32=0x20
        const int DATA_BLOCK_LENGTH = 1 << SHIFT_2;

        // Mask for getting the lower bits for the in-data-block offset.
        internal const int DATA_MASK = DATA_BLOCK_LENGTH - 1;

        // Shift size for shifting left the index array values.
        // Increases possible data size with 16-bit index values at the cost
        // of compactability.
        // This requires data blocks to be aligned by DATA_GRANULARITY.
        internal const int INDEX_SHIFT = 2;

        // The alignment size of a data block. Also the granularity for compaction.
        internal const int DATA_GRANULARITY = 1 << INDEX_SHIFT;

        // The BMP part of the index-2 table is fixed and linear and starts at offset 0.
        // Length=2048=0x800=0x10000>>SHIFT_2.
        const int INDEX_2_OFFSET = 0;

        // The part of the index-2 table for U+D800..U+DBFF stores values for
        // lead surrogate code _units_ not code _points_.
        // Values for lead surrogate code _points_ are indexed with this portion of the table.
        // Length=32=0x20=0x400>>SHIFT_2. (There are 1024=0x400 lead surrogates.)
        internal const int LSCP_INDEX_2_OFFSET = 0x10000 >> SHIFT_2;
        const int LSCP_INDEX_2_LENGTH = 0x400 >> SHIFT_2;

        // Count the lengths of both BMP pieces. 2080=0x820
        const int INDEX_2_BMP_LENGTH = LSCP_INDEX_2_OFFSET + LSCP_INDEX_2_LENGTH;

        // The 2-byte UTF-8 version of the index-2 table follows at offset 2080=0x820.
        // Length 32=0x20 for lead bytes C0..DF, regardless of SHIFT_2.
        const int UTF8_2B_INDEX_2_OFFSET = INDEX_2_BMP_LENGTH;
        const int UTF8_2B_INDEX_2_LENGTH = 0x800 >> 6;  // U+0800 is the first code point after 2-byte UTF-8

        // The index-1 table, only used for supplementary code points, at offset 2112=0x840.
        // Variable length, for code points up to highStart, where the last single-value range starts.
        // Maximum length 512=0x200=0x100000>>SHIFT_1.
        // (For 0x100000 supplementary code points U+10000..U+10ffff.)
        //
        // The part of the index-2 table for supplementary code points starts
        // after this index-1 table.
        //
        // Both the index-1 table and the following part of the index-2 table
        // are omitted completely if there is only BMP data.
        internal const int INDEX_1_OFFSET = UTF8_2B_INDEX_2_OFFSET + UTF8_2B_INDEX_2_LENGTH;
        const int MAX_INDEX_1_LENGTH = 0x100000 >> SHIFT_1;

        // The illegal-UTF-8 data block follows the ASCII block, at offset 128=0x80.
        // Used with linear access for single bytes 0..0xbf for simple error handling.
        // Length 64=0x40, not DATA_BLOCK_LENGTH.
        const int BAD_UTF8_DATA_OFFSET = 0x80;

        // The start of non-linear-ASCII data blocks, at offset 192=0xc0.
        // !!!!
        const int DATA_START_OFFSET = 0xc0;

        // The null data block.
        // Length 64=0x40 even if DATA_BLOCK_LENGTH is smaller,
        // to work with 6-bit trail bytes from 2-byte UTF-8.
        const int DATA_NULL_OFFSET = DATA_START_OFFSET;

        // The start of allocated data blocks.
        const int NEW_DATA_START_OFFSET = DATA_NULL_OFFSET + 0x40;

        // The start of data blocks for U+0800 and above.
        // Below, compaction uses a block length of 64 for 2-byte UTF-8.
        // From here on, compaction uses DATA_BLOCK_LENGTH.
        // Data values for 0x780 code points beyond ASCII.
        const int DATA_0800_OFFSET = NEW_DATA_START_OFFSET + 0x780;

        // Start with allocation of 16k data entries. */
        const int INITIAL_DATA_LENGTH = 1 << 14;

        // Grow about 8x each time.
        const int MEDIUM_DATA_LENGTH = 1 << 17;

        // Maximum length of the runtime data array.
        // Limited by 16-bit index values that are left-shifted by INDEX_SHIFT,
        // and by uint16_t UTrie2Header.shiftedDataLength.
        const int MAX_DATA_LENGTH_RUNTIME = 0xffff << INDEX_SHIFT;

        const int INDEX_1_LENGTH = 0x110000 >> SHIFT_1;

        // Maximum length of the build-time data array.
        // One entry per 0x110000 code points, plus the illegal-UTF-8 block and the null block,
        // plus values for the 0x400 surrogate code units.
        const int MAX_DATA_LENGTH_BUILDTIME = 0x110000 + 0x40 + 0x40 + 0x400;

        // At build time, leave a gap in the index-2 table,
        // at least as long as the maximum lengths of the 2-byte UTF-8 index-2 table
        // and the supplementary index-1 table.
        // Round up to INDEX_2_BLOCK_LENGTH for proper compacting.
        const int INDEX_GAP_OFFSET = INDEX_2_BMP_LENGTH;
        const int INDEX_GAP_LENGTH = ((UTF8_2B_INDEX_2_LENGTH + MAX_INDEX_1_LENGTH) + INDEX_2_MASK) & ~INDEX_2_MASK;

        // Maximum length of the build-time index-2 array.
        // Maximum number of Unicode code points (0x110000) shifted right by SHIFT_2,
        // plus the part of the index-2 table for lead surrogate code points,
        // plus the build-time index gap,
        // plus the null index-2 block.)
        const int MAX_INDEX_2_LENGTH = (0x110000 >> SHIFT_2) + LSCP_INDEX_2_LENGTH + INDEX_GAP_LENGTH + INDEX_2_BLOCK_LENGTH;

        // The null index-2 block, following the gap in the index-2 table.
        const int INDEX_2_NULL_OFFSET = INDEX_GAP_OFFSET + INDEX_GAP_LENGTH;

        // The start of allocated index-2 blocks.
        const int INDEX_2_START_OFFSET = INDEX_2_NULL_OFFSET + INDEX_2_BLOCK_LENGTH;

        // Maximum length of the runtime index array.
        // Limited by its own 16-bit index values, and by uint16_t UTrie2Header.indexLength.
        // (The actual maximum length is lower,
        // (0x110000>>SHIFT_2)+UTF8_2B_INDEX_2_LENGTH+MAX_INDEX_1_LENGTH.)
        const int MAX_INDEX_LENGTH = 0xffff;

        static bool equal(uint[] a, int s, int t, int length)
        {
            for (var i = 0; i < length; i++)
            {
                if (a[s + i] != a[t + i])
                {
                    return false;
                }
            }

            return true;
        }

        static bool equal(int[] a, int s, int t, int length)
        {
            for (var i = 0; i < length; i++)
            {
                if (a[s + i] != a[t + i])
                {
                    return false;
                }
            }

            return true;
        }

        uint _initialValue;
        uint _errorValue;
        int[] _index1;
        int[] _index2;
        int _highStart;
        UInt32[] _data;
        int _dataCapacity;
        int _firstFreeBlock;
        bool _isCompacted;
        int[] _map;
        int _dataNullOffset;
        int _dataLength;
        int _index2NullOffset;
        int _index2Length;

        public UnicodeTrieBuilder(uint initialValue = 0, uint errorValue = 0)
        {
            _initialValue = initialValue;
            _errorValue = errorValue;
            _index1 = new int[INDEX_1_LENGTH];
            _index2 = new int[MAX_INDEX_2_LENGTH];
            _highStart = 0x110000;

            _data = new uint[INITIAL_DATA_LENGTH];
            _dataCapacity = INITIAL_DATA_LENGTH;

            _firstFreeBlock = 0;
            _isCompacted = false;

            // Multi-purpose per-data-block table.
            //
            // Before compacting:
            //
            // Per-data-block reference counters/free-block list.
            //  0: unused
            // >0: reference counter (number of index-2 entries pointing here)
            // <0: next free data block in free-block list
            //
            // While compacting:
            //
            // Map of adjusted indexes, used in compactData() and compactIndex2().
            // Maps from original indexes to new ones.
            _map = new int[MAX_DATA_LENGTH_BUILDTIME >> SHIFT_2];

            int i;
            for (i = 0; i < 0x80; i++)
            {
                _data[i] = _initialValue;
            }

            for (; i < 0xc0; i++)
            {
                _data[i] = _errorValue;
            }

            for (i = DATA_NULL_OFFSET; i < NEW_DATA_START_OFFSET; i++)
            {
                _data[i] = _initialValue;
            }

            _dataNullOffset = DATA_NULL_OFFSET;
            _dataLength = NEW_DATA_START_OFFSET;

            // set the index-2 indexes for the 2=0x80>>SHIFT_2 ASCII data blocks
            int j;
            i = 0;
            for (j = 0; j < 0x80; j += DATA_BLOCK_LENGTH) {
                _index2[i] = j;
                _map[i++] = 1;
            }

            // reference counts for the bad-UTF-8-data block
            for (; j < 0xc0; j += DATA_BLOCK_LENGTH) {
                _map[i++] = 0;
            }

            // Reference counts for the null data block: all blocks except for the ASCII blocks.
            // Plus 1 so that we don't drop this block during compaction.
            // Plus as many as needed for lead surrogate code points.
            // i==newTrie->dataNullOffset
            _map[i++] = ((0x110000 >> SHIFT_2) - (0x80 >> SHIFT_2)) + 1 + LSCP_INDEX_2_LENGTH;
            j += DATA_BLOCK_LENGTH;
            for (; j < NEW_DATA_START_OFFSET; j += DATA_BLOCK_LENGTH) {
                _map[i++] = 0;
            }

            // set the remaining indexes in the BMP index-2 block
            // to the null data block
            for (i = 0x80 >> SHIFT_2; i < INDEX_2_BMP_LENGTH; i++) {
                _index2[i] = DATA_NULL_OFFSET;
            }

            // Fill the index gap with impossible values so that compaction
            // does not overlap other index-2 blocks with the gap.
            for (i = 0; i < INDEX_GAP_LENGTH; i++) {
                _index2[INDEX_GAP_OFFSET + i] = -1;
            }

            // set the indexes in the null index-2 block
            for (i = 0; i < INDEX_2_BLOCK_LENGTH; i++) {
                _index2[INDEX_2_NULL_OFFSET + i] = DATA_NULL_OFFSET;
            }

            _index2NullOffset = INDEX_2_NULL_OFFSET;
            _index2Length = INDEX_2_START_OFFSET;

            // set the index-1 indexes for the linear index-2 block
            j = 0;
            for (i = 0; i < OMITTED_BMP_INDEX_1_LENGTH; i++) {
                _index1[i] = j;
                j += INDEX_2_BLOCK_LENGTH;
            }

            // set the remaining index-1 indexes to the null index-2 block
            for (; i < INDEX_1_LENGTH; i++) {
                _index1[i] = INDEX_2_NULL_OFFSET;
            }

            // Preallocate and reset data for U+0080..U+07ff,
            // for 2-byte UTF-8 which will be compacted in 64-blocks
            // even if DATA_BLOCK_LENGTH is smaller.
            for (i = 0x80; i < 0x800; i += DATA_BLOCK_LENGTH) {
                Set(i, _initialValue);
            }

        }

        public UnicodeTrieBuilder Set(int codePoint, uint value)
        {
            if ((codePoint < 0) || (codePoint > 0x10ffff))
            {
                throw new InvalidOperationException("Invalid code point");
            }

            if (_isCompacted)
            {
                throw new InvalidOperationException("Already compacted");
            }

            var block = getDataBlock(codePoint, true);
            _data[block + (codePoint & DATA_MASK)] = value;
            return this;
        }

        public UnicodeTrieBuilder SetRange(int start, int end, uint value, bool overwrite = true)
        {

            if ((start > 0x10ffff) || (end > 0x10ffff) || (start > end))
            {
                throw new InvalidOperationException("Invalid code point");
            }

            if (_isCompacted)
            {
                throw new InvalidOperationException("Already compacted");
            }

            if (!overwrite && (value == _initialValue))
            {
                return this; // nothing to do
            }

            var limit = end + 1;
            if ((start & DATA_MASK) != 0)
            {
                // set partial block at [start..following block boundary
                var block = getDataBlock(start, true);

                var nextStart = (start + DATA_BLOCK_LENGTH) & ~DATA_MASK;
                if (nextStart <= limit)
                {
                    fillBlock(block, start & DATA_MASK, DATA_BLOCK_LENGTH, value, _initialValue, overwrite);
                    start = nextStart;
                }
                else
                {
                    fillBlock(block, start & DATA_MASK, limit & DATA_MASK, value, _initialValue, overwrite);
                    return this;
                }
            }

            // number of positions in the last, partial block
            var rest = limit & DATA_MASK;

            // round down limit to a block boundary
            limit &= ~DATA_MASK;

            // iterate over all-value blocks
            int repeatBlock;
            if (value == _initialValue)
            {
                repeatBlock = _dataNullOffset;
            }
            else
            {
                repeatBlock = -1;
            }

            while (start < limit)
            {
                var setRepeatBlock = false;

                if ((value == _initialValue) && isInNullBlock(start, true))
                {
                    start += DATA_BLOCK_LENGTH; // nothing to do
                    continue;
                }

                // get index value
                var i2 = getIndex2Block(start, true);
                i2 += (start >> SHIFT_2) & INDEX_2_MASK;

                var block = _index2[i2];
                if (isWritableBlock(block))
                {
                    // already allocated
                    if (overwrite && (block >= DATA_0800_OFFSET))
                    {
                        // We overwrite all values, and it's not a
                        // protected (ASCII-linear or 2-byte UTF-8) block:
                        // replace with the repeatBlock.
                        setRepeatBlock = true;
                    }
                    else
                    {
                        // protected block: just write the values into this block
                        fillBlock(block, 0, DATA_BLOCK_LENGTH, value, _initialValue, overwrite);
                    }

                }
                else if ((_data[block] != value) && (overwrite || (block == _dataNullOffset)))
                {
                    // Set the repeatBlock instead of the null block or previous repeat block:
                    //
                    // If !isWritableBlock() then all entries in the block have the same value
                    // because it's the null block or a range block (the repeatBlock from a previous
                    // call to utrie2_setRange32()).
                    // No other blocks are used multiple times before compacting.
                    //
                    // The null block is the only non-writable block with the initialValue because
                    // of the repeatBlock initialization above. (If value==initialValue, then
                    // the repeatBlock will be the null data block.)
                    //
                    // We set our repeatBlock if the desired value differs from the block's value,
                    // and if we overwrite any data or if the data is all initial values
                    // (which is the same as the block being the null block, see above).
                    setRepeatBlock = true;
                }

                if (setRepeatBlock)
                {
                    if (repeatBlock >= 0)
                    {
                        setIndex2Entry(i2, repeatBlock);
                    }
                    else
                    {
                        // create and set and fill the repeatBlock
                        repeatBlock = getDataBlock(start, true);
                        writeBlock(repeatBlock, value);
                    }
                }

                start += DATA_BLOCK_LENGTH;
            }

            if (rest > 0)
            {
                // set partial block at [last block boundary..limit
                var block = getDataBlock(start, true);
                fillBlock(block, 0, rest, value, _initialValue, overwrite);
            }

            return this;
        }

        public uint Get(int c, bool fromLSCP = true)
        {
            if ((c < 0) || (c > 0x10ffff))
            {
                return _errorValue;
            }

            if ((c >= _highStart) && (!((c >= 0xd800) && (c < 0xdc00)) || fromLSCP))
            {
                return _data[_dataLength - DATA_GRANULARITY];
            }

            int i2;
            if (((c >= 0xd800) && (c < 0xdc00)) && fromLSCP)
            {
                i2 = (LSCP_INDEX_2_OFFSET - (0xd800 >> SHIFT_2)) + (c >> SHIFT_2);
            }
            else
            {
                i2 = _index1[c >> SHIFT_1] + ((c >> SHIFT_2) & INDEX_2_MASK);
            }

            var block = _index2[i2];
            return _data[block + (c & DATA_MASK)];
        }

        public byte[] ToBuffer()
        {
            var mem = new MemoryStream();
            Save(mem);
            return mem.GetBuffer();
        }

        public void Save(Stream stream)
        {
            var trie = this.Freeze();
            trie.Save(stream);
        }

        bool isInNullBlock(int c, bool forLSCP)
        {
            int i2;
            if (((c & 0xfffffc00) == 0xd800) && forLSCP)
            {
                i2 = (LSCP_INDEX_2_OFFSET - (0xd800 >> SHIFT_2)) + (c >> SHIFT_2);
            }
            else
            {
                i2 = _index1[c >> SHIFT_1] + ((c >> SHIFT_2) & INDEX_2_MASK);
            }

            var block = _index2[i2];
            return block == _dataNullOffset;
        }

        int allocIndex2Block()
        {
            var newBlock = _index2Length;
            var newTop = newBlock + INDEX_2_BLOCK_LENGTH;
            if (newTop > _index2.Length)
            {
                // Should never occur.
                // Either MAX_BUILD_TIME_INDEX_LENGTH is incorrect,
                // or the code writes more values than should be possible.
                throw new InvalidOperationException("Internal error in Trie2 creation.");
            }

            _index2Length = newTop;
            Array.Copy(_index2, _index2NullOffset, _index2, newBlock, INDEX_2_BLOCK_LENGTH);
            //    _index2.set(_index2.subarray(_index2NullOffset, _index2NullOffset + INDEX_2_BLOCK_LENGTH), newBlock);

            return newBlock;
        }

        int getIndex2Block(int c, bool forLSCP)
        {
            if ((c >= 0xd800) && (c < 0xdc00) && forLSCP)
            {
                return LSCP_INDEX_2_OFFSET;
            }

            var i1 = c >> SHIFT_1;
            var i2 = _index1[i1];
            if (i2 == _index2NullOffset)
            {
                i2 = allocIndex2Block();
                _index1[i1] = i2;
            }

            return i2;
        }

        bool isWritableBlock(int block)
        {
            return (block != _dataNullOffset) && (_map[block >> SHIFT_2] == 1);
        }

        int allocDataBlock(int copyBlock)
        {
            int newBlock;
            if (_firstFreeBlock != 0)
            {
                // get the first free block
                newBlock = _firstFreeBlock;
                _firstFreeBlock = -_map[newBlock >> SHIFT_2];
            }
            else
            {
                // get a new block from the high end
                newBlock = _dataLength;
                var newTop = newBlock + DATA_BLOCK_LENGTH;
                if (newTop > _dataCapacity)
                {
                    // out of memory in the data array
                    int capacity;
                    if (_dataCapacity < MEDIUM_DATA_LENGTH)
                    {
                        capacity = MEDIUM_DATA_LENGTH;
                    }
                    else if (_dataCapacity < MAX_DATA_LENGTH_BUILDTIME)
                    {
                        capacity = MAX_DATA_LENGTH_BUILDTIME;
                    }
                    else
                    {
                        // Should never occur.
                        // Either MAX_DATA_LENGTH_BUILDTIME is incorrect,
                        // or the code writes more values than should be possible.
                        throw new InvalidOperationException("Internal error in Trie2 creation.");
                    }

                    var newData = new UInt32[capacity];
                    Array.Copy(_data, newData, _dataLength);
                    _data = newData;
                    _dataCapacity = capacity;
                }

                _dataLength = newTop;
            }

            Array.Copy(_data, copyBlock, _data, newBlock, DATA_BLOCK_LENGTH);
            //_data.set(_data.subarray(copyBlock, copyBlock + DATA_BLOCK_LENGTH), newBlock);
            _map[newBlock >> SHIFT_2] = 0;
            return newBlock;
        }

        void releaseDataBlock(int block)
        {
            // put this block at the front of the free-block chain
            _map[block >> SHIFT_2] = -_firstFreeBlock;
            _firstFreeBlock = block;
        }

        void setIndex2Entry(int i2, int block)
        {
            ++_map[block >> SHIFT_2];  // increment first, in case block == oldBlock!
            var oldBlock = _index2[i2];
            if (--_map[oldBlock >> SHIFT_2] == 0)
            {
                releaseDataBlock(oldBlock);
            }

            _index2[i2] = block;
        }

        int getDataBlock(int c, bool forLSCP)
        {
            var i2 = getIndex2Block(c, forLSCP);
            i2 += (c >> SHIFT_2) & INDEX_2_MASK;

            var oldBlock = _index2[i2];
            if (isWritableBlock(oldBlock))
            {
                return oldBlock;
            }

            // allocate a new data block
            var newBlock = allocDataBlock(oldBlock);
            setIndex2Entry(i2, newBlock);
            return newBlock;
        }

        void fillBlock(int block, int start, int limit, uint value, uint initialValue, bool overwrite)
        {
            int i;
            if (overwrite)
            {
                for (i = block + start; i < block + limit; i++)
                {
                    _data[i] = value;
                }
            }
            else
            {
                for (i = block + start; i < block + limit; i++)
                {
                    if (_data[i] == initialValue)
                    {
                        _data[i] = value;
                    }
                }
            }
        }

        void writeBlock(int block, uint value)
        {
            var limit = block + DATA_BLOCK_LENGTH;
            while (block < limit)
            {
                _data[block++] = value;
            }
        }

        int findHighStart(uint highValue)
        {
            int prevBlock, prevI2Block;
            
            // set variables for previous range
            if (highValue == _initialValue)
            {
                prevI2Block = _index2NullOffset;
                prevBlock = _dataNullOffset;
            }
            else
            {
                prevI2Block = -1;
                prevBlock = -1;
            }

            int prev = 0x110000;

            // enumerate index-2 blocks
            var i1 = INDEX_1_LENGTH;
            var c = prev;
            while (c > 0)
            {
                var i2Block = _index1[--i1];
                if (i2Block == prevI2Block)
                {
                    // the index-2 block is the same as the previous one, and filled with highValue
                    c -= CP_PER_INDEX_1_ENTRY;
                    continue;
                }

                prevI2Block = i2Block;
                if (i2Block == _index2NullOffset)
                {
                    // this is the null index-2 block
                    if (highValue != _initialValue)
                    {
                        return c;
                    }
                    c -= CP_PER_INDEX_1_ENTRY;
                }
                else
                {
                    // enumerate data blocks for one index-2 block
                    var i2 = INDEX_2_BLOCK_LENGTH;
                    while (i2 > 0)
                    {
                        var block = _index2[i2Block + --i2];
                        if (block == prevBlock)
                        {
                            // the block is the same as the previous one, and filled with highValue
                            c -= DATA_BLOCK_LENGTH;
                            continue;
                        }

                        prevBlock = block;
                        if (block == _dataNullOffset)
                        {
                            // this is the null data block
                            if (highValue != _initialValue)
                            {
                                return c;
                            }
                            c -= DATA_BLOCK_LENGTH;
                        }
                        else
                        {
                            var j = DATA_BLOCK_LENGTH;
                            while (j > 0)
                            {
                                var value = _data[block + --j];
                                if (value != highValue)
                                {
                                    return c;
                                }
                                --c;
                            }
                        }
                    }
                }
            }

            // deliver last range
            return 0;
        }

        int findSameDataBlock(int dataLength, int otherBlock, int blockLength)
        {
            // ensure that we do not even partially get past dataLength
            dataLength -= blockLength;
            var block = 0;
            while (block <= dataLength)
            {
                if (equal(_data, block, otherBlock, blockLength))
                {
                    return block;
                }
                block += DATA_GRANULARITY;
            }

            return -1;
        }

        int findSameIndex2Block(int index2Length, int otherBlock) {
            // ensure that we do not even partially get past index2Length
            index2Length -= INDEX_2_BLOCK_LENGTH;
            for (var block = 0; block <= index2Length; block++)
            {
                if (equal(_index2, block, otherBlock, INDEX_2_BLOCK_LENGTH))
                {
                    return block;
                }
            }

            return -1;
        }

        void compactData()
        {
            // do not compact linear-ASCII data
            var newStart = DATA_START_OFFSET;
            var start = 0;
            var i = 0;

            while (start < newStart)
            {
                _map[i++] = start;
                start += DATA_BLOCK_LENGTH;
            }

            // Start with a block length of 64 for 2-byte UTF-8,
            // then switch to DATA_BLOCK_LENGTH.
            var blockLength = 64;
            var blockCount = blockLength >> SHIFT_2;
            start = newStart;
            while (start < _dataLength)
            {
                // start: index of first entry of current block
                // newStart: index where the current block is to be moved
                //           (right after current end of already-compacted data)
                int mapIndex, movedStart;
                if (start == DATA_0800_OFFSET)
                {
                    blockLength = DATA_BLOCK_LENGTH;
                    blockCount = 1;
                }

                // skip blocks that are not used
                if (_map[start >> SHIFT_2] <= 0)
                {
                    // advance start to the next block
                    start += blockLength;

                    // leave newStart with the previous block!
                    continue;
                }

                // search for an identical block
                if ((movedStart = findSameDataBlock(newStart, start, blockLength)) >= 0)
                {
                    // found an identical block, set the other block's index value for the current block
                    mapIndex = start >> SHIFT_2;
                    for (i = blockCount; i > 0; i--)
                    {
                        _map[mapIndex++] = movedStart;
                        movedStart += DATA_BLOCK_LENGTH;
                    }

                    // advance start to the next block
                    start += blockLength;

                    // leave newStart with the previous block!
                    continue;
                }

                // see if the beginning of this block can be overlapped with the end of the previous block
                // look for maximum overlap (modulo granularity) with the previous, adjacent block
                var overlap = blockLength - DATA_GRANULARITY;
                while ((overlap > 0) && !equal(_data, (newStart - overlap), start, overlap))
                {
                    overlap -= DATA_GRANULARITY;
                }

                if ((overlap > 0) || (newStart < start))
                {
                    // some overlap, or just move the whole block
                    movedStart = newStart - overlap;
                    mapIndex = start >> SHIFT_2;

                    for (i = blockCount; i > 0; i--)
                    {
                        _map[mapIndex++] = movedStart;
                        movedStart += DATA_BLOCK_LENGTH;
                    }

                    // move the non-overlapping indexes to their new positions
                    start += overlap;
                    for (i = blockLength - overlap; i > 0; i--)
                    {
                        _data[newStart++] = _data[start++];
                    }

                }
                else
                { // no overlap && newStart==start
                    mapIndex = start >> SHIFT_2;
                    for (i = blockCount; i > 0; i--)
                    {
                        _map[mapIndex++] = start;
                        start += DATA_BLOCK_LENGTH;
                    }

                    newStart = start;
                }
            }

            // now adjust the index-2 table
            i = 0;
            while (i < _index2Length)
            {
                // Gap indexes are invalid (-1). Skip over the gap.
                if (i == INDEX_GAP_OFFSET)
                {
                    i += INDEX_GAP_LENGTH;
                }
                _index2[i] = _map[_index2[i] >> SHIFT_2];
                ++i;
            }

            _dataNullOffset = _map[_dataNullOffset >> SHIFT_2];

            // ensure dataLength alignment
            while ((newStart & (DATA_GRANULARITY - 1)) != 0)
            {
                _data[newStart++] = _initialValue;
            }
            _dataLength = newStart;
        }

        void compactIndex2()
        {
            // do not compact linear-BMP index-2 blocks
            var newStart = INDEX_2_BMP_LENGTH;
            var start = 0;
            var i = 0;

            while (start < newStart)
            {
                _map[i++] = start;
                start += INDEX_2_BLOCK_LENGTH;
            }

            // Reduce the index table gap to what will be needed at runtime.
            newStart += UTF8_2B_INDEX_2_LENGTH + ((_highStart - 0x10000) >> SHIFT_1);
            start = INDEX_2_NULL_OFFSET;
            while (start < _index2Length)
            {
                // start: index of first entry of current block
                // newStart: index where the current block is to be moved
                //           (right after current end of already-compacted data)

                // search for an identical block
                int movedStart;
                if ((movedStart = findSameIndex2Block(newStart, start)) >= 0)
                {
                    // found an identical block, set the other block's index value for the current block
                    _map[start >> SHIFT_1_2] = movedStart;

                    // advance start to the next block
                    start += INDEX_2_BLOCK_LENGTH;

                    // leave newStart with the previous block!
                    continue;
                }

                // see if the beginning of this block can be overlapped with the end of the previous block
                // look for maximum overlap with the previous, adjacent block
                var overlap = INDEX_2_BLOCK_LENGTH - 1;
                while ((overlap > 0) && !equal(_index2, (newStart - overlap), start, overlap))
                {
                    --overlap;
                }

                if ((overlap > 0) || (newStart < start))
                {
                    // some overlap, or just move the whole block
                    _map[start >> SHIFT_1_2] = newStart - overlap;

                    // move the non-overlapping indexes to their new positions
                    start += overlap;
                    for (i = INDEX_2_BLOCK_LENGTH - overlap; i > 0; i--)
                    {
                        _index2[newStart++] = _index2[start++];
                    }

                }
                else
                { // no overlap && newStart==start
                    _map[start >> SHIFT_1_2] = start;
                    start += INDEX_2_BLOCK_LENGTH;
                    newStart = start;
                }
            }

            // now adjust the index-1 table
            for (i = 0; i < INDEX_1_LENGTH; i++)
            {
                _index1[i] = _map[_index1[i] >> SHIFT_1_2];
            }

            _index2NullOffset = _map[_index2NullOffset >> SHIFT_1_2];

            // Ensure data table alignment:
            // Needs to be granularity-aligned for 16-bit trie
            // (so that dataMove will be down-shiftable),
            // and 2-aligned for uint32_t data.

            // Arbitrary value: 0x3fffc not possible for real data.
            while ((newStart & ((DATA_GRANULARITY - 1) | 1)) != 0)
            {
                _index2[newStart++] = 0x0000ffff << INDEX_SHIFT;
            }

            _index2Length = newStart;
        }

        void compact()
        {
            // find highStart and round it up
            var highValue = Get(0x10ffff);
            var highStart = findHighStart(highValue);
            highStart = (highStart + (CP_PER_INDEX_1_ENTRY - 1)) & ~(CP_PER_INDEX_1_ENTRY - 1);
            if (highStart == 0x110000)
            {
                highValue = _errorValue;
            }

            // Set trie->highStart only after utrie2_get32(trie, highStart).
            // Otherwise utrie2_get32(trie, highStart) would try to read the highValue.
            _highStart = highStart;
            if (_highStart < 0x110000)
            {
                // Blank out [highStart..10ffff] to release associated data blocks.
                var suppHighStart = _highStart <= 0x10000 ? 0x10000 : _highStart;
                SetRange(suppHighStart, 0x10ffff, _initialValue, true);
            }

            compactData();

            if (_highStart > 0x10000)
            {
                compactIndex2();
            }

            // Store the highValue in the data array and round up the dataLength.
            // Must be done after compactData() because that assumes that dataLength
            // is a multiple of DATA_BLOCK_LENGTH.
            _data[_dataLength++] = highValue;
            while ((_dataLength & (DATA_GRANULARITY - 1)) != 0)
            {
                _data[_dataLength++] = _initialValue;
            }

            _isCompacted = true;
        }

        public UnicodeTrie Freeze()
        {
            int allIndexesLength, i;
            if (!_isCompacted)
            {
                compact();
            }

            if (_highStart <= 0x10000)
            {
                allIndexesLength = INDEX_1_OFFSET;
            }
            else
            {
                allIndexesLength = _index2Length;
            }

            var dataMove = allIndexesLength;

            // are indexLength and dataLength within limits?
            if ((allIndexesLength > MAX_INDEX_LENGTH) || // for unshifted indexLength
              ((dataMove + _dataNullOffset) > 0xffff) || // for unshifted dataNullOffset
              ((dataMove + DATA_0800_OFFSET) > 0xffff) || // for unshifted 2-byte UTF-8 index-2 values
              ((dataMove + _dataLength) > MAX_DATA_LENGTH_RUNTIME))
            { // for shiftedDataLength
                throw new InvalidOperationException("Trie data is too large.");
            }

            // calculate the sizes of, and allocate, the index and data arrays
            var indexLength = allIndexesLength + _dataLength;
            var data = new int[indexLength];

            // write the index-2 array values shifted right by INDEX_SHIFT, after adding dataMove
            var destIdx = 0;
            for (i = 0; i < INDEX_2_BMP_LENGTH; i++)
            {
                data[destIdx++] = ((_index2[i] + dataMove) >> INDEX_SHIFT);
            }

            // write UTF-8 2-byte index-2 values, not right-shifted
            for (i = 0; i < 0xc2 - 0xc0; i++)
            { // C0..C1
                data[destIdx++] = (dataMove + BAD_UTF8_DATA_OFFSET);
            }

            for (; i < 0xe0 - 0xc0; i++)
            { // C2..DF
                data[destIdx++] = (dataMove + _index2[i << (6 - SHIFT_2)]);
            }

            if (_highStart > 0x10000)
            {
                var index1Length = (_highStart - 0x10000) >> SHIFT_1;
                var index2Offset = INDEX_2_BMP_LENGTH + UTF8_2B_INDEX_2_LENGTH + index1Length;

                // write 16-bit index-1 values for supplementary code points
                for (i = 0; i < index1Length; i++)
                {
                    data[destIdx++] = (INDEX_2_OFFSET + _index1[i + OMITTED_BMP_INDEX_1_LENGTH]);
                }

                // write the index-2 array values for supplementary code points,
                // shifted right by INDEX_SHIFT, after adding dataMove
                for (i = 0; i < _index2Length - index2Offset; i++)
                {
                    data[destIdx++] = ((dataMove + _index2[index2Offset + i]) >> INDEX_SHIFT);
                }
            }

            // write 16-bit data values
            for (i = 0; i < _dataLength; i++)
            {
                data[destIdx++] = (int)_data[i];
            }

            return new UnicodeTrie(data, _highStart, _errorValue);
        }

    }
}
