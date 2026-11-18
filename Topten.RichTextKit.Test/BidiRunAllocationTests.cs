using System;
using System.Collections.Generic;
using System.Linq;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class BidiRunAllocationTests
    {
        static readonly sbyte[] MixedLevels = new sbyte[] { 0, 0, 1, 1, 1, 0, 2, 2, 1, 0 };

        // TextBlock builds its directional runs on every layout. Filling a reused
        // list should allocate nothing once the list has grown.
        [Fact]
        void CoalescLevelsIntoReusedListAllocatesNothing()
        {
            // Arrange
            var levels = new Slice<sbyte>(MixedLevels);
            var runs = new List<BidiRun>();

            // Act
            long bytesPerCall = Allocations.BytesPerCall(1000, i => BidiRun.CoalescLevels(levels, runs));

            // Assert
            Assert.Equal(0, bytesPerCall);
        }

        [Fact]
        void CoalescLevelsGroupsEqualLevels()
        {
            // Arrange
            var runs = new List<BidiRun>() { new BidiRun() { Start = 99 } };

            // Act
            BidiRun.CoalescLevels(new Slice<sbyte>(MixedLevels), runs);

            // Assert
            Assert.Equal(new[]
            {
                "0 - 2 - L", "2 - 5 - R", "5 - 6 - L", "6 - 8 - L", "8 - 9 - R", "9 - 10 - L",
            }, runs.Select(r => r.ToString()).ToArray());
        }

        [Fact]
        void CoalescLevelsOfEmptySliceClearsList()
        {
            // Arrange
            var runs = new List<BidiRun>() { new BidiRun() { Start = 99 } };

            // Act
            BidiRun.CoalescLevels(new Slice<sbyte>(new sbyte[0]), runs);

            // Assert
            Assert.Empty(runs);
        }

        // Mixed LTR/RTL text, including a direction override, produces the same
        // font runs as before the change.
        [Theory]
        [InlineData(TextDirection.Auto)]
        [InlineData(TextDirection.RTL)]
        void MixedDirectionTextKeepsItsRuns(TextDirection baseDirection)
        {
            // Arrange
            var tb = new TextBlock() { BaseDirection = baseDirection };
            tb.AddText("Hello שלום world ", new Style() { FontSize = 16 });
            tb.AddText("עולם 123 end", new Style() { FontSize = 16, TextDirection = TextDirection.LTR });

            // Act
            tb.Layout();
            var actual = string.Join(" | ", tb.FontRuns.Select(r => $"{r.Start},{r.Length},{r.Direction}"));

            // Assert
            Assert.Equal(ExpectedRuns[baseDirection], actual);
        }

        static readonly Dictionary<TextDirection, string> ExpectedRuns = new Dictionary<TextDirection, string>()
        {
            // Captured from main at 3fec4dc
            [TextDirection.Auto] = "0,6,LTR | 6,4,RTL | 10,7,LTR | 17,5,RTL | 22,3,LTR | 25,4,LTR",
            [TextDirection.RTL] = "0,5,LTR | 5,6,RTL | 11,5,LTR | 16,1,RTL | 17,5,RTL | 22,3,LTR | 25,4,LTR",
        };
    }
}
