using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topten.RichTextKit.Utils;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class TextBlockEditTests
    {
        [Fact]
        void InsertTextAssumeStyle()
        {
            // Arrange
            var tb = Parse("\\Axxx\\Byyy");

            // Act
            tb.InsertText(1, "123");

            // Assert
            Assert.Equal("\\Ax123xx\\Byyy", Format(tb));
        }

        [Fact]
        void InsertTextAssumeStyleAtBoundary()
        {
            // Arrange
            var tb = Parse("\\Axxx\\Byyy");

            // Act
            tb.InsertText(3, "123");

            // Assert
            Assert.Equal("\\Axxx123\\Byyy", Format(tb));
        }

        [Fact]
        void InsertTextAssumeStyleAtStart()
        {
            // Arrange
            var tb = Parse("\\Axxx\\Byyy");

            // Act
            tb.InsertText(0, "123");

            // Assert
            Assert.Equal("\\A123xxx\\Byyy", Format(tb));
        }


        [Fact]
        void InsertTextWithStyle()
        {
            // Arrange
            var tb = Parse("\\Axxx\\Byyy");

            // Act
            tb.InsertText(1, "123", new DummyStyle("C"));

            // Assert
            Assert.Equal("\\Ax\\C123\\Axx\\Byyy", Format(tb));
        }

        [Fact]
        void InsertTextWithStyleAtBoundary()
        {
            // Arrange
            var tb = Parse("\\Axxx\\Byyy");

            // Act
            tb.InsertText(3, "123", new DummyStyle("C"));

            // Assert
            Assert.Equal("\\Axxx\\C123\\Byyy", Format(tb));
        }

        [Fact]
        void InsertTextWithStyleAtStart()
        {
            // Arrange
            var tb = Parse("\\Axxx\\Byyy");

            // Act
            tb.InsertText(0, "123", new DummyStyle("C"));

            // Assert
            Assert.Equal("\\C123\\Axxx\\Byyy", Format(tb));
        }

        [Fact]
        void DeleteTextInsideRun()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(1, 3);

            // Assert
            Assert.Equal("\\Aae\\Bfgh\\Cijk", Format(tb));
        }

        [Fact]
        void DeleteTextEndOfRun()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(2, 3);

            // Assert
            Assert.Equal("\\Aab\\Bfgh\\Cijk", Format(tb));
        }

        [Fact]
        void DeleteTextStartOfRun()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(0, 3);

            // Assert
            Assert.Equal("\\Ade\\Bfgh\\Cijk", Format(tb));
        }

        [Fact]
        void DeleteTextEntireRun()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(0, 5);

            // Assert
            Assert.Equal("\\Bfgh\\Cijk", Format(tb));
        }

        [Fact]
        void DeleteTextAcrossRuns()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(3, 4);

            // Assert
            Assert.Equal("\\Aabc\\Bh\\Cijk", Format(tb));
        }

        [Fact]
        void DeleteTextContainingRun()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(3, 7);

            // Assert
            Assert.Equal("\\Aabc\\Ck", Format(tb));
        }

        [Fact]
        void DeleteTextContainingRunBoundaryStart()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(5, 5);

            // Assert
            Assert.Equal("\\Aabcde\\Ck", Format(tb));
        }

        [Fact]
        void DeleteTextContainingRunBoundaryEnd()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(3, 5);

            // Assert
            Assert.Equal("\\Aabc\\Cijk", Format(tb));
        }

        [Fact]
        void DeleteTextAtStart()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(0, 3);

            // Assert
            Assert.Equal("\\Ade\\Bfgh\\Cijk", Format(tb));
        }

        [Fact]
        void DeleteTextRunAtStart()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(0, 5);

            // Assert
            Assert.Equal("\\Bfgh\\Cijk", Format(tb));
        }

        [Fact]
        void DeleteTextAtEnd()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(9, 2);

            // Assert
            Assert.Equal("\\Aabcde\\Bfgh\\Ci", Format(tb));
        }

        [Fact]
        void DeleteTextRunAtEnd()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.DeleteText(8, 3);

            // Assert
            Assert.Equal("\\Aabcde\\Bfgh", Format(tb));
        }

        [Fact]
        void ApplyStyleAll()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(0, 11, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Dabcdefghijk", Format(tb));
        }

        [Fact]
        void ApplyStyleExistingRunFirst()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(0, 5, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Dabcde\\Bfgh\\Cijk", Format(tb));
        }

        [Fact]
        void ApplyStyleExistingRunMiddle()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(5, 3, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Aabcde\\Dfgh\\Cijk", Format(tb));
        }

        [Fact]
        void ApplyStyleExistingRunLast()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(8, 3, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Aabcde\\Bfgh\\Dijk", Format(tb));
        }

        [Fact]
        void ApplyStyleRunEnd()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(6, 2, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Aabcde\\Bf\\Dgh\\Cijk", Format(tb));
        }

        [Fact]
        void ApplyStyleRunStart()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(5, 2, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Aabcde\\Dfg\\Bh\\Cijk", Format(tb));
        }

        [Fact]
        void ApplyStyleRunInternal()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(1, 3, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Aa\\Dbcd\\Ae\\Bfgh\\Cijk", Format(tb));
        }

        [Fact]
        void ApplyStyleSpanRuns()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(3, 7, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Aabc\\Ddefghij\\Ck", Format(tb));
        }

        [Fact]
        void ApplyStyleMultipleRuns()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(5, 6, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Aabcde\\Dfghijk", Format(tb));
        }

        [Fact]
        void ApplyStyleMultipleRuns2()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Act
            tb.ApplyStyle(0, 8, new DummyStyle("D"));

            // Assert
            Assert.Equal("\\Dabcdefgh\\Cijk", Format(tb));
        }

        [Fact]
        void ApplyStyleCoalesc()
        {
            // Arrange
            var tb = Parse("\\Aabcde\\Bfgh\\Cijk");

            // Get style A instance
            var style = tb.StyleRuns[0].Style;

            // Act
            tb.ApplyStyle(8, 3, style);
            tb.ApplyStyle(5, 3, style);

            // Assert
            Assert.Equal("\\Aabcdefghijk", Format(tb));
        }


        [Fact]
        void TestFormatParse()
        {
            // This is a test that our Format/Parse methods used for testing
            // work correctly - not an actual test of the text block itself

            var input = "\\Axxx\\Byyy";
            var tb = Parse(input);

            // Check parsed correctly
            Assert.Equal(2, tb.StyleRuns.Count);
            Assert.Equal("A", (tb.StyleRuns[0].Style as DummyStyle).Name);
            Assert.Equal("xxx", tb.StyleRuns[0].ToString());
            Assert.Equal("B", (tb.StyleRuns[1].Style as DummyStyle).Name);
            Assert.Equal("yyy", tb.StyleRuns[1].ToString());

            // Now format it back 
            var result = Format(tb);

            // And check we got the same as the input
            Assert.Equal(input, result);
        }

        // Formats a text block into a string of format
        //     { '\' <styleName> <text> } 
        // where style name is a single character
        //
        // eg: \Axxx\Byyy
        //   first run = style "A", text "xxx"
        //  second run = style "B", text "yyy"
        // 
        // Use for easier expression of unit test code.
        string Format(TextBlock tb)
        {
            var sb = new StringBuilder();
            foreach (var sr in tb.StyleRuns)
            {
                sb.Append($"\\{(sr.Style as DummyStyle).Name}");
                sb.Append(sr.ToString());
            }
            return sb.ToString();
        }

        // Opposite of the Format function above, parsing a 
        // string into a text block
        TextBlock Parse(string str)
        {
            var tb = new TextBlock();
            Style currentStyle = new DummyStyle("A");
            var runStart = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\\')
                {
                    if (runStart < i)
                    {
                        tb.AddText(str.AsSpan(runStart, i - runStart), currentStyle);
                    }

                    i++;
                    currentStyle = new DummyStyle(new string(str[i], 1));
                    runStart = i+1;
                    continue;
                }
            }

            if (runStart < str.Length)
            {
                tb.AddText(str.AsSpan(runStart), currentStyle);
            }

            return tb;
        }

        // Dummy style with a name used for unit tests
        [System.Diagnostics.DebuggerDisplay("Style {Name}")]
        class DummyStyle : Style
        {
            public DummyStyle(string name)
            {
                Name = name;
            }

            public string Name { get; private set; }
        }

    }
}
