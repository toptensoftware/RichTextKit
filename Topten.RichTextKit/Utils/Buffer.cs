using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
        /// Adds to the buffer, returning a slice of requested size
        /// </summary>
        /// <param name="length">Number of elements to add</param>
        /// <param name="clear">True to clear the content; otherwise false</param>
        /// <returns>A slice representing the allocated elements.</returns>
        public Slice<T> Add(int length, bool clear = true)
        {
            // Save position
            int pos = Length;

            // Grow internal buffer?
            GrowBuffer(_length + length);
            _length += length;

            // Clear it?
            if (clear)
                Array.Clear(_data, pos, length);

            // Return subslice
            return SubSlice(pos, length);
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
