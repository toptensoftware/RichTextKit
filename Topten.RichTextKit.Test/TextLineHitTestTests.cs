using System;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextLineHitTestTests
    {
        // Clicking past the end of a line that ends in a line break should put the
        // caret before the break, not after it (which would show it on the next line).
        // The breaks are '\n' and U+2029 (paragraph separator), given as char codes.
        [Theory]
        [InlineData(0x000A)]
        [InlineData(0x2029)]
        void HitTestPastEndOfLinePlacesCaretBeforeLineBreak(int lineBreak)
        {
            // Arrange
            var tb = new TextBlock();
            tb.AddText("Hello" + (char)lineBreak + "World", new Style() { FontSize = 16 });

            // Act
            var htr = tb.HitTest(10000, 1);

            // Assert
            Assert.Equal(0, htr.OverLine);
            Assert.Equal(5, htr.ClosestCodePointIndex);
        }

        // HitTestLine is public, so like HitTest it must lay out the block first
        [Fact]
        void HitTestLineLaysOutBlock()
        {
            // Arrange
            var tb = new TextBlock();
            tb.AddText("Hello", new Style() { FontSize = 16 });

            // Act
            var htr = tb.HitTestLine(0, 10000);

            // Assert
            Assert.Equal(5, htr.ClosestCodePointIndex);
        }

        // Clicking past the end of a line that wraps without a line break puts the
        // caret at the end of the line's trailing whitespace
        [Fact]
        void HitTestPastEndOfWrappedLinePlacesCaretAtLineEnd()
        {
            // Arrange
            var tb = new TextBlock() { MaxWidth = 60 };
            tb.AddText("Hello World", new Style() { FontSize = 16 });
            Assert.True(tb.Lines.Count > 1, "Expected the text to wrap");

            // Act
            var htr = tb.HitTestLine(0, 10000);

            // Assert
            Assert.Equal(tb.Lines[0].End, htr.ClosestCodePointIndex);
        }
    }
}
