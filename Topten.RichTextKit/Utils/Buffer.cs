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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Topten.RichTextKit.Utils
{
    /// <summary>
    /// A growable array of elements of type `T`
    /// </summary>
    /// <typeparam name="T">The buffer element type</typeparam>
    [DebuggerDisplay("Length = {Length}")]
    public class Buffer<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Constructs a new buffer.
        /// </summary>
        public Buffer()
        {
            // Create initial array
            _data = new T[32];
        }

        /// <summary>
        /// The data held by this buffer
        /// </summary>
        T[] _data;

        /// <summary>
        /// The data held by this buffer
        /// </summary>
        public T[] Underlying => _data;

        /// <summary>
        /// The used length of the buffer
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int _length;

        /// <summary>
        /// Gets or sets the length of the buffer
        /// </summary>
        /// <remarks>
        /// The internal buffer will be grown if the new length is larger
        /// than the current buffer size.
        /// </remarks>
        public int Length
        {
            get => _length;
            set
            {
                // Now grow buffer
                if (!GrowBuffer(value))
                {
                    // If the length is increasing, but we didn't re-size the buffer
                    // then we need to clear the new elements.
                    if (value > _length)
                    {
                        Array.Clear(_data, _length, value - _length);
                    }
                }

                // Store new length
                _length = value;
            }
        }

        /// <summary>
        /// Ensures the buffer has sufficient capacity
        /// </summary>
        /// <param name="requiredLength"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool GrowBuffer(int requiredLength)
        {
            if (requiredLength <= _data.Length)
                return false;

            // Work out new length
            int newLength;
            if (_data.Length < 1048576)
            {
                // Below 1MB, grow by doubling...
                newLength = _data.Length * 2;
            }
            else
            {
                // otherwise grow by 1mb at a time...
                newLength = _data.Length + 1048576;
            }

            // Make sure we're allocating enough
            if (newLength < requiredLength)
                newLength = requiredLength;

            // Allocate new buffer, only copying _length, not Data.Length
            var newData = new T[requiredLength];
            Array.Copy(_data, 0, newData, 0, _length);
            _data = newData;

            return true;
        }

        /// <summary>
        /// Clears the buffer, keeping the internally allocated array.
        /// </summary>
        public void Clear()
        {
            _length = 0;
        }

        /// <summary>
        /// Inserts room into the buffer
        /// </summary>
        /// <param name="position">The position to insert at</param>
        /// <param name="length">The length to insert</param>
        /// <param name="clear">Whether to clear the inserted part of the buffer</param>
        /// <returns>The new buffer area as a slice</returns>
        public Slice<T> Insert(int position, int length, bool clear = true)
        {
            // Grow internal buffer?
            GrowBuffer(_length + length);

            // Shuffle existing to make room for inserted data
            if (position < _length)
            {
                Array.Copy(_data, position, _data, position + length, _length - position);
            }
            
            // Update the length
            _length += length;

            // Clear it?
            if (clear)
                Array.Clear(_data, position, length);

            // Return slice
            return SubSlice(position, length);
        }

        /// <summary>
        /// Insert a slice of data into this buffer
        /// </summary>
        /// <param name="position">The position to insert at</param>
        /// <param name="data">The data to insert</param>
        /// <returns>The newly inserted data as a slice</returns>
        public Slice<T> Insert(int position, Slice<T> data)
        {
            // Make room
            var slice = Insert(position, data.Length, false);

            // Copy in the source slice
            Array.Copy(data.Underlying, data.Start, _data, position, data.Length);

            // Return slice
            return slice;
        }

        /// <summary>
        /// Adds to the buffer, returning a slice of requested size
        /// </summary>
        /// <param name="length">Number of elements to add</param>
        /// <param name="clear">True to clear the content; otherwise false</param>
        /// <returns>A slice representing the allocated elements.</returns>
        public Slice<T> Add(int length, bool clear = true)
        {
            return Insert(_length, length, clear);
        }

        /// <summary>
        /// Add a slice of data to this buffer.
        /// </summary>
        /// <param name="slice">The slice to be added</param>
        /// <returns>A slice representing the added elements.</returns>
        public Slice<T> Add(Slice<T> slice)
        {
            var pos = _length;

            // Grow internal buffer?
            GrowBuffer(_length + slice.Length);
            _length += slice.Length;

            // Copy in the slice
            Array.Copy(slice.Underlying, slice.Start, _data, pos, slice.Length);

            // Return position
            return SubSlice(pos, slice.Length);
        }

        /// <summary>
        /// Delete a section of the buffer
        /// </summary>
        /// <param name="from">The position to delete from</param>
        /// <param name="length">The length to of the deletion</param>
        public void Delete(int from, int length)
        {
            // Clamp to buffer size
            if (from >= _length)
                return;
            if (from + length >= _length)
                length = _length - from;

            // Shuffle trailing data
            if (from + length < _length)
            {
                Array.Copy(_data, from + length, _data, from, _length - (from + length));
            }

            // Update length
            _length -= length;
        }

        /// <summary>
        /// Gets a reference to an element in the buffer
        /// </summary>
        /// <param name="index">The element index</param>
        /// <returns>A reference to the element value.</returns>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return ref _data[index];
            }
        }

        /// <summary>
        /// Returns a range within this buffer as a <see cref="Slice{T}"/>
        /// </summary>
        /// <param name="start">Start offset of the slice</param>
        /// <param name="length">Length of the slice</param>
        /// <returns>A slice for the specified sub-range</returns>
        public Slice<T> SubSlice(int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (start + length > _length)
                throw new ArgumentOutOfRangeException(nameof(length));

            return new Slice<T>(_data, start, length);
        }

        /// <summary>
        /// Returns the entire buffer contents as a <see cref="Slice{T}"/>
        /// </summary>
        /// <returns>A Slice</returns>
        public Slice<T> AsSlice()
        {
            return SubSlice(0, _length);
        }

        /// <summary>
        /// Split the utf32 buffer on a codepoint type
        /// </summary>
        /// <param name="delim">The delimiter</param>
        /// <returns>An enumeration of slices</returns>
        public IEnumerable<Slice<T>> Split(T delim)
        {
            int start = 0;
            for (int i = 0; i < Length; i++)
            {
                if (_data[i].Equals(delim))
                {
                    yield return SubSlice(start, i - start);
                    start = i + 1;
                }
            }
            yield return SubSlice(start, Length - start);
        }

        /// <summary>
        /// Split the utf32 buffer on a codepoint type
        /// </summary>
        /// <param name="delim">The delimiter to split on</param>
        /// <returns>An enumeration of offset/length for each range</returns>
        public IEnumerable<(int Offset, int Length)> GetRanges(T delim)
        {
            int start = 0;
            for (int i = 0; i < Length; i++)
            {
                if (_data[i].Equals(delim))
                {
                    yield return (start, i - start);
                    start = i + 1;
                }
            }
            yield return (start, Length - start);
        }

        /// <summary>
        /// Replaces all instances of a value in the buffer with another value
        /// </summary>
        /// <param name="oldValue">The value to replace</param>
        /// <param name="newValue">The new value</param>
        /// <returns>The number of replacements made</returns>
        public int Replace(T oldValue, T newValue)
        {
            int count = 0;
            for (int i = 0; i < Length; i++)
            {
                if (_data[i].Equals(oldValue))
                {
                    _data[i] = newValue;
                    count++;
                }
            }
            return count;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new ArraySliceEnumerator<T>(_data, 0, _length);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ArraySliceEnumerator<T>(_data, 0, _length);
        }
    }
}
