using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Schema;

namespace Topten.RichTextKit.Utils
{
    /// <summary>
    /// Interface to a run object with a start offset and a length
    /// </summary>
    public interface IRun
    {
        /// <summary>
        /// Offset of the start of this run
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Length of this run
        /// </summary>
        int Length { get; }
    }

    /// <summary>
    /// Represents a sub-run in a list of runs
    /// </summary>
    [DebuggerDisplay("[{Index}] [{Offset} + {Length} = {Offset + Length}] {Partial}")]
    public struct SubRun
    {
        /// <summary>
        /// Constructs a new sub-run
        /// </summary>
        /// <param name="index">The run index</param>
        /// <param name="offset">The sub-run offset</param>
        /// <param name="length">The sub-run length</param>
        /// <param name="partial">True if the sub-run is partial run</param>
        public SubRun(int index, int offset, int length, bool partial)
        {
            Index = index;
            Offset = offset;
            Length = length;
            Partial = partial;
        }

        /// <summary>
        /// The index of the run in the list of runs
        /// </summary>
        public int Index;

        /// <summary>
        /// Offset of this sub-run in the containing run
        /// </summary>
        public int Offset;

        /// <summary>
        /// Length of this sub-run in the containing run
        /// </summary>
        public int Length;

        /// <summary>
        /// Indicates if this sub-run is partial sub-run
        /// </summary>
        public bool Partial;
    }

    /// <summary>
    /// Helpers for iterating over a set of consecutive runs
    /// </summary>
    public static class RunExtensions
    {
        /// <summary>
        /// Given a list of consecutive runs, a start index and a length
        /// provides a list of sub-runs in the list of runs.
        /// </summary>
        /// <typeparam name="T">The list element type</typeparam>
        /// <param name="list">The list of runs</param>
        /// <param name="offset">The offset of the run</param>
        /// <param name="length">The length of the run</param>
        /// <returns>An enumerable collection of SubRuns</returns>
        public static IEnumerable<SubRun> GetInterectingRuns<T>(this IReadOnlyList<T> list, int offset, int length) where T : IRun
        {
            // Check list is consistent
            list.CheckValid();

            // Calculate end position
            int to = offset + length;

            // Find the start run
            int startRunIndex = list.BinarySearch(offset, (r, a) =>
            {
                if (r.Offset > a)
                    return 1;
                if (r.Offset + r.Length <= a)
                    return -1;
                return 0;
            });
            Debug.Assert(startRunIndex >= 0);
            Debug.Assert(startRunIndex < list.Count);

            // Iterate over all runs
            for (int i = startRunIndex; i < list.Count; i++)
            {
                // Get the run
                var r = list[i];

                // Quit if past requested run
                if (r.Offset >= to)
                    break;

                // Yield sub-run
                var sr = new SubRun();
                sr.Index = i;
                sr.Offset = i == startRunIndex ? offset - r.Offset : 0;
                sr.Length = Math.Min(r.Offset + r.Length, to) - r.Offset - sr.Offset;
                sr.Partial = r.Length != sr.Length;
                yield return sr;
            }
        }

        /// <summary>
        /// Given a list of consecutive runs, a start index and a length
        /// provides a list of sub-runs in the list of runs (in reverse order)
        /// </summary>
        /// <typeparam name="T">The list element type</typeparam>
        /// <param name="list">The list of runs</param>
        /// <param name="offset">The offset of the run</param>
        /// <param name="length">The length of the run</param>
        /// <returns>An enumerable collection of SubRuns</returns>
        public static IEnumerable<SubRun> GetIntersectingRunsReverse<T>(this IReadOnlyList<T> list, int offset, int length) where T : IRun
        {
            // Check list is consistent
            list.CheckValid();

            // Calculate end position
            int to = offset + length;

            // Find the start run
            int endRunIndex = list.BinarySearch(to, (r, a) =>
            {
                if (r.Offset >= a)
                    return 1;
                if (r.Offset + r.Length < a)
                    return -1;
                return 0;
            });
            Debug.Assert(endRunIndex >= 0);
            Debug.Assert(endRunIndex < list.Count);

            // Iterate over all runs
            for (int i = endRunIndex; i >= 0; i--)
            {
                // Get the run
                var r = list[i];

                // Quit if past requested run
                if (r.Offset + r.Length <= offset)
                    break;

                // Yield sub-run
                var sr = new SubRun();
                sr.Index = i;
                sr.Offset = r.Offset > offset ? 0 : offset - r.Offset;
                sr.Length = Math.Min(r.Offset + r.Length, to) - r.Offset - sr.Offset;
                sr.Partial = r.Length != sr.Length;
                yield return sr;
            }
        }

        /// <summary>
        /// Get the total length of a list of consecutive runs
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="list">The list of runs</param>
        /// <returns>The total length</returns>
        public static int TotalLength<T>(this IReadOnlyList<T> list) where T : IRun
        {
            // Empty list?
            if (list.Count == 0)
                return 0;

            // Get length from last element
            var last = list[list.Count - 1];
            return last.Offset + last.Length;
        }

        /// <summary>
        /// Check that a list of runs is valid
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="list">The list to be checked</param>
        [Conditional("DEBUG")]
        public static void CheckValid<T>(this IReadOnlyList<T> list) where T : IRun
        {
            CheckValid(list, list.TotalLength());
        }

        /// <summary>
        /// Check that a list of runs is valid
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="list">The list to be checked</param>
        /// <param name="totalLength">The expected total length of the list of runs</param>
        [Conditional("DEBUG")]
        public static void CheckValid<T>(this IReadOnlyList<T> list, int totalLength) where T : IRun
        {
            if (list.Count > 0)
            {
                // Must start at zero
                Debug.Assert(list[0].Offset == 0);

                // Must cover entire code point buffer
                Debug.Assert(list[list.Count - 1].Offset + list[list.Count - 1].Length == totalLength);

                var prev = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    // All runs must have a length
                    Debug.Assert(list[i].Length > 0);

                    // All runs must be consecutive and joined end to end
                    Debug.Assert(list[i].Offset == prev.Offset + prev.Length);

                    prev = list[i];
                }
            }
            else
            {
                // If no style runs then mustn't have any code points either
                Debug.Assert(totalLength == 0);
            }
        }

    }
}
