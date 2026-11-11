using System;
using SkiaSharp;
using Xunit;

namespace Topten.RichTextKit.Test
{
    public class StyleManagerTests
    {
        // A style that inherits its font width from the current style must not
        // share a cache entry with an otherwise identical style of a different width.
        [Fact]
        void UpdateKeepsInheritedFontWidth()
        {
            // Arrange
            var sm = new StyleManager();
            sm.Update(fontSize: 20);                                // cached with normal width
            sm.Update(fontWidth: SKFontStyleWidth.Condensed);

            // Act
            var style = sm.Update(fontSize: 20);                    // inherits condensed

            // Assert
            Assert.Equal(SKFontStyleWidth.Condensed, style.FontWidth);
            Assert.Equal(20, style.FontSize);
        }
    }
}
