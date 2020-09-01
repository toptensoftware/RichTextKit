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
using System.Threading;
using System.Threading.Tasks;
using Topten.RichTextKit.Utils;

namespace Topten.RichTextKit
{
    /// <summary>
    /// Represets a style run - a logical run of characters all with the same
    /// style.
    /// </summary>
    public class StyleRun : IRun
    {
        /// <summary>
        /// Get the code points of this run.
        /// </summary>
        public Slice<int> CodePoints => CodePointBuffer.SubSlice(Start, Length);

        /// <summary>
        /// Get the text of this style run
        /// </summary>
        /// <returns>A string</returns>
        public override string ToString()
        {
            return Utf32Utils.FromUtf32(CodePoints);
        }

        /// <summary>
        /// The index of the first code point in this run (relative to the text block
        /// as a whole).
        /// </summary>
        public int Start
        {
            get;
            internal set;
        }

        /// <summary>
        /// The number of code points this run.
        /// </summary>
        public int Length
        {
            get;
            internal set;
        }

        /// <summary>
        /// The index of the first code point after this run.
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// The style attributes to be applied to text in this run.
        /// </summary>
        public IStyle Style
        {
            get;
            internal set;
        }

        int IRun.Offset => Start;
        int IRun.Length => Length;

        /// <summary>
        /// The global list of code points
        /// </summary>
        internal Buffer<int> CodePointBuffer;

        internal static ThreadLocal<ObjectPool<StyleRun>> Pool = new ThreadLocal<ObjectPool<StyleRun>>(() => new ObjectPool<StyleRun>()
        {
            Cleaner = (r) =>
            {
                r.CodePointBuffer = null;
                r.Style = null;
            }
        });
    }
}
