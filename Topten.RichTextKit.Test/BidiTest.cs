using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class BidiTest
    {
        [Fact]
        public void Test()
        {
            // Read the test file
            var location = System.IO.Path.GetDirectoryName(typeof(LineBreakTests).Assembly.Location);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(location, "BidiTest.txt"));

            // Process each line
            int testCount = 0;
            int passCount = 0;
            int[] levels = null;
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Get the line, remove comments
                var line = lines[lineNumber-1].Split("#")[0].Trim();

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
                Assert.Equal(2, parts.Length);

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
                    

                    // Run the algorithm...
                    var bidi = new Bidi(new Slice<Directionality>(directions), new Slice<PairedBracketType>(pairTypes), new Slice<int>(pairValues), paragraphEmbeddingLevel);

                    // Check the results match
                    bool pass = true;
                    if (bidi.ResultLevels.Length == directions.Length)
                    {
                        for (int i = 0; i < bidi.Result.Length; i++)
                        {
                            /*
                            if (levels[i] == -1)
                                continue;
                                */

                            if ((int)bidi.ResultLevels[i] != levels[i])
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
                    else
                    {
                        int x = 3;
                    }

                    testCount++;
                }
            }

            Assert.Equal(testCount, passCount);
        }

        Dictionary<string, Directionality> _dirnameMap;

        Directionality DirectionalityFromName(string name)
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

        static bool IsHexDigit(char ch)
        {
            return char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
        }
    }
}
