using System.Reflection;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextBlockUnshapedRunAllocationTests
    {
        // A struct keeps layout from allocating one object per font run; other
        // allocations hide those bytes from a byte-count test.
        [Fact]
        void UnshapedRunIsValueType()
        {
            var type = typeof(TextBlock).GetNestedType("UnshapedRun", BindingFlags.NonPublic);
            Assert.True(type.IsValueType);
        }

        // Pins how style runs become font runs: runs that differ only in color are
        // shaped together then split, runs with different fonts are shaped apart.
        // Expected values were captured on main before UnshapedRun became a struct.
        [Theory]
        [InlineData(false, null, "lines=1 0+6:Arial 6+6:Arial 12+9:Arial")]
        [InlineData(true, null, "lines=1 0+6:Arial 6+6:Times New Roman 12+9:Arial")]
        [InlineData(true, 60f, "lines=4 0+5:Arial 5+1:Arial 6+5:Times New Roman 11+1:Times New Roman 12+3:Arial 15+1:Arial 16+5:Arial")]
        void LayoutSplitsFontRunsAtStyleChanges(bool alternateFonts, float? maxWidth, string expected)
        {
            // Arrange
            var arial = new Style() { FontFamily = "Arial", FontSize = 16 };
            var other = alternateFonts
                ? new Style() { FontFamily = "Times New Roman", FontSize = 16 }
                : new Style() { FontFamily = "Arial", FontSize = 16, TextColor = SkiaSharp.SKColors.Red };
            var tb = new TextBlock() { MaxWidth = maxWidth };
            tb.AddText("Hello ", arial);
            tb.AddText("brave ", other);
            tb.AddText("new world", arial);

            // Act
            tb.Layout();

            // Assert
            var actual = $"lines={tb.Lines.Count}";
            foreach (var run in tb.FontRuns)
                actual += $" {run.Start}+{run.Length}:{run.Typeface.FamilyName}";
            Assert.Equal(expected, actual);
        }
    }
}
