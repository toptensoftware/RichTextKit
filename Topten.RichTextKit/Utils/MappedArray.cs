using System.Runtime.CompilerServices;

namespace Topten.RichTextKit.Utils
{
    /// <summary>
    /// Provides a mapped view of an array, selecting arbitrary indicies
    /// from the source array
    /// </summary>
    /// <typeparam name="T">The element type of the underlying array</typeparam>
    public class MappedArray<T>
    {
        /// <summary>
        /// Constructs a new mapped array
        /// </summary>
        public MappedArray()
        {
        }

        Buffer<int> _mapping = new Buffer<int>();
        T[] _data;
        int _dataOffset;
        int _dataLength;

        /// <summary>
        /// Resets the mapped array and sets the underlying array
        /// </summary>
        /// <param name="array">The array to be mapped</param>
        public void Reset(T[] array)
        {
            _data = array;
            _dataOffset = 0;
            _dataLength = array.Length;
            _mapping.Clear();
        }

        /// <summary>
        /// Resets the mapped array and sets the underlying array from a Slice
        /// </summary>
        /// <param name="slice"></param>
        public void Reset(Slice<T> slice)
        {
            _data = slice.Underlying;
            _dataOffset = slice.Start;
            _dataLength = slice.Length;
            _mapping.Clear();
        }

        /// <summary>
        /// Add a range of indicies from the underlying array to this mapped array
        /// </summary>
        /// <param name="from">Index of the first element to mapped from the underlying array</param>
        /// <param name="length">The number of elements from the underlying array to be mapped</param>
        public void MapRange(int from, int length)
        {
            // Get mapped section
            var map = _mapping.Add(length, false);

            // Apply the data offset
            from += _dataOffset;

            // Store indicies
            for (int i = 0; i < length; i++)
            {
                map[i] = from + i;
            }
        }

        /// <summary>
        /// Map a single element from the underlying array into this mapping
        /// </summary>
        /// <param name="from">The index of the item in the underlying array to be mapped</param>
        public void Map(int from)
        {
            MapRange(from, 1);
        }

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
    }
}
