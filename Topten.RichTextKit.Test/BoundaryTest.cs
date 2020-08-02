using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class BoundaryTest
    {
        [Fact]
        public void SingleWordTest()
        {
            var str = "Hello";
            var boundaries = WordBoundaryAlgorithm.FindWordBoundaries(new Utf32Buffer(str).AsSlice()).ToList();

            Assert.Single(boundaries);
            Assert.Equal(0, boundaries[0]);
        }

        [Fact]
        public void TwoWordTest()
        {
            var str = "Hello World";
            var boundaries = WordBoundaryAlgorithm.FindWordBoundaries(new Utf32Buffer(str).AsSlice()).ToList();

            Assert.Equal(2, boundaries.Count);
            Assert.Equal(0, boundaries[0]);
            Assert.Equal(6, boundaries[1]);
        }

        [Fact]
        public void LeadingSpaceTest()
        {
            var str = "  Hello World";
            var boundaries = WordBoundaryAlgorithm.FindWordBoundaries(new Utf32Buffer(str).AsSlice()).ToList();

            Assert.Equal(3, boundaries.Count);
            Assert.Equal(0, boundaries[0]);
            Assert.Equal(2, boundaries[1]);
            Assert.Equal(8, boundaries[2]);
        }

        [Fact]
        public void TrailingSpaceTest()
        {
            var str = "Hello World   ";
            var boundaries = WordBoundaryAlgorithm.FindWordBoundaries(new Utf32Buffer(str).AsSlice()).ToList();

            Assert.Equal(3, boundaries.Count);
            Assert.Equal(0, boundaries[0]);
            Assert.Equal(6, boundaries[1]);
            Assert.Equal(14, boundaries[2]);
        }

        [Fact]
        public void CombinedLettersAndDigits()
        {
            var str = "Hello99 World";
            var boundaries = WordBoundaryAlgorithm.FindWordBoundaries(new Utf32Buffer(str).AsSlice()).ToList();

            Assert.Equal(2, boundaries.Count);
            Assert.Equal(0, boundaries[0]);
            Assert.Equal(8, boundaries[1]);
        }

        [Fact]
        public void Punctuation()
        {
            var str = "Hello, World";
            var boundaries = WordBoundaryAlgorithm.FindWordBoundaries(new Utf32Buffer(str).AsSlice()).ToList();

            Assert.Equal(3, boundaries.Count);
            Assert.Equal(0, boundaries[0]);
            Assert.Equal(5, boundaries[1]);
            Assert.Equal(7, boundaries[2]);
        }

        [Fact]
        public void Punctuation2()
        {
            var str = "Hello () World";
            var boundaries = WordBoundaryAlgorithm.FindWordBoundaries(new Utf32Buffer(str).AsSlice()).ToList();

            Assert.Equal(3, boundaries.Count);
            Assert.Equal(0, boundaries[0]);
            Assert.Equal(6, boundaries[1]);
            Assert.Equal(9, boundaries[2]);
        }

    }
}
