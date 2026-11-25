using Xunit;

namespace Topten.RichTextKit.Test;

public class TextRangeTests
{
    [Theory]
    [InlineData(2, 10, 0, 3, 2, 13)]
    [InlineData(10, 2, 0, 3, 13, 2)]
    [InlineData(2, 10, 2, 0, 2, 8)]
    [InlineData(10, 2, 2, 0, 8, 2)]
    [InlineData(2, 10, 2, 2, 2, 10)]
    [InlineData(10, 2, 2, 2, 10, 2)]
    public void EditInsideRangePreservesDirection(int start, int end, int oldLength, int newLength, int expectedStart, int expectedEnd)
    {
        var range = new TextRange(start, end, altPosition: true);

        var updated = range.UpdateForEdit(5, oldLength, newLength);

        Assert.Equal(expectedStart, updated.Start);
        Assert.Equal(expectedEnd, updated.End);
        Assert.True(updated.AltPosition);
    }
}
