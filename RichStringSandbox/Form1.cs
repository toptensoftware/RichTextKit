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

            _richString = new RichString()
                        .FontSize(28).FontFamily("Open Sans")
                        .Alignment(TextAlignment.Left)
                        .Add("Para1")
                        .Paragraph()
                        .Paragraph()
                        .Add("Para2a\nParam2b")
                        ;
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

        void OnRender(SKCanvas canvas)
        {
            canvas.Clear(new SKColor(0xFFFFFFFF));

            float margin = 30;

            float canvasWidth = canvas.LocalClipBounds.Width;
            float canvasHeight = canvas.LocalClipBounds.Height;
            float? height = (float)(canvasHeight - margin * 2);
            float? width = (float)(canvasWidth - margin * 2);

            if (!_useMaxHeight)
                height = null;
            if (!_useMaxWidth)
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

            _richString.MaxWidth = width;
            _richString.MaxHeight = height;


            _richString.Paint(canvas, new SKPoint(margin, margin));
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

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            //_driver.HitTest(e.X * 96.0f / DeviceDpi, e.Y * 96.0f / DeviceDpi);
            //Invalidate();
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {

        }
    }
}
