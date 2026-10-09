using System;
using SkiaSharp;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextBlockPaintTests
    {
        // An SKPaint that is never disposed is finalizable garbage, so the number of
        // objects the GC finds waiting for finalization after a burst of Paint calls
        // is a proxy for undisposed paints.  Disposed paints suppress finalization.
        [Fact]
        void PaintDisposesSelectionHandlePaint()
        {
            // Arrange
            const int paintCount = 2000;
            var withoutHandles = new TextPaintOptions()
            {
                Selection = new TextRange(0, 3),
                SelectionColor = SKColors.Blue,
            };
            var withHandles = withoutHandles.Clone();
            withHandles.SelectionHandleColor = SKColors.Red;
            withHandles.SelectionHandleScale = 1;

            // Act
            long baseline = CountPendingFinalizers(withoutHandles, paintCount);
            long withHandlesCount = CountPendingFinalizers(withHandles, paintCount);

            // Assert
            // Everything else Paint allocates is the same in both runs, so the
            // difference is only the selection handle paints.  Allow a little noise
            // from the GC, but a leak would show up as roughly paintCount.
            Assert.True(withHandlesCount - baseline < paintCount / 10,
                $"Painting with selection handles left {withHandlesCount - baseline} extra objects awaiting finalization over {paintCount} paints");
        }

        static long CountPendingFinalizers(TextPaintOptions options, int paintCount)
        {
            // Clear out anything left over from earlier work so it isn't counted
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            PaintRepeatedly(options, paintCount);

            // Force a collection and read how many finalizable objects it found dead
            GC.Collect();
            var pending = GC.GetGCMemoryInfo().FinalizationPendingCount;
            GC.WaitForPendingFinalizers();
            return pending;
        }

        // Kept out of line so nothing from the loop stays reachable from the caller's frame
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void PaintRepeatedly(TextPaintOptions options, int paintCount)
        {
            using (var bitmap = new SKBitmap(100, 50))
            using (var canvas = new SKCanvas(bitmap))
            {
                var tb = new TextBlock();
                tb.AddText("Hello world", new Style() { FontSize = 12 });

                for (int i = 0; i < paintCount; i++)
                {
                    tb.Paint(canvas, options);
                }
            }
        }
    }
}
