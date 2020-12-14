using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Topten.RichTextKit;

namespace RichStringSandbox
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ResizeRedraw = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            /*
            _richString = new RichString()
                        .FontSize(18).FontFamily("Open Sans")
                        .Alignment(TextAlignment.Center).MarginBottom(20)
                        .Add("CHARACTER SPACING!!", letterSpacing: 10, fontSize: 28)
                        .Paragraph()
                        .Alignment(TextAlignment.Left)
                        .Add("Para2a\nParam2b")
                        ;
            var rs = new RichString()
                .Alignment(TextAlignment.Center)
                .FontFamily("Segoe UI")
                .MarginBottom(20)
                .Add("Welcome To RichTextKit", fontSize:24, fontWeight: 700, fontItalic: true)
                .Paragraph().Alignment(TextAlignment.Left)
                .FontSize(18)
                .Add("This is a test string");
            */

            var rs = new RichString()
                .Add("Big text", fontSize: 40, letterSpacing: 0, backgroundColor: new SKColor(0xFFFF0000))
                .Add("Little text", fontSize: 12, letterSpacing: 0);


            _richString = rs;
        }

        private Bitmap _bitmap;

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // Create bitmap
            var info = new SKImageInfo(this.ClientSize.Width, this.ClientSize.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
			if (_bitmap == null || _bitmap.Width != info.Width || _bitmap.Height != info.Height)
			{
                _bitmap?.Dispose();

				if (info.Width != 0 && info.Height != 0)
					_bitmap = new Bitmap(info.Width, info.Height, PixelFormat.Format32bppPArgb);
			}

            // Lock bits
            var data = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.WriteOnly, _bitmap.PixelFormat);

			// create the surface
			using (var surface = SKSurface.Create(info, data.Scan0, data.Stride))
			{
                surface.Canvas.Scale(DeviceDpi/96.0f);
                OnRender(surface.Canvas);
				surface.Canvas.Flush();
			}

			// write the bitmap to the graphics
			_bitmap.UnlockBits(data);
			e.Graphics.DrawImage(_bitmap, 0, 0);
        }

        RichString _richString;

        bool _useMaxWidth;
        bool _useMaxHeight;

        const float margin = 60;

        void OnRender(SKCanvas canvas)
        {
            canvas.Clear(new SKColor(0xFFFFFFFF));


            float canvasWidth = canvas.LocalClipBounds.Width;
            float canvasHeight = canvas.LocalClipBounds.Height;
            float? height = (float)(canvasHeight - margin * 2);
            float? width = (float)(canvasWidth - margin * 2);

            if (!_useMaxHeight)
                height = null;
            if (!_useMaxWidth)
                width = null;

            using (var gridlinePaint = new SKPaint() { Color = new SKColor(0x20000000), StrokeWidth = 1 })
            {
                canvas.DrawLine(new SKPoint(margin, 0), new SKPoint(margin, (float)canvasHeight), gridlinePaint);
                if (width.HasValue)
                    canvas.DrawLine(new SKPoint(margin + width.Value, 0), new SKPoint(margin + width.Value, (float)canvasHeight), gridlinePaint);
                canvas.DrawLine(new SKPoint(0, margin), new SKPoint((float)canvasWidth, margin), gridlinePaint);
                if (height.HasValue)
                     canvas.DrawLine(new SKPoint(0, margin + height.Value), new SKPoint((float)canvasWidth, margin + height.Value), gridlinePaint);
            }

            _richString.MaxWidth = width;
            _richString.MaxHeight = height;

            var state = $"Measured: {_richString.MeasuredWidth} x {_richString.MeasuredHeight} Lines: {_richString.LineCount} Truncated: {_richString.Truncated} Length: {_richString.MeasuredLength} Revision: {_richString.Revision}";
            canvas.DrawText(state, margin, 20, new SKPaint()
            {
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 12,
                IsAntialias = true,
            });

            state = $"Hit Test: Over {_htr.OverCodePointIndex} Line {_htr.OverLine}.  Closest: {_htr.ClosestCodePointIndex} Line {_htr.ClosestLine}";
            canvas.DrawText(state, margin, 40, new SKPaint()
            {
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 12,
                IsAntialias = true,
            });

            var options = new TextPaintOptions()
            {
                SelectionColor = new SKColor(0x60FF0000),
            };

            if (_htr.OverCodePointIndex >= 0)
            {
                options.Selection = new TextRange(_htr.OverCodePointIndex, _htr.OverCodePointIndex + 1);
            }

            _richString.Paint(canvas, new SKPoint(margin, margin), options);

            if (_htr.ClosestCodePointIndex >= 0)
            {
                var ci = _richString.GetCaretInfo(new CaretPosition(_htr.ClosestCodePointIndex, false));
                using (var paint = new SKPaint()
                {
                    Color = new SKColor(0xFF000000),
                    IsStroke = true,
                    IsAntialias = true,
                    StrokeWidth = 1,
                })
                {
                    var rect = ci.CaretRectangle;
                    rect.Offset(margin, margin);
                    canvas.DrawLine(rect.Right, rect.Top, rect.Left, rect.Bottom, paint);
                }
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                    _useMaxWidth = !_useMaxWidth;
                    Invalidate();
                    break;

                case Keys.H:
                    _useMaxHeight = !_useMaxHeight;
                    Invalidate();
                    break;
                    /*
                    case Keys.Space:
                        _driver.ContentMode = (_driver.ContentMode + 1) % _driver.ContentModeCount;
                        Invalidate();
                        break;

                    case Keys.Left:
                        if (e.Modifiers.HasFlag(Keys.Control))
                        {
                            if (_driver.BaseDirection == TextDirection.RTL)
                                _driver.BaseDirection = TextDirection.LTR;
                            else if (_driver.BaseDirection == TextDirection.LTR)
                                _driver.BaseDirection = TextDirection.Auto;
                        }
                        else
                        {
                            if (_driver.TextAlignment > TextAlignment.Auto)
                                _driver.TextAlignment--;
                        }

                        Invalidate();
                        break;

                    case Keys.Right:
                        if (e.Modifiers.HasFlag(Keys.Control))
                        {
                            if (_driver.BaseDirection == TextDirection.Auto)
                                _driver.BaseDirection = TextDirection.LTR;
                            else if (_driver.BaseDirection == TextDirection.LTR)
                                _driver.BaseDirection = TextDirection.RTL;
                        }
                        else
                        {
                            if (_driver.TextAlignment < TextAlignment.Right)
                                _driver.TextAlignment++;
                        }

                        Invalidate();
                        break;

                    case Keys.Up:
                        _driver.Scale += 0.1f;
                        Invalidate();
                        break;

                    case Keys.Down:
                        _driver.Scale -= 0.1f;
                        if (_driver.Scale < 0.5f)
                            _driver.Scale = 0.5f;
                        Invalidate();
                        break;


                    case Keys.M:
                        _driver.ShowMeasuredSize = !_driver.ShowMeasuredSize;
                        Invalidate();
                        break;

                }
                */
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            //_driver.HitTest(e.X * 96.0f / DeviceDpi, e.Y * 96.0f / DeviceDpi);
            //Invalidate();
        }

        HitTestResult _htr;

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            var htr = _richString.HitTest(e.X * 96.0f / DeviceDpi - margin, e.Y * 96.0f / DeviceDpi - margin);

            if (!_htr.Equals(htr))
            {
                _htr = htr;
                Invalidate();
            }
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {

        }
    }
}
