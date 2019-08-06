//#define USE_BIDI2

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Topten.RichTextKit;
using Topten.RichTextKit.Utils;

namespace BidiSandbox
{
    class Program
    {
        class Test
        {
            public Slice<Directionality> Types;
            public Slice<PairedBracketType> PairedBracketTypes;
            public Slice<int> PairedBracketValues;
            public int ParagraphEmbeddingLevel;
            public int[] ExpectedLevels;
            public int LineNumber;
        }

        static void Main(string[] args)
        {
             // Read the test file
            var location = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(location, "BidiTest.txt"));

#if USE_BIDI2
            var bidi2 = new Bidi2();
#endif

            var sw = new Stopwatch();

            var memUsed = 0L;

            List<Test> tests = new List<Test>();

            // Process each line
            int testCount = 0;
            int passCount = 0;
            int[] levels = null;
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Get the line, remove comments
                var line = lines[lineNumber - 1].Split("#")[0].Trim();

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
                var parts = line.Split(";");
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

                    byte paragraphEmbeddingLevel;
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
                        PairedBracketTypes = new Slice<PairedBracketType>(pairTypes),
                        PairedBracketValues = new Slice<int>(pairValues),
                        ParagraphEmbeddingLevel = paragraphEmbeddingLevel,
                        ExpectedLevels = levels,
                        LineNumber = lineNumber,
                    });
                }
            }

            Console.WriteLine($"Test data loaded: {tests.Count} test cases");

            // Capture GC Counts
            System.GC.Collect();
            var preGCCounts = new List<long>();
            for (int i = 0; i < System.GC.MaxGeneration; i++)
            {
                preGCCounts.Add(System.GC.CollectionCount(i));
            }

            for (int testNumber=0; testNumber<tests.Count; testNumber++)
            {
                var t = tests[testNumber];

                // Run the algorithm...
                var memBefore = System.GC.GetTotalMemory(false);
                Slice<int> resultLevels;
#if USE_BIDI2
                    sw.Start();
                    bidi2.Process(t.Types, t.PairedBracketTypes, t.PairedBracketValues, t.ParagraphEmbeddingLevel);
                    sw.Stop();
                    resultLevels = bidi2.ResultLevels;
#else
                    sw.Start();
                    var bidi = new Bidi(t.Types, t.PairedBracketTypes, t.PairedBracketValues, (byte)t.ParagraphEmbeddingLevel);
                    sw.Stop();
                    resultLevels = new Slice<int>(bidi.getLevels(new int[] { t.Types.Length }).Select(x => (int)x).ToArray());
#endif

                var memAfter = System.GC.GetTotalMemory(false);
                if (memAfter > memBefore)
                    memUsed += (memAfter - memBefore);

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

                if (pass)
                    passCount++;

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
                    return;
                }

                testCount++;
                if ((testCount % 10000) == 0)
                {
                    Console.WriteLine($"Progress: line {testCount} of {tests.Count}");
                }
            }

            Console.WriteLine();
#if USE_BIDI2
            Console.WriteLine("Bidi Version 2");
#else
            Console.WriteLine("Bidi Version 1");
#endif
            Console.WriteLine($"Passed {passCount} of {testCount} tests");
            Console.WriteLine($"Time in algorithm: {sw.Elapsed}");
            Console.WriteLine($"Memory in algorithm: {memUsed:n0}");
            // Display collection counts.
            Console.WriteLine($"GC Collection Counts:");
            for (int i = 0; i < GC.MaxGeneration; i++)
            {
                Console.WriteLine($"  - generation #{i}: {GC.CollectionCount(i) - preGCCounts[i]}");
            }
            Console.WriteLine();
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
