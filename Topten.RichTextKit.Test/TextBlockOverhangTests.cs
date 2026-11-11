using System;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextBlockOverhangTests
    {
        // "e" + combining acute shapes to one glyph for two code points, so in the run
        // after it the code point index and the glyph index differ.  Precomposed "é"
        // gives the same glyphs with matching indices, so both blocks must measure
        // the same overhang.
        [Fact]
        void OverhangReadsRunGlyphsWhenCodePointAndGlyphIndicesDiffer()
        {
            // Arrange
            var decomposed = BuildBlock(Repeat("é", 20));
            var precomposed = BuildBlock(Repeat("é", 20));

            // The first run must really have fewer glyphs than code points, or the
            // two blocks wouldn't test anything
            var firstRun = decomposed.FontRuns[0];
            Assert.True(firstRun.Glyphs.Length < firstRun.Length,
                $"Expected the decomposed run to compose ({firstRun.Glyphs.Length} glyphs for {firstRun.Length} code points)");

            // Act
            var expected = precomposed.MeasuredOverhang;
            var actual = decomposed.MeasuredOverhang;

            // Assert
            Assert.True(expected.Right > 0, "The italic 'f' should overhang on the right");
            Assert.Equal(expected, actual);
        }

        static TextBlock BuildBlock(string prefix)
        {
            var tb = new TextBlock();
            tb.AddText(prefix, new Style() { FontFamily = "Arial", FontSize = 40 });
            tb.AddText("f", new Style() { FontFamily = "Arial", FontSize = 40, FontItalic = true });
            return tb;
        }

        static string Repeat(string str, int count)
        {
            return string.Concat(System.Linq.Enumerable.Repeat(str, count));
        }
    }
}
