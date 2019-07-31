﻿using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Topten.RichText;

namespace Sandbox
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ResizeRedraw = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        private Bitmap _bitmap;

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // Create bitmap
            var info = new SKImageInfo(Width, Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
			if (_bitmap == null || _bitmap.Width != info.Width || _bitmap.Height != info.Height)
			{
                _bitmap?.Dispose();

				if (info.Width != 0 && info.Height != 0)
					_bitmap = new Bitmap(info.Width, info.Height, PixelFormat.Format32bppPArgb);
			}

            // Lock bits
            var data = _bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, _bitmap.PixelFormat);

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

        SandboxDriver.SandboxDriver _driver = new SandboxDriver.SandboxDriver();

        void OnRender(SKCanvas canvas)
        {
            _driver.Render(canvas, canvas.LocalClipBounds.Width, canvas.LocalClipBounds.Height);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    _driver.ContentMode = (_driver.ContentMode + 1) % _driver.ContentModeCount;
                    Invalidate();
                    break;

                case Keys.Left:
                    if (e.Modifiers.HasFlag(Keys.Control))
                    {
                        _driver.BaseDirection = TextDirection.LTR;
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
                    _driver.Scale += 0.5f;
                    Invalidate();
                    break;

                case Keys.Down:
                    _driver.Scale -= 0.5f;
                    if (_driver.Scale < 0.5f)
                        _driver.Scale = 0.5f;
                    Invalidate();
                    break;

                case Keys.Z:
                    _driver.UseMSWordStyleRTLLayout = !_driver.UseMSWordStyleRTLLayout;
                    Invalidate();
                    break;

                case Keys.W:
                    _driver.UseMaxWidth = !_driver.UseMaxWidth;
                    Invalidate();
                    break;

                case Keys.H:
                    _driver.UseMaxHeight = !_driver.UseMaxHeight;
                    Invalidate();
                    break;

                case Keys.M:
                    _driver.ShowMeasuredSize = !_driver.ShowMeasuredSize;
                    Invalidate();
                    break;

            }
        }
    }
}
