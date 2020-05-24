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

using System.Runtime.CompilerServices;

namespace Topten.RichTextKit.Utils
{
    /// <summary>
    /// Provides a mapped view of an underlying slice array, selecting arbitrary indicies
    /// from the source array
    /// </summary>
    /// <typeparam name="T">The element type of the underlying array</typeparam>
    public struct MappedSlice<T>
    {
        /// <summary>
        /// Constructs a new mapped array
        /// </summary>
        /// <param name="data">The data to be mapped</param>
        /// <param name="mapping">The index map</param>
        public MappedSlice(Slice<T> data, Slice<int> mapping)
        {
            _data = data;
            _mapping = mapping;
        }

        Slice<T> _data;
        Slice<int> _mapping;

        /// <summary>
        /// Get the underlying slice for this mapped array
        /// </summary>
        public Slice<T> Underlying => _data;

        /// <summary>
        /// Get the index mapping for this mapped array
        /// </summary>
        public Slice<int> Mapping => _mapping;

        /// <summary>
        /// Gets the number of elements in this mapping
        /// </summary>
        public int Length => _mapping.Length;

        /// <summary>
        /// Gets a reference to a mapped element 
        /// </summary>
        /// <param name="index">The mapped index to be retrieved</param>
        /// <returns>A reference to the element</returns>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref _data[_mapping[index]];
            }
        }

        /// <summary>
        /// Get the content of this mapped slice as an array
        /// </summary>
        /// <returns>The content as an array</returns>
        public T[] ToArray()
        {
            var arr = new T[Length];
            for (int i = 0; i < Length; i++)
            {
                arr[i] = this[i];
            }
            return arr;
        }
    }
}
