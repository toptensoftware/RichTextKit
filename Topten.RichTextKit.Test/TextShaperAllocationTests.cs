using System;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextShaperAllocationTests
    {
        // Copying HarfBuzz's glyph infos and positions out to new arrays costs 40
        // bytes per glyph (two 20-byte structs), on every layout.  Other layout work
        // also grows a little with text length, so the test allows half that.
        [Fact]
        void ShapingDoesNotCopyGlyphArrays()
        {
            // Arrange
            const string word = "quick ";
            const int shortCount = 10;
            const int longCount = 100;

            // Act
            long shortText = MeasureBytesPerLayout(Repeat(word, shortCount));
            long longText = MeasureBytesPerLayout(Repeat(word, longCount));

            // Assert
            long extraGlyphs = (longCount - shortCount) * word.Length;
            long bytesPerExtraGlyph = (longText - shortText) / extraGlyphs;
            Assert.True(bytesPerExtraGlyph < 20,
                $"Layout allocated {bytesPerExtraGlyph} bytes per glyph ({longText} bytes for {longCount * word.Length} characters, {shortText} for {shortCount * word.Length})");
        }

        static long MeasureBytesPerLayout(string text)
        {
            const int layoutCount = 500;
            var tb = new TextBlock();
            tb.AddText(text, new Style() { FontSize = 16 });

            // Warm up so pools and buffers are already grown.  The wide
            // MaxWidth keeps the text on a single line.
            for (int i = 0; i < 100; i++)
            {
                tb.MaxWidth = 100000 + (i & 1);
                tb.Layout();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < layoutCount; i++)
            {
                tb.MaxWidth = 100000 + (i & 1);
                tb.Layout();
            }
            return (GC.GetAllocatedBytesForCurrentThread() - before) / layoutCount;
        }

        static string Repeat(string str, int count)
        {
            return string.Concat(System.Linq.Enumerable.Repeat(str, count));
        }
    }
}
