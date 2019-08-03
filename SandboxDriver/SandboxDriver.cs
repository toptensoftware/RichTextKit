using System;
using Topten.RichTextKit;
using SkiaSharp;
using System.Diagnostics;

namespace SandboxDriver
{
    public class SandboxDriver
    {
        public int ContentModeCount = 9;
        public int ContentMode = 0;
        public TextDirection BaseDirection = TextDirection.LTR;
        public TextAlignment TextAlignment = TextAlignment.Auto;
        public bool UseMSWordStyleRTLLayout = false;
        public float Scale = 1.0f;
        public bool UseMaxWidth = true;
        public bool UseMaxHeight = false;
        public bool ShowMeasuredSize = false;

        public void Render(SKCanvas canvas, float canvasWidth, float canvasHeight)
        {
            canvas.Clear(new SKColor(0xFFFFFFFF));

            const float margin = 80;


            float? height = (float)(canvasHeight - margin * 2);
            float? width = (float)(canvasWidth - margin * 2);
            //width = 25;

            if (!UseMaxHeight)
                height = null;
            if (!UseMaxWidth)
                width = null;

            using (var gridlinePaint = new SKPaint() { Color = new SKColor(0xFFFF0000), StrokeWidth = 1 })
            {
                canvas.DrawLine(new SKPoint(margin, 0), new SKPoint(margin, (float)canvasHeight), gridlinePaint);
                if (width.HasValue)
                    canvas.DrawLine(new SKPoint(margin + width.Value, 0), new SKPoint(margin + width.Value, (float)canvasHeight), gridlinePaint);
                canvas.DrawLine(new SKPoint(0, margin), new SKPoint((float)canvasWidth, margin), gridlinePaint);
                if (height.HasValue)
                    canvas.DrawLine(new SKPoint(0, margin + height.Value), new SKPoint((float)canvasWidth, margin + height.Value), gridlinePaint);
            }

            //string typefaceName = "Times New Roman";
            string typefaceName = "Segoe UI";

            var styleNormal = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, LineHeight = 1.0f };
            var styleUnderline = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, Underline = UnderlineStyle.Gapped, TextColor = new SKColor(0xFF0000FF) };
            var styleStrike = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, StrikeThrough = StrikeThroughStyle.Solid, TextColor = new SKColor(0xFFFF0000) };
            var styleSubScript = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, FontVariant = FontVariant.SubScript };
            var styleSuperScript = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, FontVariant = FontVariant.SuperScript };
            var styleItalic = new Style() { FontFamily = typefaceName, FontItalic = true, FontSize = 18 * Scale };
            var styleBold = new Style() { FontFamily = typefaceName, FontSize = 28 * Scale, FontWeight = 700 };
            var styleRed = new Style() { FontFamily = typefaceName, FontSize = 18 * Scale, TextColor = new SKColor(0xFFFF0000) };


            var tle = new TextBlock();
            tle.MaxWidth = width;
            tle.MaxHeight = height;
            tle.Clear();

            tle.BaseDirection = BaseDirection;
            tle.Alignment = TextAlignment;
            tle.UseMSWordStyleRTLLayout = UseMSWordStyleRTLLayout;

            switch (ContentMode)
            {
                case 0:
                    tle.AddText("Hello Wor", styleNormal);
                    tle.AddText("ld", styleRed);
                    tle.AddText(". This is normal 18px. These are emojis: 🌐 🍪 🍕 🚀 ", styleNormal);
                    tle.AddText("This is ", styleNormal);
                    tle.AddText("bold 28px", styleBold);
                    tle.AddText(". ", styleNormal);
                    tle.AddText("This is italic", styleItalic);
                    tle.AddText(". This is ", styleNormal);
                    tle.AddText("red", styleRed);
                    tle.AddText(". This is Arabic: (", styleNormal);
                    tle.AddText("تسجّل ", styleNormal);
                    tle.AddText("يتكلّم", styleNormal);
                    tle.AddText("), Hindi: ", styleNormal);
                    tle.AddText("हालाँकि प्रचलित रूप पूज", styleNormal);
                    tle.AddText(", Han: ", styleNormal);
                    tle.AddText("緳 踥踕", styleNormal);
                    break;

                case 1:
                    tle.AddText("Hello Wor", styleNormal);
                    tle.AddText("ld", styleRed);
                    tle.AddText(".\nThis is normal 18px.\nThese are emojis: 🌐 🍪 🍕 🚀\n", styleNormal);
                    tle.AddText("This is ", styleNormal);
                    tle.AddText("bold 28px", styleBold);
                    tle.AddText(".\n", styleNormal);
                    tle.AddText("This is italic", styleItalic);
                    tle.AddText(".\nThis is ", styleNormal);
                    tle.AddText("red", styleRed);
                    /*
                    tle.AddText(".\nThis is Arabic: (", styleNormal);
                    tle.AddText("تسجّل ", styleNormal);
                    tle.AddText("يتكلّم", styleNormal);
                    tle.AddText("), Hindi: ", styleNormal);
                    */
                    tle.AddText(".\nThis is Arabic: (تسجّل يتكلّم), Hindi: ", styleNormal);
                    tle.AddText("हालाँकि प्रचलित रूप पूज", styleNormal);
                    tle.AddText(", Han: ", styleNormal);
                    tle.AddText("緳 踥踕", styleNormal);
                    break;

                case 2:
                    tle.AddText("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus semper, sapien vitae placerat sollicitudin, lorem diam aliquet quam, id finibus nisi quam eget lorem.\nDonec facilisis sem nec rhoncus elementum. Cras laoreet porttitor malesuada.\n\nVestibulum sed lacinia diam. Mauris a mollis enim. Cras in rhoncus mauris, at vulputate nisl. Sed nec lobortis dolor, hendrerit posuere quam. Vivamus malesuada sit amet nunc ac cursus. Praesent volutpat euismod efficitur. Nam eu ante.", styleNormal);
                    break;

                case 3:
                    tle.AddText("مرحبا بالعالم.  هذا هو اختبار التفاف \nالخط للتأكد من أنه يعمل للغات من اليمين إلى اليسار.", styleNormal);
                    break;

                case 4:
                    tle.AddText("مرحبا بالعالم.  هذا هو اختبار التفاف الخط للتأكد من \u2066ACME Inc.\u2069 أنه يعمل للغات من اليمين إلى اليسار.", styleNormal);
                    break;

                case 5:
                    tle.AddText("Subscript: H", styleNormal);
                    tle.AddText("2", styleSubScript);
                    tle.AddText("O  Superscript: E=mc", styleNormal);
                    tle.AddText("2", styleSuperScript);
                    tle.AddText("  Key: C", styleNormal);
                    tle.AddText("♯", styleSuperScript);
                    tle.AddText(" B", styleNormal);
                    tle.AddText("♭", styleSubScript);
                    break;

                case 6:
                    tle.AddText("The quick brown fox jumps over the lazy dog.", styleUnderline);
                    tle.AddText(" ", styleNormal);
                    tle.AddText("Strike Through", styleStrike);
                    break;

                case 7:
                    tle.AddText("Apples and Bananas\r\n", styleNormal);
                    tle.AddText("Pears\r\n", styleNormal);
                    tle.AddText("Bananas\r\n", styleNormal);
                    break;

                case 8:
                    tle.AddText("Hello World", styleNormal);
                    break;

            }

            var sw = new Stopwatch();
            sw.Start();
            tle.Layout();
            var elapsed = sw.ElapsedMilliseconds;

            var options = new TextPaintOptions()
            {
                SelectionColor = new SKColor(0x60FF0000),
            };

            HitTestResult? htr = null;
            CaretInfo? ci = null;
            if (_showHitTest)
            {
                htr = tle.HitTest(_hitTestX - margin, _hitTestY - margin);
                if (htr.Value.OverCluster >= 0)
                {
                    options.SelectionStart = htr.Value.OverCluster;
                    options.SelectionEnd = tle.CursorIndicies[tle.LookupCursorIndex(htr.Value.OverCluster) + 1];
                }

                ci = tle.GetCursorInfo(htr.Value.ClosestCluster);
            }

            if (ShowMeasuredSize)
            {
                using (var paint = new SKPaint()
                {
                    Color = new SKColor(0x1000FF00),
                    IsStroke = false,
                })
                {
                    var rect = new SKRect(margin + tle.MeasuredInset, margin, margin + tle.MeasureWidth + tle.MeasuredInset, margin + tle.MeasureHeight);
                    canvas.DrawRect(rect, paint);
                }
            }

            if (tle.RequiredLeftMargin > 0)
            {
                using (var paint = new SKPaint() { Color = new SKColor(0xFFf0f0f0), StrokeWidth = 1 })
                {
                    canvas.DrawLine(new SKPoint(margin - tle.RequiredLeftMargin, 0), new SKPoint(margin - tle.RequiredLeftMargin, (float)canvasHeight), paint);
                }
            }

            tle.Paint(canvas, new SKPoint(margin, margin), options);

            if (ci != null)
            {
                using (var paint = new SKPaint()
                {
                    Color = new SKColor(0xFF000000),
                    IsStroke = true,
                    IsAntialias = true,
                    StrokeWidth = Scale,
                })
                {
                    var rect = ci.Value.CaretRectangle;
                    rect.Offset(margin, margin);
                    canvas.DrawLine(rect.Right, rect.Top, rect.Left, rect.Bottom, paint);
                }
            }

            var state = $"Size: {width} x {height} Base Direction: {BaseDirection} Alignment: {TextAlignment} Content: {ContentMode} scale: {Scale} time: {elapsed} msword: {UseMSWordStyleRTLLayout}";
            canvas.DrawText(state, margin, 20, new SKPaint()
            {
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 12,
            });

            state = $"Selection: {options.SelectionStart}-{options.SelectionEnd} Closest: {(htr.HasValue ? htr.Value.ClosestCluster.ToString() : "-")}";
            canvas.DrawText(state, margin, 40, new SKPaint()
            {
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 12,
            });
        }

        float _hitTestX;
        float _hitTestY;
        bool _showHitTest;

        public void HitTest(float x, float y)
        {
            _hitTestX = x;
            _hitTestY = y;
            _showHitTest = true;
        }

    }   
}
