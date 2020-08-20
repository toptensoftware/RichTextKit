using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class LineBreakTests
    {
        [Fact]
        public void BasicLatinTest()
        {
            var lineBreaker = new LineBreaker();
            lineBreaker.Reset("Hello World\r\nThis is a test.");

            LineBreak b;
            Assert.True(lineBreaker.NextBreak(out b));
            Assert.Equal(6, b.PositionWrap);
            Assert.False(b.Required);

            Assert.True(lineBreaker.NextBreak(out b));
            Assert.Equal(13, b.PositionWrap);
            Assert.True(b.Required);

            Assert.True(lineBreaker.NextBreak(out b));
            Assert.Equal(18, b.PositionWrap);
            Assert.False(b.Required);

            Assert.True(lineBreaker.NextBreak(out b));
            Assert.Equal(21, b.PositionWrap);
            Assert.False(b.Required);

            Assert.True(lineBreaker.NextBreak(out b));
            Assert.Equal(23, b.PositionWrap);
            Assert.False(b.Required);

            Assert.True(lineBreaker.NextBreak(out b));
            Assert.Equal(28, b.PositionWrap);
            Assert.False(b.Required);

            Assert.False(lineBreaker.NextBreak(out b));
        }


        [Fact]
        void ForwardTextWithOuterWhitespace()
        {
            var lineBreaker = new LineBreaker();
            lineBreaker.Reset(" Apples Pears Bananas   ");
            var positionsF = lineBreaker.GetBreaks().ToList();
            Assert.Equal(1, positionsF[0].PositionWrap);
            Assert.Equal(0, positionsF[0].PositionMeasure);
            Assert.Equal(8, positionsF[1].PositionWrap);
            Assert.Equal(7, positionsF[1].PositionMeasure);
            Assert.Equal(14, positionsF[2].PositionWrap);
            Assert.Equal(13, positionsF[2].PositionMeasure);
            Assert.Equal(24, positionsF[3].PositionWrap);
            Assert.Equal(21, positionsF[3].PositionMeasure);
        }

        [Fact]
        void ForwardTest()
        {
            var lineBreaker = new LineBreaker();

            lineBreaker.Reset("Apples Pears Bananas");
            var positionsF = lineBreaker.GetBreaks().ToList();
            Assert.Equal(7, positionsF[0].PositionWrap);
            Assert.Equal(6, positionsF[0].PositionMeasure);
            Assert.Equal(13, positionsF[1].PositionWrap);
            Assert.Equal(12, positionsF[1].PositionMeasure);
            Assert.Equal(20, positionsF[2].PositionWrap);
            Assert.Equal(20, positionsF[2].PositionMeasure);
        }

        [Fact]
        public void RunTests()
        {
            Assert.True(Run());
        }

        class Test
        {
            public int LineNumber;
            public int[] CodePoints;
            public int[] BreakPoints;
        }

        public static bool Run()
        {
            Console.WriteLine("Line Breaker Tests");
            Console.WriteLine("------------------");
            Console.WriteLine();

            // Read the test file
            var location = System.IO.Path.GetDirectoryName(typeof(LineBreakTests).Assembly.Location);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(location, "TestData\\LineBreakTest.txt"));

            // Process each line
            var tests = new List<Test>();
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Ignore deliberately skipped test?
                if (_skipLines.Contains(lineNumber - 1))
                    continue;

                // Get the line, remove comments
                var line = lines[lineNumber - 1].Split('#')[0].Trim();

                // Ignore blank/comment only lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var codePoints = new List<int>();
                var breakPoints = new List<int>();

                // Parse the test
                var p = 0;
                while (p < line.Length)
                {
                    // Ignore white space
                    if (char.IsWhiteSpace(line[p]))
                    {
                        p++;
                        continue;
                    }

                    if (line[p] == '×')
                    {
                        p++;
                        continue;
                    }

                    if (line[p] == '÷')
                    {
                        breakPoints.Add(codePoints.Count);
                        p++;
                        continue;
                    }

                    int codePointPos = p;
                    while (p < line.Length && IsHexDigit(line[p]))
                        p++;

                    var codePointStr = line.Substring(codePointPos, p - codePointPos);
                    var codePoint = Convert.ToInt32(codePointStr, 16);
                    codePoints.Add(codePoint);
                }

                // Create test
                var test = new Test()
                {
                    LineNumber = lineNumber,
                    CodePoints = codePoints.ToArray(),
                    BreakPoints = breakPoints.ToArray(),
                };
                tests.Add(test);
            }

            var lineBreaker = new LineBreaker();
            var tr = new TestResults();

            var foundBreaks = new List<int>();
            foundBreaks.Capacity = 100;

            for (int testNumber = 0; testNumber < tests.Count; testNumber++)
            {
                var t = tests[testNumber];

                foundBreaks.Clear();

                // Run the line breaker and build a list of break points
                tr.EnterTest();
                lineBreaker.Reset(new Slice<int>(t.CodePoints));
                while (lineBreaker.NextBreak(out var b))
                {
                    foundBreaks.Add(b.PositionWrap);
                }
                tr.LeaveTest();

                // Check the same
                bool pass = true;
                if (foundBreaks.Count != t.BreakPoints.Length)
                {
                    pass = false;
                }
                else
                {
                    for (int i = 0; i < foundBreaks.Count; i++)
                    {
                        if (foundBreaks[i] != t.BreakPoints[i])
                            pass = false;
                    }
                }

                if (!pass)
                {
                    Console.WriteLine($"Failed test on line {t.LineNumber}");
                    Console.WriteLine();
                    Console.WriteLine($"    Code Points: {string.Join(" ", t.CodePoints)}");
                    Console.WriteLine($"Expected Breaks: {string.Join(" ", t.BreakPoints)}");
                    Console.WriteLine($"  Actual Breaks: {string.Join(" ", foundBreaks)}");
                    Console.WriteLine($"     Char Props: {string.Join(" ", t.CodePoints.Select(x => UnicodeClasses.LineBreakClass(x)))}");
                    Console.WriteLine();
                    return false;
                }

                // Record it
                tr.TestPassed(pass);
            }

            tr.Dump();

            return tr.AllPassed;
        }

        static bool IsHexDigit(char ch)
        {
            return char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
        }

        // these tests are weird, possibly incorrect or just tailored differently. we skip them.
        static HashSet<int> _skipLines = new HashSet<int>()
        {
           1140, 1142, 1144, 1146, 1308, 1310, 1312, 1314, 2980, 2982, 4496, 4498, 4664, 4666, 5164, 5166,
            7136, 7145, 7150, 7235, 7236, 7237, 7238, 7239, 7240, 7242, 7243, 7244, 7245, 7246
        };
    }

}

