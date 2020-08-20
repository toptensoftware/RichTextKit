using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class GraphemeClusterTests
    {
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
            Console.WriteLine("Grapheme Cluster Tests");
            Console.WriteLine("----------------------");
            Console.WriteLine();

            // Read the test file
            var location = System.IO.Path.GetDirectoryName(typeof(LineBreakTests).Assembly.Location);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(location, "TestData\\GraphemeBreakTest.txt"));

            // Process each line
            var tests = new List<Test>();
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
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

            // Preload
            GraphemeClusterAlgorithm.IsBoundary(new Slice<int>(new int[10]), 0);

            var lineBreaker = new LineBreaker();
            var tr = new TestResults();

            var foundBreaks = new List<int>();
            foundBreaks.Capacity = 100;

            for (int testNumber = 0; testNumber < tests.Count; testNumber++)
            {
                var t = tests[testNumber];

                foundBreaks.Clear();

                var codePointsSlice = new Slice<int>(t.CodePoints.ToArray());

                tr.EnterTest();

                // Run the algorithm
                for (int i = 0; i < codePointsSlice.Length + 1; i++)
                {
                    if (GraphemeClusterAlgorithm.IsBoundary(codePointsSlice, i))
                        foundBreaks.Add(i);
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
                    Console.WriteLine($"     Char Props: {string.Join(" ", t.CodePoints.Select(x => UnicodeClasses.GraphemeClusterClass(x)))}");
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

    }
}
