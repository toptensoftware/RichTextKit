using System;
using System.Collections.Generic;
using SkiaSharp;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextBlockConfigurePaintTests
    {
        static TextBlock BuildBlock()
        {
            var tb = new TextBlock();
            tb.AddText("Plain ", new Style() { FontFamily = "Arial", FontSize = 30 });
            tb.AddText("Halo", new Style() { FontFamily = "Arial", FontSize = 30, HaloColor = SKColors.Yellow, HaloWidth = 2 });
            return tb;
        }

        static byte[] Render(TextBlock tb, TextPaintOptions options)
        {
            using (var bitmap = new SKBitmap(200, 80))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);
                tb.Paint(canvas, new SKPoint(10, 10), options);
                return bitmap.Bytes;
            }
        }

        static int CountCoveredColumns(byte[] bytes)
        {
            int count = 0;
            for (int x = 0; x < 200; x++)
            {
                for (int y = 0; y < 80; y++)
                {
                    if (bytes[(y * 200 + x) * 4 + 3] != 255 || bytes[(y * 200 + x) * 4] != 255)
                    {
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        static int CountNonWhite(byte[] bytes)
        {
            int count = 0;
            for (int i = 0; i < bytes.Length; i += 4)
                if (bytes[i] != 255 || bytes[i + 1] != 255 || bytes[i + 2] != 255)
                    count++;
            return count;
        }

        [Fact]
        void ConfigurePaintIsCalledOncePerRunAndOncePerHalo()
        {
            // Arrange
            var tb = BuildBlock();
            var calls = new List<(uint color, bool isHalo)>();
            var options = new TextPaintOptions()
            {
                ConfigurePaint = (paint, isHalo) => calls.Add(((uint)paint.Color, isHalo))
            };

            // Act
            Render(tb, options);

            // Assert
            int drawnRuns = 0;
            foreach (var r in tb.FontRuns)
                if (r.RunKind != FontRunKind.TrailingWhitespace)
                    drawnRuns++;
            Assert.Equal(drawnRuns, calls.FindAll(c => !c.isHalo).Count);
            Assert.Single(calls.FindAll(c => c.isHalo));
            Assert.All(calls.FindAll(c => !c.isHalo), c => Assert.Equal((uint)SKColors.Black, c.color));
            Assert.All(calls.FindAll(c => c.isHalo), c => Assert.Equal((uint)SKColors.Yellow, c.color));
        }

        [Fact]
        void ConfigurePaintCanChangeColorAndBlur()
        {
            // Arrange
            var tb = BuildBlock();
            var plain = Render(tb, null);
            var shadow = Render(tb, new TextPaintOptions()
            {
                ConfigurePaint = (paint, isHalo) =>
                {
                    paint.Color = SKColors.Red;
                    paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4);
                }
            });

            // Assert: red pixels exist, and the blur covers more area than plain text
            bool anyRed = false;
            for (int i = 0; i < shadow.Length; i += 4)
            {
                // Bgra: red text has high R, low G and B
                if (shadow[i + 2] > 200 && shadow[i + 1] < 100 && shadow[i] < 100) { anyRed = true; break; }
            }
            Assert.True(anyRed, "Expected red pixels");
            Assert.True(CountNonWhite(shadow) > CountNonWhite(plain), "Blurred pass should cover more pixels");
            Assert.True(CountCoveredColumns(shadow) > CountCoveredColumns(plain), "Blurred pass should be wider");
        }

        [Fact]
        void UnsetAndNoOpCallbackGiveIdenticalOutput()
        {
            // Arrange
            var tb = BuildBlock();

            // Act
            var unset = Render(tb, new TextPaintOptions());
            var noOp = Render(tb, new TextPaintOptions() { ConfigurePaint = (p, h) => { } });
            var defaults = Render(tb, null);

            // Assert
            Assert.Equal(defaults, unset);
            Assert.Equal(defaults, noOp);
        }

        [Fact]
        void UnsetCallbackAllocatesNothingExtra()
        {
            // Arrange
            const int paintCount = 2000;
            var tb = BuildBlock();
            var options = new TextPaintOptions();
            using (var bitmap = new SKBitmap(200, 80))
            using (var canvas = new SKCanvas(bitmap))
            {
                for (int i = 0; i < 100; i++)
                    tb.Paint(canvas, options);

                // Act
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < paintCount; i++)
                    tb.Paint(canvas, options);
                long bytesPerPaint = (GC.GetAllocatedBytesForCurrentThread() - before) / paintCount;

                // Assert
                Assert.True(bytesPerPaint < 16, $"Painting allocated {bytesPerPaint} bytes per paint");
            }
        }
    }
}
