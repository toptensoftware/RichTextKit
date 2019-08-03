using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichTextKit
{
    /// <summary>
    /// A buffer of utf-32 character data
    /// </summary>
    public class Utf32Buffer : Buffer<int>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Utf32Buffer()
        {
        }

        /// <summary>
        /// Constructs a UTF32 buffer with an initial string
        /// </summary>
        /// <param name="str">The string to initialize with</param>
        public Utf32Buffer(string str)
        {
            Add(str);
        }

        /// <summary>
        /// Clear this text buffer
        /// </summary>
        public new void Clear()
        {
            _surrogatePositions.Clear();
            base.Clear();
        }

        /// <summary>
        /// Add text to this UTF-32 buffer
        /// </summary>
        /// <param name="str">The text to add</param>
        /// <returns>A slice representing the added UTF-32 data</returns>
        public Slice<int> Add(string str)
        {
            // Remember old length
            int oldLength = Length;

            // For performance reasons and to save copying to intermediate arrays if we use 
            // (Encoding.UTF32), we do our own utf16 to utf32 decoding directly to our 
            // internal code point buffer.  Also stores the indicies of any surrogate pairs 
            // for later back conversion.  
            // Also use pointers for performance reasons too (maybe)
            Slice<int> codePointBuffer = this.Add(str.Length);
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

                        // Normalize line endings to '\n'
                        if (ch == '\r' && pSrc < pSrcEnd && *pSrc == '\n')
                        {
                            *pDest++ = '\n';
                            pSrc++;
                            _surrogatePositions.Add((int)(pDest - pDestBuf - 1));
                        }
                        else if (ch >= 0xD800 && ch <= 0xDFFF)
                        {
                            if (ch <= 0xDBFF)
                            {
                                // High surrogate
                                var chL = pSrc < pSrcEnd ? (*pSrc++) : 0;
                                *pDest++ = 0x10000 | ((ch - 0xD800) << 10) | (chL - 0xDC00);
                                _surrogatePositions.Add((int)(pDest - pDestBuf - 1));
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

                    // Truncate length of buffer (may be shorter due to surrogates)
                    this.Length = (int)(pDest - pDestBuf);
                }
            }

            // Return the encapsulating slice
            return SubSlice(oldLength, Length - oldLength);
        }


        /// <summary>
        /// Convers an offset into this utf32 buffer to a utf16 offset
        /// </summary>
        /// <remarks>
        /// This function assumes the was text added to the buffer as utf16
        /// and hasn't been modified in any way since.
        /// </remarks>
        /// <param name="utf32Offset">The utf32 offset to convert</param>
        /// <returns>The utf16 character offset</returns>
        public int Utf32OffsetToUtf16Offset(int utf32Offset)
        {
            // How many surrogate pairs were there before this utf32 offset?
            int pos = _surrogatePositions.BinarySearch(utf32Offset);
            if (pos < 0)
            {
                pos = ~pos;
            }

            return utf32Offset + pos;
        }

        /// <summary>
        /// Given a utf-16 index, convert it to a utf-32 index
        /// </summary>
        /// <param name="utf16Offset">The utf-16 character index</param>
        /// <returns>The utf-32 code point index</returns>
        public int Utf16OffsetToUtf32Offset(int utf16Offset)
        {
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
        /// Get the enture buffer's content as a string
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            return Utf32Utils.FromUtf32(AsSlice());
        }

        /// <summary>
        /// Get a slide of the buffer as a string
        /// </summary>
        /// <param name="start">Start offset (in utf-32 coords)</param>
        /// <param name="length">Length of the slice (in utf-32 coords)</param>
        /// <returns></returns>
        public string GetString(int start, int length)
        {
            return Utf32Utils.FromUtf32(SubSlice(start, length));
        }

        /// <summary>
        /// Indicies of all code points in the in the buffer
        /// that were decoded from a surrogate pair
        /// </summary>
        List<int> _surrogatePositions = new List<int>();
    }
}
