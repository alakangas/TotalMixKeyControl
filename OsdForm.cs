using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TotalMixKeyControl
{
	internal sealed class OsdForm : Form
	{
		private readonly System.Windows.Forms.Timer _hideTimer;
		private float _normalized = 0f;
		private float _db = -60f;
		private bool _showRawText = false;
		private string _rawText = string.Empty;

		// Layout configuration
		private enum Pos { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight }
		private Pos _pos = Pos.BottomCenter;
		private float _marginPercent = 0.03f;

		private const int MarginPx = 12;
		private const int BorderThickness = 2;
		private const int BarWidth = 320;
		private const int BarHeight = 24;

		private static readonly Color TermGreen = Color.FromArgb(0, 255, 0);
		private static readonly Color FillGreen = Color.FromArgb(180, 0, 255, 0);	
		private static readonly Color BorderGreen = Color.FromArgb(255, 0, 255, 0);

		public OsdForm()
		{
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			TopMost = true;
			StartPosition = FormStartPosition.Manual;
			DoubleBuffered = true;

			BackColor = Color.Fuchsia;
			TransparencyKey = Color.Fuchsia;

			_hideTimer = new System.Windows.Forms.Timer();
			_hideTimer.Tick += (_, __) => { _hideTimer.Stop(); Hide(); };

			Size = new Size(BarWidth, BarHeight);
			PositionForm();
		}

		protected override bool ShowWithoutActivation => true;

		protected override CreateParams CreateParams
		{
			get
			{
				const int WS_EX_TOOLWINDOW = 0x00000080;
				const int WS_EX_NOACTIVATE = 0x08000000;
				var cp = base.CreateParams;
				cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
				return cp;
			}
		}

		public void ShowLevel(float normalized, float dbValue, int durationMs)
		{
			_showRawText = false;
			_normalized = Math.Max(0f, Math.Min(1f, normalized));
			_db = dbValue;

			PositionForm();
			Show();
			_hideTimer.Stop();
			_hideTimer.Interval = Math.Max(250, durationMs);
			_hideTimer.Start();
			Invalidate();
		}

		public void ShowText(string text, int durationMs)
		{
			_showRawText = true;
			_rawText = text ?? string.Empty;

			PositionForm();
			Show();
			_hideTimer.Stop();
			_hideTimer.Interval = Math.Max(250, durationMs);
			_hideTimer.Start();
			Invalidate();
		}

		public void ShowBarWithText(float normalized, string text, int durationMs)
		{
			_showRawText = true;
			_normalized = Math.Max(0f, Math.Min(1f, normalized));
			_rawText = text ?? string.Empty;
			PositionForm();
			Show();
			_hideTimer.Stop();
			_hideTimer.Interval = Math.Max(250, durationMs);
			_hideTimer.Start();
			Invalidate();
		}

		public void ConfigureLayout(string position, string marginPreset)
		{
			// Position
			switch ((position ?? "").Trim().ToLowerInvariant())
			{
				case "topleft": _pos = Pos.TopLeft; break;
				case "topcenter": _pos = Pos.TopCenter; break;
				case "topright": _pos = Pos.TopRight; break;
				case "bottomleft": _pos = Pos.BottomLeft; break;
				case "bottomright": _pos = Pos.BottomRight; break;
				case "bottomcenter":
				default: _pos = Pos.BottomCenter; break;
			}

			// Margin preset to percentage
			switch ((marginPreset ?? "").Trim().ToLowerInvariant())
			{
				case "none": _marginPercent = 0.0f; break;
				case "medium": _marginPercent = 0.06f; break;
				case "large": _marginPercent = 0.09f; break;
				case "small":
				default: _marginPercent = 0.03f; break;
			}

			PositionForm();
		}

		private void PositionForm()
		{
			var wa = Screen.PrimaryScreen!.WorkingArea;
			int marginX = (int)Math.Round(wa.Width * _marginPercent);
			int marginY = (int)Math.Round(wa.Height * _marginPercent);

			int x = wa.Left, y = wa.Top;
			switch (_pos)
			{
				case Pos.TopLeft:
					x = wa.Left + marginX; y = wa.Top + marginY; break;
				case Pos.TopCenter:
					x = wa.Left + (wa.Width - Width) / 2; y = wa.Top + marginY; break;
				case Pos.TopRight:
					x = wa.Right - Width - marginX; y = wa.Top + marginY; break;
				case Pos.BottomLeft:
					x = wa.Left + marginX; y = wa.Bottom - Height - marginY; break;
				case Pos.BottomCenter:
					x = wa.Left + (wa.Width - Width) / 2; y = wa.Bottom - Height - marginY; break;
				case Pos.BottomRight:
					x = wa.Right - Width - marginX; y = wa.Bottom - Height - marginY; break;
			}

			// Clamp inside working area
			x = Math.Max(wa.Left, Math.Min(x, wa.Right - Width));
			y = Math.Max(wa.Top, Math.Min(y, wa.Bottom - Height));
			Location = new Point(x, y);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
			g.PageUnit = GraphicsUnit.Pixel;

			var rectOuter = new Rectangle(0, 0, Width - 1, Height - 1);
			var rectInner = new Rectangle(BorderThickness, BorderThickness,
										  Width - 2 * BorderThickness, Height - 2 * BorderThickness);

			using var font = new Font("Segoe UI", 9, FontStyle.Bold);
			var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
			string text = _showRawText ? _rawText : (float.IsNegativeInfinity(_db) ? "-∞ dB" : $"{_db:0.#} dB");

			int fillW = (int)Math.Round(rectInner.Width * _normalized);
			var rectFill = new Rectangle(rectInner.X, rectInner.Y, fillW, rectInner.Height);

			// Filled region
			using (var fill = new SolidBrush(FillGreen))
			{
				if (fillW > 0)
				{
					g.FillRectangle(fill, rectFill);
				}
			}

			// Text
			// Unfilled area: green
			using (var textBrushUnfilled = new SolidBrush(TermGreen))
			using (var unfilledClip = new Region(rectInner))
			{
				if (fillW > 0) unfilledClip.Exclude(rectFill);
				g.SetClip(unfilledClip, CombineMode.Replace);
				g.DrawString(text, font, textBrushUnfilled, rectInner, sf);
				g.ResetClip();
			}

			// Filled area: black
			using (var textBrushFilled = new SolidBrush(Color.Black))
			using (var filledClip = new Region(rectInner))
			{
				if (fillW > 0)
				{
					filledClip.Intersect(rectFill);
					g.SetClip(filledClip, CombineMode.Replace);
					g.DrawString(text, font, textBrushFilled, rectInner, sf);
					g.ResetClip();
				}
			}

			// Border
			using (var pen = new Pen(BorderGreen, BorderThickness))
			{
				g.DrawRectangle(pen, rectOuter);
			}
		}
	}
}
