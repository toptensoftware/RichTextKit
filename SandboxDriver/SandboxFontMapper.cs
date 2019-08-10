using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Topten.RichTextKit;

namespace SandboxDriver
{
    class SandboxFontMapper : FontMapper
    {
        public SandboxFontMapper()
        {
            var stm = typeof(SandboxDriver).Assembly.GetManifestResourceStream("SandboxDriver.fontawesome.ttf");
            _fontAwesome = SKTypeface.FromStream(stm);
        }

        SKTypeface _fontAwesome;

        public override SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants)
        {
            if (style.FontFamily == "FontAwesome")
                return _fontAwesome;

            return base.TypefaceFromStyle(style, ignoreFontVariants);
        }
    }
}
