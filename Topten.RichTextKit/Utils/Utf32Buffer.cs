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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit.Utils
{
    /// <summary>
    /// Represents a buffer of UTF-32 encoded code point data
    /// </summary>
    public class Utf32Buffer : Buffer<int>
    {
        /// <summary>
        /// Constructs a new Utf32Buffer
        /// </summary>
        public Utf32Buffer()
        {
        }

        /// <summary>
        /// Constructs a Utf32 buffer with an initial string
        /// </summary>
        /// <param name="str">The string to initialize with</param>
        public Utf32Buffer(string str)
        {
            Add(str);
        }

        /// <summary>
        /// Clears this buffer.
        /// </summary>
        public new void Clear()
        {
            _surrogatePositionsValid = false;
            base.Clear();
        }

        /// <summary>
        /// Appends utf32 data to this buffer
        /// </summary>
        /// <param name="data">The UTF32 data to be appended</param>
        /// <returns>A slice representing the added UTF-32 data.</returns>
        public new Slice<int> Add(Slice<int> data)
        {
            _surrogatePositionsValid = false;
            return base.Add(data);
        }

        /// <summary>
        /// Appends text to this buffer, converting from UTF-16 to UTF-32
        /// </summary>
        /// <param name="str">The string of text to be inserted</param>
        /// <returns>A slice representing the added UTF-32 data.</returns>
        public Slice<int> Add(string str)
        {
            return Insert(Length, str);
        }

        /// <summary>
        /// Appends text to this buffer, converting from UTF-16 to UTF-32
        /// </summary>
        /// <param name="str">The string of text to be inserted</param>
        /// <returns>A slice representing the added UTF-32 data.</returns>
        public Slice<int> Add(ReadOnlySpan<char> str)
        {
            return Insert(Length, str);
        }

        /// <summary>
        /// Appends utf32 data to this buffer
        /// </summary>
        /// <param name="position">Position to insert the string</param>
        /// <param name="data">The string of text to be appended</param>
        /// <returns>A slice representing the added UTF-32 data.</returns>
        public new Slice<int> Insert(int position, Slice<int> data)
        {
            _surrogatePositionsValid = false;
            return base.Insert(position, data);
        }

        /// <summary>
        /// Inserts text to this buffer, converting from UTF-16 to UTF-32
        /// </summary>
        /// <param name="position">The position to insert the string</param>
        /// <param name="str">The string of text to be inserted</param>
        /// <returns>A slice representing the added UTF-32 data.</returns>
        public Slice<int> Insert(int position, string str)
        {
            return Insert(position, str.AsSpan());
        }

        /// <summary>
        /// Inserts text to this buffer, converting from UTF-16 to UTF-32
        /// </summary>
        /// <param name="position">The position to insert the string</param>
        /// <param name="str">The string of text to be inserted</param>
        /// <returns>A slice representing the added UTF-32 data.</returns>
        public Slice<int> Insert(int position, ReadOnlySpan<char> str)
        {
            // Remember old length
            int oldLength = Length;

            // Invalidate surrogate positions
            _surrogatePositionsValid = false;

            // For performance reasons and to save copying to intermediate arrays if we use 
            // (Encoding.UTF32), we do our own utf16 to utf32 decoding directly to our 
            // internal code point buffer.  Also stores the indicies of any surrogate pairs 
            // for later back conversion.  
            // Also use pointers for performance reasons too (maybe)
            Slice<int> codePointBuffer = base.Insert(position, str.Length);
            int convertedLength;
            unsafe
            {
                fixed (int* pDestBuf = codePointBuffer.Underlying)
                fixed (char* pSrcBuf = str)
                {
                    int* pDestStart = pDestBuf + codePointBuffer.Start;
                    int* pDest = pDestStart;
                    char* pSrc = pSrcBuf;
                    char* pSrcEnd = pSrcBuf + str.Length;
                    while (pSrc < pSrcEnd)
                    {
                        char ch = *pSrc++;

                        if (ch >= 0xD800 && ch <= 0xDFFF)
                        {
                            if (ch <= 0xDBFF)
                            {
                                // High surrogate
                                var chL = pSrc < pSrcEnd ? (*pSrc++) : 0;
                                *pDest++ = 0x10000 | ((ch - 0xD800) << 10) | (chL - 0xDC00);
                            }
                            else
                            {
                                // Single low surrogte?
                                *pDest++ = 0x10000 + ch - 0xDC00;
                            }
                        }
                        else
                        {
                            *pDest++ = ch;
                        }
                    }

                    // Work out the converted length
                    convertedLength = (int)(pDest - pDestStart);
                }
            }

            // If converted length was shorter due to surrogates, then remove
            // the extra space that was allocated
            if (convertedLength < str.Length)
            {
                base.Delete(position + convertedLength, str.Length - convertedLength);
            }

            // Return the encapsulating slice
            return SubSlice(position, convertedLength);
        }

        /// <summary>
        /// Delete a section of the buffer
        /// </summary>
        /// <param name="from">The position to delete from</param>
        /// <param name="length">The length to of the deletion</param>
        public new void Delete(int from, int length)
        {
            _surrogatePositionsValid = false;
            base.Delete(from, length);
        }



        /// <summary>
        /// Convers an offset into this buffer to a UTF-16 offset in the originally 
        /// added string.
        /// </summary>
        /// <remarks>
        /// This function assumes the was text added to the buffer as UTF-16
        /// and hasn't been modified in any way since.
        /// </remarks>
        /// <param name="utf32Offset">The UTF-3232 offset to convert</param>
        /// <returns>The converted UTF-16 character offset</returns>
        public int Utf32OffsetToUtf16Offset(int utf32Offset)
        {
            // Make sure surrorgate positions are valid
            BuildSurrogatePositions();

            // How many surrogate pairs were there before this utf32 offset?
            int pos = _surrogatePositions.BinarySearch(utf32Offset);
            if (pos < 0)
            {
                pos = ~pos;
            }

            return utf32Offset + pos;
        }

        /// <summary>
        /// Converts an offset in the original UTF-16 string, a code point index into 
        /// this UTF-32 buffer.
        /// </summary>
        /// <param name="utf16Offset">The utf-16 character index</param>
        /// <returns>The utf-32 code point index</returns>
        public int Utf16OffsetToUtf32Offset(int utf16Offset)
        {
            // Make sure surrorgate positions are valid
            BuildSurrogatePositions();

            var pos = utf16Offset;
            for (int i = 0; i < _surrogatePositions.Count; i++)
            {
                var sp = _surrogatePositions[i];
                if (sp < pos)
                    pos--;
                if (sp > pos)
                    return pos;
            }
            return pos;
        }

        /// <summary>
        /// Gets the enture buffer's content as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Utf32Utils.FromUtf32(AsSlice());
        }

        /// <summary>
        /// Gets a part of the buffer as a string.
        /// </summary>
        /// <param name="start">The UTF-32 code point index of the first character to retrieve</param>
        /// <param name="length">The number of code points in the string to be retrieved</param>
        /// <returns>A string equivalent to the specified code point range.</returns>
        public string GetString(int start, int length)
        {
            return Utf32Utils.FromUtf32(SubSlice(start, length));
        }

        /// <summary>
        /// Indicies of all code points in the in the buffer
        /// that were decoded from a surrogate pair
        /// </summary>
        List<int> _surrogatePositions = new List<int>();
        bool _surrogatePositionsValid = false;

        /// <summary>
        /// Build an array indicies to all characters that require surrogates
        /// when converted to utf16.
        /// </summary>
        void BuildSurrogatePositions()
        {
            if (_surrogatePositionsValid)
                return;
            _surrogatePositionsValid = true;

            _surrogatePositions.Clear();
            unsafe
            {
                fixed (int* pBuf = this.Underlying)
                {
                    int* pEnd = pBuf + this.Length;
                    int* p = pBuf;
                    while (p < pEnd)
                    {
                        if (p[0] >= 0x10000)
                            _surrogatePositions.Add((int)(p - pBuf));
                        p++;
                    }
                }
            }
        }
    }
}
