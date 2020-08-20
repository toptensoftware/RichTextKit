using System;
using System.Collections.Generic;
using System.Text;
using Topten.RichTextKit;

namespace Topten.RichTextKit.Test
{
    public static class TextBlockLoadTest
    {
        public static bool Run()
        {
            Console.WriteLine("Text Block Load Test");
            Console.WriteLine("--------------------");
            Console.WriteLine();

            string typefaceName = "Arial";
            float Scale = 1.5f;
            var styleSmall = new Style() { FontFamily = typefaceName, FontSize = 12 * Scale };
            var styleScript = new Style() { FontFamily = "Segoe Script", FontSize = 18 * Scale };
            var styleHeading = new Style() { FontFamily = typefaceName, FontSize = 24 * Scale, FontWeight = 700 };
            var styleNormal = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, LineHeight = 1.0f };
            var styleBold = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, FontWeight = 700 };
            var styleUnderline = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, Underline = UnderlineStyle.Gapped };
            var styleStrike = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, StrikeThrough = StrikeThroughStyle.Solid };
            var styleSubScript = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, FontVariant = FontVariant.SubScript };
            var styleSuperScript = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, FontVariant = FontVariant.SuperScript };
            var styleItalic = new Style() { FontFamily = typefaceName, FontItalic = true, FontSize = 18 * Scale };
            var styleBoldLarge = new Style() { FontFamily = typefaceName, FontSize = 28 * Scale, FontWeight = 700 };
            var styleRed = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale/*, TextColor = new SKColor(0xFFFF0000) */};
            var styleBlue = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale/*, TextColor = new SKColor(0xFF0000FF) */};


            var tr = new TestResults();
            var tb = new TextBlock();

            for (int i = 0; i < 1000; i++)
            {
                tr.EnterTest();

                tb.Clear();
                tb.MaxWidth = 1000;
                tb.AddText("Welcome to RichTextKit!\n", styleHeading);
                tb.AddText("\nRichTextKit is a rich text layout, rendering and measurement library for SkiaSharp.\n\nIt supports normal, ", styleNormal);
                tb.AddText("bold", styleBold);
                tb.AddText(", ", styleNormal);
                tb.AddText("italic", styleItalic);
                tb.AddText(", ", styleNormal);
                tb.AddText("underline", styleUnderline);
                tb.AddText(" (including ", styleNormal);
                tb.AddText("gaps over descenders", styleUnderline);
                tb.AddText("), ", styleNormal);
                tb.AddText("strikethrough", styleStrike);
                tb.AddText(", superscript (E=mc", styleNormal);
                tb.AddText("2", styleSuperScript);
                tb.AddText("), subscript (H", styleNormal);
                tb.AddText("2", styleSubScript);
                tb.AddText("O), ", styleNormal);
                tb.AddText("colored ", styleRed);
                tb.AddText("text", styleBlue);
                tb.AddText(" and ", styleNormal);
                tb.AddText("mixed ", styleNormal);
                tb.AddText("sizes", styleSmall);
                tb.AddText(" and ", styleNormal);
                tb.AddText("fonts", styleScript);
                tb.AddText(".\n\n", styleNormal);
                tb.AddText("Font fallback means emojis work: 🌐 🍪 🍕 🚀 and ", styleNormal);
                tb.AddText("text shaping and bi-directional text support means complex scripts and languages like Arabic: مرحبا بالعالم, Japanese: ハローワールド, Chinese: 世界您好 and Hindi: हैलो वर्ल्ड are rendered correctly!\n\n", styleNormal);
                tb.AddText("RichTextKit also supports left/center/right text alignment, word wrapping, truncation with ellipsis place-holder, text measurement, hit testing, painting a selection range, caret position & shape helpers.", styleNormal);
                tb.Layout();
                tr.LeaveTest();
                tr.TestPassed(true);
            }

            tr.Dump();
            return tr.AllPassed;
        }
    }
}
