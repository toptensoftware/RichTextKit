using System;
using SkiaSharp;
using Xunit;

namespace Topten.RichTextKit.Test
{
    // These cover the parts of RichTextKit that talk to SkiaSharp's font and glyph
    // measuring API (SKFont), so a SkiaSharp upgrade that breaks them is caught.
    // Expected values are derived from the text block itself or from the pixels
    // it draws, so they hold for whatever font the platform resolves.
    public class TextBlockRenderingTests
    {
        const float Origin = 40;

        [Fact]
        void Paint_DrawsGlyphs()
        {
            // Arrange
            var tb = new TextBlock();
            tb.AddText("Hello world", new Style() { FontSize = 24 });

            // Act
            using var bitmap = Render(tb, 300, 100);

            // Assert
            var ink = InkBounds(bitmap);
            Assert.False(ink.IsEmpty, "Painting a text block drew nothing");
            Assert.True(ink.Right - Origin <= tb.MeasuredWidth + tb.MeasuredOverhang.Right + 2,
                $"Ink extends to {ink.Right - Origin} but the block measured {tb.MeasuredWidth} wide");
        }

        [Fact]
        void MeasuredHeight_ScalesWithFontSize()
        {
            // Arrange
            var small = new TextBlock();
            small.AddText("Hello", new Style() { FontSize = 20 });
            var large = new TextBlock();
            large.AddText("Hello", new Style() { FontSize = 40 });

            // Act
            var ratio = large.MeasuredHeight / small.MeasuredHeight;

            // Assert
            Assert.InRange(ratio, 1.9f, 2.1f);
        }

        [Theory]
        [InlineData(FontVariant.Normal)]
        [InlineData(FontVariant.SuperScript)]
        [InlineData(FontVariant.SubScript)]
        void MeasuredOverhang_MatchesDrawnGlyphs(FontVariant variant)
        {
            // Arrange
            // Synthetic italic slants the tall glyphs so they stick out past the advance
            var tb = new TextBlock();
            tb.AddText("ffjff", new Style() { FontSize = 96, FontItalic = true, FontVariant = variant });

            // Act
            using var bitmap = Render(tb, 600, 300);
            var overhang = tb.MeasuredOverhang;
            var ink = InkBounds(bitmap);

            // Assert
            Assert.False(ink.IsEmpty);
            Assert.True(overhang.Right > 0, "Italic glyphs were expected to overhang on the right");

            // The overhang is measured from glyph bounds, so it should land on the drawn edge
            float inkRight = ink.Right - Origin;
            Assert.InRange(inkRight, tb.MeasuredWidth + overhang.Right - 3, tb.MeasuredWidth + overhang.Right + 3);

            float inkLeft = ink.Left - Origin;
            Assert.True(inkLeft >= -overhang.Left - 2,
                $"Ink starts at {inkLeft} but the left overhang is only {overhang.Left}");
        }

        [Fact]
        void ReplacementCharacter_MeasuresLikeTheCharacterItReplaces()
        {
            // Arrange
            var literal = new TextBlock();
            literal.AddText("***", new Style() { FontSize = 24 });

            var replaced = new TextBlock();
            replaced.AddText("abc", new Style() { FontSize = 24, ReplacementCharacter = '*' });

            // Act
            using var bitmap = Render(replaced, 300, 100);

            // Assert
            Assert.InRange(replaced.MeasuredWidth, literal.MeasuredWidth - 0.5f, literal.MeasuredWidth + 0.5f);
            Assert.False(InkBounds(bitmap).IsEmpty, "Replacement glyphs were not drawn");
        }

        static SKBitmap Render(TextBlock tb, int width, int height)
        {
            var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                tb.Paint(canvas, new SKPoint(Origin, Origin), new TextPaintOptions());
            }
            return bitmap;
        }

        // The bounding box of every pixel the text drew onto the transparent bitmap
        static SKRectI InkBounds(SKBitmap bitmap)
        {
            int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).Alpha <= 32)
                        continue;
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
            }

            return right < 0 ? SKRectI.Empty : new SKRectI(left, top, right, bottom);
        }
    }
}
