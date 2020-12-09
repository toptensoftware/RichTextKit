using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class WordBoundaryTest
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

        [Fact]
        public void TestIsWordBoundary()
        {
            var str = new Utf32Buffer("Hello () World").AsSlice();

            // Get the boundaries (assuming FindWordBoundaries is correct)
            var boundaries = WordBoundaryAlgorithm.FindWordBoundaries(str).ToList();

            var boundaries2 = new List<int>();
            for (int i = 0; i < str.Length; i++)
            {
                if (WordBoundaryAlgorithm.IsWordBoundary(str.SubSlice(0, i), str.SubSlice(i, str.Length - i)))
                {
                    boundaries2.Add(i);
                }
            }

            Assert.Equal(boundaries, boundaries2);
        }

    }
}
