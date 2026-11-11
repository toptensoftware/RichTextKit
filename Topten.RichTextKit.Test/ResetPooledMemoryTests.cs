using System;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class ResetPooledMemoryTests
    {
        // Each pool resets objects as they're returned.  ResetPooledMemory must keep
        // that, or reused objects come back holding their old state.  (The pools are
        // per thread, so this test only affects its own thread.)
        [Fact]
        void PoolsStillResetObjectsAfterResetPooledMemory()
        {
            // Arrange
            TextBlock.ResetPooledMemory();
            var fontRun = FontRun.Pool.Value.Get();
            var line = TextLine.Pool.Value.Get();
            var styleRun = StyleRun.Pool.Value.Get();
            fontRun.Style = new Style();
            line.TextBlock = new TextBlock();
            styleRun.CodePointBuffer = new Utils.Utf32Buffer("x");

            // Act
            FontRun.Pool.Value.Return(fontRun);
            TextLine.Pool.Value.Return(line);
            StyleRun.Pool.Value.Return(styleRun);

            // Assert
            var reusedFontRun = FontRun.Pool.Value.Get();
            var reusedLine = TextLine.Pool.Value.Get();
            var reusedStyleRun = StyleRun.Pool.Value.Get();
            Assert.Same(fontRun, reusedFontRun);
            Assert.Same(line, reusedLine);
            Assert.Same(styleRun, reusedStyleRun);
            Assert.Null(reusedFontRun.Style);
            Assert.Null(reusedLine.TextBlock);
            Assert.Null(reusedStyleRun.CodePointBuffer);
        }
    }
}
