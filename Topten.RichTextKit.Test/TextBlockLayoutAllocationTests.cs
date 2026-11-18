using System;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextBlockLayoutAllocationTests
    {
        // Clear() returns the font runs and lines to their pools, so a layout after
        // Clear() and AddText() reuses them.  A layout triggered by a property change
        // (here MaxWidth) should reuse them too, and so allocate no more than that.
        [Fact]
        void RelayoutReusesPooledRunsAndLines()
        {
            // Arrange
            const int layoutCount = 2000;
            var tb = new TextBlock();
            var style = new Style() { FontSize = 16 };

            // Act
            long afterClear = MeasureBytesPerLayout(layoutCount, i =>
            {
                tb.Clear();
                tb.AddText(Text, style);
                tb.MaxWidth = 100;
            });
            long afterMaxWidthChange = MeasureBytesPerLayout(layoutCount, i =>
            {
                tb.MaxWidth = 100 + (i & 1);
            });

            // Assert
            // Each leaked run or line costs well over 64 bytes, and this text has
            // several of each, so a leak shows up as a difference of hundreds of bytes.
            Assert.True(afterMaxWidthChange - afterClear < 64,
                $"Relayout after a MaxWidth change allocated {afterMaxWidthChange} bytes per layout, vs {afterClear} after Clear()");

            long MeasureBytesPerLayout(int count, Action<int> invalidate)
            {
                return Allocations.BytesPerCall(count, i =>
                {
                    invalidate(i);
                    tb.Layout();
                });
            }
        }

        const string Text = "The quick brown fox jumps over the lazy dog";
    }
}
