using System;
using SkiaSharp;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class FontFallbackTests
    {
        // An SKFont that is never disposed is finalizable garbage, so the number of
        // objects the GC finds waiting for finalization after a burst of calls is a
        // proxy for undisposed fonts.  Disposed fonts suppress finalization.
        [Fact]
        void GetFontRunsDisposesFont()
        {
            // Arrange
            const int callCount = 2000;
            var codePoints = new Utf32Buffer("The quick brown fox jumps over the lazy dog").AsSlice();
            var typeface = SKTypeface.FromFamilyName("Arial");

            // Clear out anything left over from earlier work so it isn't counted
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Act
            CallRepeatedly(codePoints, typeface, callCount);
            GC.Collect();
            var pending = GC.GetGCMemoryInfo().FinalizationPendingCount;
            GC.WaitForPendingFinalizers();

            // Assert
            // A leak would show up as roughly one object per call
            Assert.True(pending < callCount / 10,
                $"{callCount} calls to GetFontRuns left {pending} objects awaiting finalization");
        }

        // Kept out of line so nothing from the loop stays reachable from the caller's frame
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void CallRepeatedly(Slice<int> codePoints, SKTypeface typeface, int callCount)
        {
            for (int i = 0; i < callCount; i++)
            {
                foreach (var run in FontFallback.GetFontRuns(codePoints, typeface))
                {
                }
            }
        }
    }
}
