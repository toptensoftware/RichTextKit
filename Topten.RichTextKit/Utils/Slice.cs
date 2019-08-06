using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Topten.RichTextKit.Utils
{
    /// <summary>
    /// Represents a slice of an array
    /// </summary>
    /// <typeparam name="T">The array type</typeparam>
    [DebuggerDisplay("Length = {Length}")]
    public struct Slice<T> : IEnumerable<T>, System.Collections.IEnumerable
    {
        /// <summary>
        /// Constructs a new slice covering the entire passed array.
        /// </summary>
        /// <param name="array"></param>
        public Slice(T[] array)
            : this(array, 0, array.Length)
        {
        }

        /// <summary>
        /// Constructs a new slice for part of the passed array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        public Slice(T[] array, int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (start + length > array.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            _array = array;
            _start = start;
            _length = length;
        }

        /// <summary>
        /// Gets the length of the array slice.
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _length;
            }

        }

        /// <summary>
        /// Clears the entire slice content
        /// </summary>
        public void Clear()
        {
            System.Array.Clear(_array, _start, _length);
        }

        /// <summary>
        /// Fill the slice with a specified value
        /// </summary>
        /// <param name="value"></param>
        public void Fill(T value)
        {
            for (int i = 0; i < _length; i++)
            {
                _array[i + _start] = value;
            }
        }

        /// <summary>
        /// Gets a reference to an element in the slice
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
                return ref _array[_start + index];
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        T[] _array;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int _start;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int _length;

        /// <summary>
        /// Creates a sub-slice of this slice
        /// </summary>
        /// <param name="start">The slice start index</param>
        /// <param name="length">The slice length</param>
        /// <returns>A new array slice</returns>
        public Slice<T> SubSlice(int start, int length)
        {
            return new Slice<T>(_array, _start + start, length);
        }

        /// <summary>
        /// Creates a subslice of an array slice, from a specified position to the end
        /// </summary>
        /// <param name="start">The slice start index</param>
        /// <returns>A new array slice</returns>
        public Slice<T> SubSlice(int start)
        {
            return SubSlice(start, Length - start);
        }

        /// <summary>
        /// Gets the slice contents as a new array  
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            var array = new T[_length];
            System.Array.Copy(_array, _start, array, 0, _length);
            return array;
        }

        /// <summary>
        /// Gets the underlying array
        /// </summary>
        public T[] Underlying => _array;

        /// <summary>
        /// Gets the offset of this slice within the underlying array
        /// </summary>
        public int Start => _start;

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new ArraySliceEnumerator<T>(_array, _start, _length);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ArraySliceEnumerator<T>(_array, _start, _length);
        }
    }
}
