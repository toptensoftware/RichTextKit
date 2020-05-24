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
    /// Miscellaneous utility functions for working with UTF-32 data.
    /// </summary>
    public static class Utf32Utils
    {
        /// <summary>
        /// Convert a slice of UTF-32 integer code points to a string
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
        /// Converts a string to an integer array of UTF-32 code points
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
