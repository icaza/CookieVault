using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CookieVault
{
    // ─────────────────────────────────────────────
    //  Rounded TextBox with custom border
    // ─────────────────────────────────────────────
    public class RoundedTextBox : UserControl
    {
        readonly TextBox inner;
        public Color BorderColor = Color.FromArgb(40, 40, 65);
        public Color FocusBorderColor = Color.FromArgb(100, 180, 255);
        bool focused = false;

        public new Font Font
        {
            get => inner.Font;
            set { inner.Font = value; }
        }

        public new Color ForeColor
        {
            get => inner.ForeColor;
            set { inner.ForeColor = value; }
        }

        public new Color BackColor
        {
            get => base.BackColor;
            set { base.BackColor = value; inner.BackColor = value; }
        }

        public new string Text
        {
            get => inner.Text;
            set { inner.Text = value; }
        }

        public new event KeyEventHandler KeyDown
        {
            add { inner.KeyDown += value; }
            remove { inner.KeyDown -= value; }
        }

        public void SelectAll() => inner.SelectAll();

        public RoundedTextBox()
        {
            inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(22, 22, 32),
                ForeColor = Color.FromArgb(200, 210, 240),
                Dock = DockStyle.None,
                Font = new Font("Segoe Mono", 9.5f)
            };
            inner.GotFocus += (s, e) => { focused = true; Invalidate(); };
            inner.LostFocus += (s, e) => { focused = false; Invalidate(); };
            inner.TextChanged += (s, e) => OnTextChanged(e);

            Controls.Add(inner);
            Padding = new Padding(10, 0, 10, 0);
            DoubleBuffered = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int pad = 10;
            int h = inner.PreferredHeight;
            inner.SetBounds(pad, (Height - h) / 2, Width - pad * 2, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            int r = 8;

            using (var brush = new SolidBrush(inner.BackColor))
                FillRoundedRect(e.Graphics, brush, rect, r);

            var borderColor = focused ? FocusBorderColor : BorderColor;
            using (var pen = new Pen(borderColor, focused ? 1.5f : 1f))
                DrawRoundedRect(e.Graphics, pen, rect, r);
        }

        void FillRoundedRect(Graphics g, Brush b, RectangleF r, float radius)
        {
            using (var path = RoundedPath(r, radius))
                g.FillPath(b, path);
        }

        void DrawRoundedRect(Graphics g, Pen p, RectangleF r, float radius)
        {
            using (var path = RoundedPath(r, radius))
                g.DrawPath(p, path);
        }

        GraphicsPath RoundedPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ─────────────────────────────────────────────
    //  Icon Button (Navigate / Refresh)
    // ─────────────────────────────────────────────
    public class IconButton : UserControl
    {
        public new string Text { get; set; }
        public Color IconColor
        {
            get => iconColor;
            set { iconColor = value; Invalidate(); }
        }
        Color iconColor;
        readonly Color bgColor;
        bool hovered = false;

        public IconButton(string icon, Color iconColor, Color bgColor)
        {
            Text = icon;
            this.iconColor = iconColor;
            this.bgColor = bgColor;
            Size = new Size(36, 36);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);

            var bg = hovered ? Color.FromArgb(30, iconColor.R, iconColor.G, iconColor.B) : Color.Transparent;
            using (var brush = new SolidBrush(bg))
            using (var path = RoundPath(rect, 8))
                e.Graphics.FillPath(brush, path);

            using (var pen = new Pen(Color.FromArgb(hovered ? 80 : 40, iconColor), 1))
            using (var path = RoundPath(rect, 8))
                e.Graphics.DrawPath(pen, path);

            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var font = new Font("Segoe UI", 13f))
            using (var brush = new SolidBrush(iconColor))
                e.Graphics.DrawString(Text, font, brush, new RectangleF(0, 0, Width, Height), sf);
        }

        GraphicsPath RoundPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ─────────────────────────────────────────────
    //  Chrome Button (Close / Minimize)
    // ─────────────────────────────────────────────
    public class ChromeButton : UserControl
    {
        readonly string symbol;
        readonly Color accentColor;
        bool hovered = false;

        public ChromeButton(string symbol, Color accentColor)
        {
            this.symbol = symbol;
            this.accentColor = accentColor;
            Size = new Size(22, 22);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(1, 1, Width - 2, Height - 2);

            var fillColor = hovered ? accentColor : Color.FromArgb(35, 35, 50);
            using (var brush = new SolidBrush(fillColor))
            using (var path = CirclePath(rect))
                e.Graphics.FillPath(brush, path);

            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var font = new Font("Segoe UI", 7.5f))
            using (var brush = new SolidBrush(hovered ? Color.White : Color.FromArgb(90, 90, 110)))
                e.Graphics.DrawString(symbol, font, brush, new RectangleF(0, 0, Width, Height), sf);
        }

        GraphicsPath CirclePath(RectangleF r)
        {
            var p = new GraphicsPath();
            p.AddEllipse(r);
            return p;
        }
    }

    // ─────────────────────────────────────────────
    //  Resize Grip
    // ─────────────────────────────────────────────
    public class ResizeGrip : UserControl
    {
        readonly Form target;
        Point dragStart;
        Size startSize;

        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public ResizeGrip(Form target)
        {
            this.target = target;
            Size = new Size(16, 16);
            Cursor = Cursors.SizeNWSE;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var c = Color.FromArgb(50, 50, 70);
            int s = 4;
            for (int i = 1; i <= 3; i++)
            {
                using (var pen = new Pen(c, 1.2f))
                {
                    int off = i * s;
                    e.Graphics.DrawLine(pen, Width - off, Height, Width, Height - off);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStart = Cursor.Position;
                startSize = target.Size;
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Capture)
            {
                var delta = new Point(Cursor.Position.X - dragStart.X, Cursor.Position.Y - dragStart.Y);
                target.Size = new Size(
                    Math.Max(target.MinimumSize.Width, startSize.Width + delta.X),
                    Math.Max(target.MinimumSize.Height, startSize.Height + delta.Y));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            Capture = false;
        }
    }

    // ─────────────────────────────────────────────
    //  Status Panel (unused placeholder)
    // ─────────────────────────────────────────────
    public class StatusPanel : Panel
    {
        public StatusPanel() { }
    }
}
