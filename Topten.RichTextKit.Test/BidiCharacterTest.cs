using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class BidiCharacterTest
    {
        class Test
        {
            public int LineNumber;
            public int[] CodePoints;
            public sbyte ParagraphLevel;
            public sbyte ResolvedParagraphLevel;
            public sbyte[] ResolvedLevels;
            public int[] ResolvedOrder;
        }

        [Fact]
        public void RunTests()
        {
            Assert.True(Run());
        }

        public static bool Run()
        {
            Console.WriteLine("Bidi Character Tests");
            Console.WriteLine("--------------------");
            Console.WriteLine();

            // Read the test file
            var location = System.IO.Path.GetDirectoryName(typeof(BidiCharacterTest).Assembly.Location);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(location, "TestData\\BidiCharacterTest.txt"));

            // Parse lines
            var tests = new List<Test>();
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Get the line, remove comments
                var line = lines[lineNumber - 1].Split('#')[0].Trim();

                // Ignore blank/comment only lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Split into fields
                var fields = line.Split(';');

                var test = new Test();
                test.LineNumber = lineNumber;

                // Parse field 0 - code points
                test.CodePoints = fields[0].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.ToInt32(x, 16)).ToArray();

                // Parse field 1 - paragraph level
                test.ParagraphLevel = sbyte.Parse(fields[1]);

                // Parse field 2 - resolved paragraph level
                test.ResolvedParagraphLevel = sbyte.Parse(fields[2]);

                // Parse field 3 - resolved levels
                test.ResolvedLevels = fields[3].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => x == "x" ? (sbyte)-1 : Convert.ToSByte(x)).ToArray();

                // Parse field 4 - resolved levels
                test.ResolvedOrder = fields[4].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.ToInt32(x)).ToArray();

                tests.Add(test);
            }

            Console.WriteLine($"Test data loaded: {tests.Count} test cases");

            var bidi = new Bidi();
            var bidiData = new BidiData();

            // Run tests...
            var tr = new TestResults();
            for (int testNumber = 0; testNumber < tests.Count; testNumber++)
            {
                var t = tests[testNumber];

                // Arrange
                bidiData.Init(new Slice<int>(t.CodePoints), t.ParagraphLevel);

                // Act

                tr.EnterTest();
                for (int i = 0; i < 10; i++)
                {
                    bidi.Process(bidiData);
                }
                tr.LeaveTest();
                var resultLevels = bidi.ResolvedLevels;
                int resultParagraphLevel = bidi.ResolvedParagraphEmbeddingLevel;

                // Assert
                bool passed = true;
                if (t.ResolvedParagraphLevel != resultParagraphLevel)
                {
                    passed = false;
                }
                for (int i = 0; i < t.ResolvedLevels.Length; i++)
                {
                    if (t.ResolvedLevels[i] == -1)
                        continue;

                    if (t.ResolvedLevels[i] != resultLevels[i])
                    {
                        passed = false;
                        break;
                    }
                }

                /*
                if (!passed)
                {
                    Console.WriteLine($"Failed line {t.LineNumber}");
                    Console.WriteLine();
                    Console.WriteLine($"             Code Points: {string.Join(" ", t.CodePoints.Select(x => x.ToString("X4")))}");
                    Console.WriteLine($"      Pair Bracket Types: {string.Join(" ", bidiData.PairedBracketTypes.Select(x => "   " + x.ToString()))}");
                    Console.WriteLine($"     Pair Bracket Values: {string.Join(" ", bidiData.PairedBracketValues.Select(x => x.ToString("X4")))}");
                    Console.WriteLine($"             Embed Level: {t.ParagraphLevel}");
                    Console.WriteLine($"    Expected Embed Level: {t.ResolvedParagraphLevel}");
                    Console.WriteLine($"      Actual Embed Level: {resultParagraphLevel}");
                    Console.WriteLine($"          Directionality: {string.Join(" ", bidiData.Types)}");
                    Console.WriteLine($"         Expected Levels: {string.Join(" ", t.ResolvedLevels)}");
                    Console.WriteLine($"           Actual Levels: {string.Join(" ", resultLevels)}");
                    Console.WriteLine();
                    return false;
                }
                */

                // Record it
                tr.TestPassed(passed);
            }

            tr.Dump();

            return tr.AllPassed;
        }
    }
}
