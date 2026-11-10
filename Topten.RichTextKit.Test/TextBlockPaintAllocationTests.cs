using System;
using SkiaSharp;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextBlockPaintAllocationTests
    {
        // Painting an already laid out text block shouldn't need to allocate
        // anything per paint, so a burst of paints should allocate next to nothing.
        [Fact]
        void PaintDoesNotAllocatePerRun()
        {
            // Arrange
            const int paintCount = 2000;
            var tb = new TextBlock();
            tb.AddText("The quick brown fox ", new Style() { FontSize = 16 });
            tb.AddText("jumps over ", new Style() { FontSize = 16, HaloColor = SKColors.Yellow, HaloWidth = 2 });
            tb.AddText("the lazy dog", new Style() { FontSize = 16, Underline = UnderlineStyle.Solid });
            tb.MaxWidth = 100;

            using (var bitmap = new SKBitmap(200, 200))
            using (var canvas = new SKCanvas(bitmap))
            {
                // Warm up so layout, text blobs and fonts are already created
                for (int i = 0; i < 100; i++)
                {
                    tb.Paint(canvas);
                }

                // Act
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < paintCount; i++)
                {
                    tb.Paint(canvas);
                }
                long bytesPerPaint = (GC.GetAllocatedBytesForCurrentThread() - before) / paintCount;

                // Assert
                // A single SKPaint per run would cost far more than this
                Assert.True(bytesPerPaint < 16,
                    $"Painting allocated {bytesPerPaint} bytes per paint over {tb.FontRuns.Count} runs on {tb.Lines.Count} lines");
            }
        }
    }
}
