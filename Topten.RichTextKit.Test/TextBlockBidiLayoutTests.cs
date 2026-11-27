using System.Linq;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextBlockBidiLayoutTests
    {
        // UAX #9 rule L2: runs are reversed by level, from the highest down to the lowest odd one.
        // "123" is level 2 inside the level 1 Hebrew, so it ends up left of it, as in a browser.
        [Fact]
        void NumberAfterRightToLeftTextInLeftToRightParagraph()
        {
            AssertWordsLeftToRight(TextDirection.LTR, "abc שלום 123 def", "abc", "123", "שלום", "def");
        }

        // A left-to-right isolate (LRI ... PDI) in a right-to-left paragraph, holding Hebrew and a
        // number: levels 1 to 4, so every pass of rule L2 is needed.
        [Fact]
        void NestedLevelsInRightToLeftParagraph()
        {
            AssertWordsLeftToRight(TextDirection.RTL, "שלום \u2066abc דוד 12\u2069", "abc", "12", "דוד", "שלום");
        }

        static void AssertWordsLeftToRight(TextDirection baseDirection, string text, params string[] words)
        {
            // Arrange
            var tb = new TextBlock() { BaseDirection = baseDirection };
            tb.AddText(text, new Style() { FontFamily = "Arial", FontSize = 20 });

            // Act
            tb.Layout();

            // Assert
            var actual = words.OrderBy(w => RunAt(tb, text.IndexOf(w)).XCoord).ToArray();
            Assert.Equal(words, actual);
        }

        static FontRun RunAt(TextBlock tb, int codePointIndex)
        {
            return tb.Lines.SelectMany(l => l.Runs).Single(r => r.Start <= codePointIndex && codePointIndex < r.End);
        }
    }
}
