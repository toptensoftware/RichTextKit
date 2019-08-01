using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichText
{
    /// <summary>
    /// A buffer of T
    /// </summary>
    /// <typeparam name="T">The buffer element type</typeparam>
    public class Buffer<T>
    {
        /// <summary>
        /// Constructs a new buffer
        /// </summary>
        public Buffer()
        {
            // Create initial array
            _data = new T[1024];
        }

        /// <summary>
        /// The data held by this buffer
        /// </summary>
        T[] _data;

        /// <summary>
        /// The used length of the buffer
        /// </summary>
        int _length;

        /// <summary>
        /// Get or set the length of the buffer
        /// </summary>
        public int Length
        {
            get => _length;
            set
            {
                if (value > _data.Length)
                {
                    var newData = new T[value];
                    Array.Copy(_data, 0, newData, 0, _data.Length);
                    _data = newData;
                }
                _length = value;
            }
        }

        /// <summary>
        /// Clear the buffer (keep internal allocated memory)
        /// </summary>
        public void Clear()
        {
            _length = 0;
        }

        /// <summary>
        /// Add to the buffer, returning a slice of requested size
        /// </summary>
        /// <param name="length">Number of items to add</param>
        /// <param name="clear">True to clear the content</param>
        /// <returns>A slice representing the allocated area</returns>
        public Slice<T> Add(int length, bool clear = true)
        {
            // Grow internal buffer?
            if (_length + length > _data.Length)
            {
                var newData = new T[_length + length + 1024];
                Array.Copy(_data, 0, newData, 0, _length);
                _data = newData;
            }

            // Clear it?
            if (clear)
                Array.Clear(_data, _length, length);

            // Capture where it was placed
            var pos = _length;

            // Update length
            _length += length;

            // Return position
            return SubSlice(pos, length);
        }

        /// <summary>
        /// Add a slice of data to this buffer
        /// </summary>
        /// <param name="slice">The slice to be added</param>
        public Slice<T> Add(Slice<T> slice)
        {
            // Grow internal buffer?
            if (_length + slice.Length > _data.Length)
            {
                var newData = new T[_length + slice.Length + 1024];
                Array.Copy(_data, 0, newData, 0, _length);
                _data = newData;
            }

            // Copy in the slice
            Array.Copy(slice.Underlying, slice.Start, _data, _length, slice.Length);

            // Capture where it was placed
            var pos = _length;

            // Update length
            _length += slice.Length;

            // Return position
            return SubSlice(pos, slice.Length);
        }

        /// <summary>
        /// Get/set an element in the slice
        /// </summary>
        /// <param name="index">The element index</param>
        /// <returns>The element value</returns>
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
        /// Create a subslice of data from this array
        /// </summary>
        /// <param name="start">Start offset of the slice</param>
        /// <param name="length">Length of the slice</param>
        /// <returns>A Slice for the specified sub-range</returns>
        public Slice<T> SubSlice(int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (start + length > _length)
                throw new ArgumentOutOfRangeException(nameof(length));

            return new Slice<T>(_data, start, length);
        }

        /// <summary>
        /// Get the entire buffer contents as a slice
        /// </summary>
        /// <returns>A Slice</returns>
        public Slice<T> AsSlice()
        {
            return SubSlice(0, _length);
        }
    }
}
