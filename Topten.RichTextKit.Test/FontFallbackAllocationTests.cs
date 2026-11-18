using System;
using System.Collections.Generic;
using SkiaSharp;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class FontFallbackAllocationTests
    {
        // TextBlock splits each run into font fallback runs on every layout.  Once the
        // run list and glyph buffer have grown, that split should allocate nothing.
        [Fact]
        void GetFontRunsIntoListDoesNotAllocate()
        {
            // Arrange
            var codePoints = new Utf32Buffer(Text).AsSlice();
            var typeface = SKTypeface.FromFamilyName("Arial");
            var runs = new List<FontFallback.Run>();

            // Act
            long bytesPerCall = Allocations.BytesPerCall(1000, i =>
            {
                runs.Clear();
                FontFallback.GetFontRuns(codePoints, typeface, '\0', runs);
            });

            // Assert
            Assert.Single(runs);
            Assert.True(bytesPerCall == 0, $"GetFontRuns allocated {bytesPerCall} bytes per call");
        }

        // Pins the runs both overloads return, as captured on main before this change.
        // Each run is "start,length,family", joined by ";".
        [Theory]
        [InlineData("abc", '\0', "0,3,Arial")]
        [InlineData("", '\0', "")]
        [InlineData("Hello \u4E16\u754C world \U0001F600\U0001F600 x", '\0', "0,6,Arial;6,2,Yu Gothic UI;8,7,Arial;15,2,Segoe UI Emoji;17,2,Arial")]
        [InlineData("\U0001F600x", '\0', "0,1,Segoe UI Emoji;1,1,Arial")]
        [InlineData("abc", '\u2022', "0,3,Arial")]
        [InlineData("abc", '\u2E3B', "0,3,Segoe UI")]
        void GetFontRunsSplitsRuns(string text, char replacementCharacter, string expected)
        {
            // Arrange
            var codePoints = new Utf32Buffer(text).AsSlice();
            var typeface = SKTypeface.FromFamilyName("Arial");
            var list = new List<FontFallback.Run>();

            // Act
            var enumerated = new List<FontFallback.Run>(FontFallback.GetFontRuns(codePoints, typeface, replacementCharacter));
            FontFallback.GetFontRuns(codePoints, typeface, replacementCharacter, list);

            // Assert
            Assert.Equal(expected, Describe(enumerated));
            Assert.Equal(expected, Describe(list));

            static string Describe(List<FontFallback.Run> runs)
            {
                return string.Join(";", runs.ConvertAll(r => $"{r.Start},{r.Length},{r.Typeface.FamilyName}"));
            }
        }

        const string Text ="The quick brown fox jumps over the lazy dog";
    }
}
