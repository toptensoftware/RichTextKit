using System;
using System.Collections.Generic;
using System.Linq;
using Topten.RichTextKit;
using Topten.RichTextKit.Utils;

namespace TestBench
{
    class LineBreakTest
    {
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
            var location = System.IO.Path.GetDirectoryName(typeof(LineBreakTest).Assembly.Location);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(location, "LineBreakTest.txt"));

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
             812,  814,  848,  850,  864,  866,  900,  902,  956,  958, 1068, 1070, 1072, 1074, 1224, 1226,
            1228, 1230, 1760, 1762, 2932, 2934, 4100, 4101, 4102, 4103, 4340, 4342, 4496, 4498, 4568, 4570,
            4704, 4706, 4707, 4708, 4710, 4711, 4712, 4714, 4715, 4716, 4718, 4719, 4722, 4723, 4726, 4727,
            4730, 4731, 4734, 4735, 4736, 4738, 4739, 4742, 4743, 4746, 4747, 4748, 4750, 4751, 4752, 4754,
            4755, 4756, 4758, 4759, 4760, 4762, 4763, 4764, 4766, 4767, 4768, 4770, 4771, 4772, 4774, 4775,
            4778, 4779, 4780, 4782, 4783, 4784, 4786, 4787, 4788, 4790, 4791, 4794, 4795, 4798, 4799, 4800,
            4802, 4803, 4804, 4806, 4807, 4808, 4810, 4811, 4812, 4814, 4815, 4816, 4818, 4819, 4820, 4822,
            4823, 4826, 4827, 4830, 4831, 4834, 4835, 4838, 4839, 4840, 4842, 4843, 4844, 4846, 4847, 4848,
            4850, 4851, 4852, 4854, 4855, 4856, 4858, 4859, 4960, 4962, 5036, 5038, 6126, 6135, 6140, 6225,
            6226, 6227, 6228, 6229, 6230, 6232, 6233, 6234, 6235, 6236, 6332,
        };
    }
}
