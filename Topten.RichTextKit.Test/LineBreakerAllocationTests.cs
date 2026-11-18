using System;
using System.Collections.Generic;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class LineBreakerAllocationTests
    {
        // TextBlock finds line breaks on every layout.  With a reused list,
        // finding them must not allocate, with or without a max width.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        void GetBreaksDoesNotAllocate(bool mandatoryOnly)
        {
            // Arrange
            var lineBreaker = new LineBreaker();
            var list = new List<LineBreak>();
            var codePoints = new Slice<int>(Utf32Utils.ToUtf32("The quick brown fox\njumps over\r\nthe lazy dog."));
            // Act
            long bytes = Allocations.BytesPerCall(1000, i =>
            {
                lineBreaker.Reset(codePoints);
                lineBreaker.GetBreaks(list, mandatoryOnly);
            });

            // Assert
            Assert.NotEmpty(list);
            Assert.Equal(0, bytes);
        }
    }
}
