using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Topten.RichTextKit;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class BidiTest
    {
        class Test
        {
            public Slice<Directionality> Types;
            public sbyte ParagraphEmbeddingLevel;
            public int[] ExpectedLevels;
            public int LineNumber;
        }

        [Fact]
        public void RunTests()
        {
            Assert.True(Run());
        }


        public static bool Run()
        {
            Console.WriteLine("Bidi Class Tests");
            Console.WriteLine("----------------");
            Console.WriteLine();

             // Read the test file
            var location = System.IO.Path.GetDirectoryName(typeof(BidiTest).Assembly.Location);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(location, "TestData\\BidiTest.txt"));

            var bidi = new Bidi();

            List<Test> tests = new List<Test>();

            // Process each line
            int[] levels = null;
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Get the line, remove comments
                var line = lines[lineNumber - 1].Split('#')[0].Trim();

                // Ignore blank/comment only lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Directive?
                if (line.StartsWith("@"))
                {
                    if (line.StartsWith("@Levels:"))
                    {
                        levels = line.Substring(8).Trim().Split(' ').Where(x => x.Length > 0).Select(x =>
                        {
                            if (x == "x")
                                return -1;
                            else
                                return int.Parse(x);
                        }).ToArray();
                    }
                    continue;
                }

                // Split data line
                var parts = line.Split(';');
                System.Diagnostics.Debug.Assert(parts.Length == 2);

                // Get the directions
                var directions = parts[0].Split(' ').Select(x => DirectionalityFromName(x)).ToArray();

                // Get the bit set
                var bitset = Convert.ToInt32(parts[1].Trim(), 16);

                var pairTypes = Enumerable.Repeat(PairedBracketType.n, directions.Length).ToArray();
                var pairValues = Enumerable.Repeat(0, directions.Length).ToArray();

                for (int bit = 1; bit < 8; bit <<= 1)
                {
                    if ((bitset & bit) == 0)
                        continue;

                    sbyte paragraphEmbeddingLevel;
                    switch (bit)
                    {
                        case 1:
                            paragraphEmbeddingLevel = 2;        // Auto
                            break;

                        case 2:
                            paragraphEmbeddingLevel = 0;        // LTR
                            break;

                        case 4:
                            paragraphEmbeddingLevel = 1;        // RTL
                            break;

                        default:
                            throw new NotImplementedException();
                    }


                    tests.Add(new Test()
                    {
                        Types = new Slice<Directionality>(directions),
                        ParagraphEmbeddingLevel = paragraphEmbeddingLevel,
                        ExpectedLevels = levels,
                        LineNumber = lineNumber,
                    });
                }
            }

            Console.WriteLine($"Test data loaded: {tests.Count} test cases");

            var tr = new TestResults();

#if FOR_PROFILING
            for (int repeat = 0; repeat < 50; repeat++)
#endif
            for (int testNumber=0; testNumber<tests.Count; testNumber++)
            {
                var t = tests[testNumber];

                // Run the algorithm...
                Slice<sbyte> resultLevels;
                tr.EnterTest();
                bidi.Process(t.Types, Slice<PairedBracketType>.Empty, Slice<int>.Empty, t.ParagraphEmbeddingLevel, false, null, null, null);
                tr.LeaveTest();
                resultLevels = bidi.ResolvedLevels;


                // Check the results match
                bool pass = true;
                if (resultLevels.Length == t.ExpectedLevels.Length)
                {
                    for (int i = 0; i < t.ExpectedLevels.Length; i++)
                    {
                        if (t.ExpectedLevels[i] == -1)
                            continue;

                        if (resultLevels[i] != t.ExpectedLevels[i])
                        {
                            pass = false;
                            break;
                        }
                    }
                }
                else
                {
                    pass = false;
                }

                tr.TestPassed(pass);

                if (pass)
                {
//                    Console.WriteLine($"Passed line {t.LineNumber} {t.ParagraphEmbeddingLevel}");
                }
                else
                {
                    Console.WriteLine($"Failed line {t.LineNumber}");
                    Console.WriteLine();
                    Console.WriteLine($"        Data: {string.Join(" ", t.Types)}");
                    Console.WriteLine($" Embed Level: {t.ParagraphEmbeddingLevel}");
                    Console.WriteLine($"    Expected: {string.Join(" ", t.ExpectedLevels)}");
                    Console.WriteLine($"      Actual: {string.Join(" ", resultLevels)}");
                    Console.WriteLine();
                    return false;
                }

            }

            tr.Dump();

            return tr.AllPassed;
        }

        static Dictionary<string, Directionality> _dirnameMap;

        static Directionality DirectionalityFromName(string name)
        {
            if (_dirnameMap == null)
            {
                _dirnameMap = new Dictionary<string, Directionality>();
                for (var dir = Directionality.TYPE_MIN; dir <= Directionality.TYPE_MAX; dir++)
                {
                    _dirnameMap[dir.ToString()] = dir;
                }
            }

            return _dirnameMap[name];
        }

    }
}
