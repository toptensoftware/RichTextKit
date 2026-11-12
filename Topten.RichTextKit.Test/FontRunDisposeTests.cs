using System;
using System.Runtime.CompilerServices;
using SkiaSharp;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class FontRunDisposeTests
    {
        // An SKFont or SKTextBlob that is never disposed is finalizable garbage, so the
        // number of objects the GC finds waiting for finalization after a burst of
        // relayouts is a proxy for undisposed native objects.
        [Fact]
        void RelayoutDisposesPooledRunNativeObjects()
        {
            // Arrange
            const int relayoutCount = 2000;

            // Act
            long pending = CountPendingFinalizers(relayoutCount);

            // Assert
            // Each relayout returns the old runs to the pool.  A leak shows up as
            // roughly relayoutCount objects.  Allow a little noise from the GC.
            Assert.True(pending < relayoutCount / 10,
                $"{relayoutCount} relayouts left {pending} objects awaiting finalization");
        }

        // Returns one shared typeface so the mapper itself does not add native
        // objects (FromFamilyName returns a new wrapper per call)
        class SharedTypefaceMapper : FontMapper
        {
            static readonly SKTypeface typeface = SKTypeface.CreateDefault();

            public override SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants) => typeface;
        }

        static long CountPendingFinalizers(int relayoutCount)
        {
            // Clear out anything left over from earlier work so it isn't counted
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            RelayoutRepeatedly(relayoutCount);

            // Force a collection and read how many finalizable objects it found dead
            GC.Collect();
            var pending = GC.GetGCMemoryInfo().FinalizationPendingCount;
            GC.WaitForPendingFinalizers();
            return pending;
        }

        // Kept out of line so nothing from the loop stays reachable from the caller's frame
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RelayoutRepeatedly(int relayoutCount)
        {
            using (var bitmap = new SKBitmap(200, 100))
            using (var canvas = new SKCanvas(bitmap))
            {
                var tb = new TextBlock() { FontMapper = new SharedTypefaceMapper() };
                tb.AddText("Hello world", new Style() { FontSize = 12 });

                for (int i = 0; i < relayoutCount; i++)
                {
                    // Changing the width forces a relayout, then Paint creates the
                    // run's font and text blob
                    tb.MaxWidth = 100 + (i % 2);
                    tb.Paint(canvas);
                }
            }
        }
    }
}
