using System;
using System.Collections.Generic;
using System.Text;

namespace Topten.RichText
{
    public static class BinarySearchExtension
    {
        private static int GetMedian(int low, int hi)
        {
            System.Diagnostics.Debug.Assert(low <= hi);
            System.Diagnostics.Debug.Assert( hi - low >= 0, "Length overflow!");
            return low + ((hi - low) >> 1);
        }

        public static int BinarySearch<T, U>(this IReadOnlyList<T> list, U value, Func<T, U, int> compare)
        {
            return BinarySearch(list, 0, list.Count, value, compare);
        }

        // Based on this: https://referencesource.microsoft.com/#mscorlib/system/array.cs,957
        public static int BinarySearch<T, U>(this IReadOnlyList<T> list, int index, int length, U value, Func<T, U, int> compare)
        {
            int lo = index;
            int hi = index + length - 1;   
            while (lo <= hi)
            {
                int i = GetMedian(lo, hi);                    
                int c = compare(list[i], value);
                if (c == 0) return i;
                if (c < 0) {
                    lo = i + 1;
                }
                else {
                    hi = i - 1;
                }
            }
            return ~lo;
        }
    }
}
