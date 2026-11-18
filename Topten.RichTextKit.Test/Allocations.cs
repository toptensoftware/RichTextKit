using System;

namespace Topten.RichTextKit.Test
{
    static class Allocations
    {
        /// <summary>
        /// Measures the bytes an action allocates per call on this thread, after a
        /// warm up so pools and buffers have already grown
        /// </summary>
        /// <remarks>
        /// The runtime now and then allocates on the calling thread for its own
        /// reasons (eg: when tiered compilation swaps in optimized code), so this
        /// takes the lowest of several measurements.
        /// </remarks>
        /// <param name="callCount">The number of calls to measure</param>
        /// <param name="action">The action, passed the index of the call</param>
        /// <returns>The bytes allocated per call</returns>
        public static long BytesPerCall(int callCount, Action<int> action)
        {
            const int warmupCount = 100;
            const int attempts = 3;

            for (int i = 0; i < warmupCount; i++)
                action(i);

            long best = long.MaxValue;
            for (int attempt = 0; attempt < attempts && best > 0; attempt++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < callCount; i++)
                    action(i);
                best = Math.Min(best, (GC.GetAllocatedBytesForCurrentThread() - before) / callCount);
            }
            return best;
        }
    }
}
