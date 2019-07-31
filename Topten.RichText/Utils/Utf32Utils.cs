using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    public static class Utf32Utils
    {
        /// <summary>
        /// Convert a slice of utf32 integer code points back to a string
        /// </summary>
        /// <param name="buffer">The code points to convert</param>
        /// <returns>A string</returns>
        public static string FromUtf32(Slice<int> buffer)
        {
            unsafe
            {
                fixed (int* p = buffer.Underlying)
                {
                    var pBuf = p + buffer.Start;
                    return new string((sbyte*)pBuf, 0, buffer.Length * sizeof(int), Encoding.UTF32);
                }
            }
        }

        /// <summary>
        /// Convert a string to an integer array of utf-32 code points
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns>The converted code points</returns>
        public static int[] ToUtf32(string str)
        {
            unsafe
            {
                fixed (char* pstr = str)
                {
                    // Get required byte count
                    int byteCount = Encoding.UTF32.GetByteCount(pstr, str.Length);
                    System.Diagnostics.Debug.Assert((byteCount % 4) == 0);

                    // Allocate buffer
                    int[] utf32 = new int[byteCount / sizeof(int)];
                    fixed (int* putf32 = utf32)
                    {
                        // Convert
                        Encoding.UTF32.GetBytes(pstr, str.Length, (byte*)putf32, byteCount);

                        // Done
                        return utf32;
                    }
                }
            }
        }
    }
}
