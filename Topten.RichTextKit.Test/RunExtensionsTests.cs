using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class RunExtensionsTests
    {
        [Fact]
        void CheckCreateRanges()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            Assert.Equal(3, ranges.Count);

            Assert.Equal(0, ranges[0].Offset);
            Assert.Equal(10, ranges[0].Length);

            Assert.Equal(10, ranges[1].Offset);
            Assert.Equal(20, ranges[1].Length);

            Assert.Equal(30, ranges[2].Offset);
            Assert.Equal(30, ranges[2].Length);

            ranges.CheckValid(60);
        }

        [Fact]
        void All()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(0, 60).ToList();

            Assert.Equal(3, subRanges.Count);
            Assert.Equal(new SubRun(0, 0, 10, false), subRanges[0]);
            Assert.Equal(new SubRun(1, 0, 20, false), subRanges[1]);
            Assert.Equal(new SubRun(2, 0, 30, false), subRanges[2]);
        }

        [Fact]
        void NonPartialFirst()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(0, 10).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(0, 0, 10, false), subRanges[0]);
        }

        [Fact]
        void NonPartialMiddle()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(10, 20).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(1, 0, 20, false), subRanges[0]);
        }

        [Fact]
        void NonPartialLast()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(30, 30).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(2, 0, 30, false), subRanges[0]);
        }

        [Fact]
        void PartialFirst()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(3, 3).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(0, 3, 3, true), subRanges[0]);
        }

        [Fact]
        void PartialMiddle()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(13, 3).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(1, 3, 3, true), subRanges[0]);
        }

        [Fact]
        void PartialLast()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(33, 3).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(2, 3, 3, true), subRanges[0]);
        }


        [Fact]
        void OverlapFirstSecond()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(8, 4).ToList();

            Assert.Equal(2, subRanges.Count);
            Assert.Equal(new SubRun(0, 8, 2, true), subRanges[0]);
            Assert.Equal(new SubRun(1, 0, 2, true), subRanges[1]);
        }


        [Fact]
        void OverlapSecondThird()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(28, 4).ToList();

            Assert.Equal(2, subRanges.Count);
            Assert.Equal(new SubRun(1, 18, 2, true), subRanges[0]);
            Assert.Equal(new SubRun(2, 0, 2, true), subRanges[1]);
        }


        [Fact]
        void OverlapFirstThird()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(8, 24).ToList();

            Assert.Equal(3, subRanges.Count);
            Assert.Equal(new SubRun(0, 8, 2, true), subRanges[0]);
            Assert.Equal(new SubRun(1, 0, 20, false), subRanges[1]);
            Assert.Equal(new SubRun(2, 0, 2, true), subRanges[2]);
        }


        [Fact]
        void WholeFirstPartialSecond()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(0, 15).ToList();

            Assert.Equal(2, subRanges.Count);
            Assert.Equal(new SubRun(0, 0, 10, false), subRanges[0]);
            Assert.Equal(new SubRun(1, 0, 5, true), subRanges[1]);
        }


        [Fact]
        void PartialFirstWholeSecond()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetInterectingRuns(5, 25).ToList();

            Assert.Equal(2, subRanges.Count);
            Assert.Equal(new SubRun(0, 5, 5, true), subRanges[0]);
            Assert.Equal(new SubRun(1, 0, 20, false), subRanges[1]);
        }




        [Fact]
        void AllReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(0, 60).ToList();

            Assert.Equal(3, subRanges.Count);
            Assert.Equal(new SubRun(0, 0, 10, false), subRanges[2]);
            Assert.Equal(new SubRun(1, 0, 20, false), subRanges[1]);
            Assert.Equal(new SubRun(2, 0, 30, false), subRanges[0]);
        }

        [Fact]
        void NonPartialFirstReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(0, 10).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(0, 0, 10, false), subRanges[0]);
        }

        [Fact]
        void NonPartialMiddleReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(10, 20).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(1, 0, 20, false), subRanges[0]);
        }

        [Fact]
        void NonPartialLastReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(30, 30).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(2, 0, 30, false), subRanges[0]);
        }

        [Fact]
        void PartialFirstReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(3, 3).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(0, 3, 3, true), subRanges[0]);
        }

        [Fact]
        void PartialMiddleReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(13, 3).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(1, 3, 3, true), subRanges[0]);
        }

        [Fact]
        void PartialLastReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(33, 3).ToList();

            Assert.Single(subRanges);
            Assert.Equal(new SubRun(2, 3, 3, true), subRanges[0]);
        }


        [Fact]
        void OverlapFirstSecondReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(8, 4).ToList();

            Assert.Equal(2, subRanges.Count);
            Assert.Equal(new SubRun(0, 8, 2, true), subRanges[1]);
            Assert.Equal(new SubRun(1, 0, 2, true), subRanges[0]);
        }


        [Fact]
        void OverlapSecondThirdReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(28, 4).ToList();

            Assert.Equal(2, subRanges.Count);
            Assert.Equal(new SubRun(1, 18, 2, true), subRanges[1]);
            Assert.Equal(new SubRun(2, 0, 2, true), subRanges[0]);
        }


        [Fact]
        void OverlapFirstThirdReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(8, 24).ToList();

            Assert.Equal(3, subRanges.Count);
            Assert.Equal(new SubRun(0, 8, 2, true), subRanges[2]);
            Assert.Equal(new SubRun(1, 0, 20, false), subRanges[1]);
            Assert.Equal(new SubRun(2, 0, 2, true), subRanges[0]);
        }


        [Fact]
        void WholeFirstPartialSecondReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(0, 15).ToList();

            Assert.Equal(2, subRanges.Count);
            Assert.Equal(new SubRun(0, 0, 10, false), subRanges[1]);
            Assert.Equal(new SubRun(1, 0, 5, true), subRanges[0]);
        }


        [Fact]
        void PartialFirstWholeSecondReverse()
        {
            var ranges = CreateRanges(10, 20, 30).ToList();

            var subRanges = ranges.GetIntersectingRunsReverse(5, 25).ToList();

            Assert.Equal(2, subRanges.Count);
            Assert.Equal(new SubRun(0, 5, 5, true), subRanges[1]);
            Assert.Equal(new SubRun(1, 0, 20, false), subRanges[0]);
        }



        IEnumerable<TestRange> CreateRanges(params int[] lengths)
        {
            int offset = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                yield return new TestRange(offset, lengths[i]);
                offset += lengths[i];
            }
        }


        class TestRange : IRun
        {
            public TestRange(int offset, int length)
            {
                Offset = offset;
                Length = length;
            }
            public int Offset { get; set; }
            public int Length { get; set; }
        }

    }
}
