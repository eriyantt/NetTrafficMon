using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NetTrafficMon
{
    public class TaskbarWidget : Form
    {
        private readonly TrafficEngine _engine;
        private readonly AppSettings _settings;
        private readonly ToolTip _toolTip;
        private ContextMenuStrip _contextMenu;
        private Action _onDoubleClickAction;

        private Font _fontArrow;
        private Font _fontSpeed;

        private Rectangle _lastTbRect;
        private int _checkTbCounter = 0;

        public TaskbarWidget(TrafficEngine engine, AppSettings settings)
        {
            _engine = engine;
            _settings = settings;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;

            _toolTip = new ToolTip
            {
                InitialDelay = 400,
                ReshowDelay = 200,
                AutoPopDelay = 8000,
                ShowAlways = true
            };

            InitializeFonts();
            ApplySizeAndPosition();

            MouseDown += OnWidgetMouseDown;
            MouseUp += OnWidgetMouseUp;
            DoubleClick += OnWidgetDoubleClick;
            LocationChanged += OnWidgetLocationChanged;

            _engine.TrafficUpdated += OnTrafficUpdated;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_LAYERED;
                cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                cp.ExStyle |= NativeMethods.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        private void InitializeFonts()
        {
            try
            {
                _fontArrow = new Font("Segoe UI", 8.0f, FontStyle.Bold);
                _fontSpeed = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            }
            catch
            {
                _fontArrow = new Font(FontFamily.GenericSansSerif, 8.0f, FontStyle.Bold);
                _fontSpeed = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold);
            }
        }

        public void SetContextMenu(ContextMenuStrip menu)
        {
            _contextMenu = menu;
            ContextMenuStrip = menu;
        }

        public void SetDoubleClickHandler(Action action)
        {
            _onDoubleClickAction = action;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RenderLayeredWindow();
        }

        public void ApplyThemeAndLayout()
        {
            ApplySizeAndPosition();
            RenderLayeredWindow();
        }

        public static bool IsSmallTaskbar(Rectangle tbRect)
        {
            if (tbRect.Width > tbRect.Height)
            {
                // Horizontal taskbar: small icon mode is usually 30px or 32px
                return tbRect.Height > 0 && tbRect.Height <= 36;
            }
            else
            {
                // Vertical taskbar
                return tbRect.Width > 0 && tbRect.Width <= 45;
            }
        }

        public void ApplySizeAndPosition()
        {
            Rectangle tbRect = GetTaskbarRectangle();
            _lastTbRect = tbRect;
            bool isSmall = _settings.AutoSmallTaskbar && IsSmallTaskbar(tbRect);

            int w = 110;
            int h = 38;

            if (isSmall)
            {
                // Small taskbar mode (e.g. 30px - 34px)
                // Automatically use horizontal layout so text is never vertically squished!
                h = Math.Min(26, Math.Max(22, tbRect.Height - 4));

                if (_settings.Mode == DisplayMode.WithMiniGraph)
                {
                    // Horizontal with mini graph
                    w = 236;
                }
                else
                {
                    // Single line horizontal (CompactTwoLine also converts to horizontal on small taskbar)
                    w = 182;
                }
            }
            else
            {
                // Normal taskbar mode (40px or 48px)
                h = Math.Min(38, Math.Max(32, tbRect.Height - 6));

                switch (_settings.Mode)
                {
                    case DisplayMode.SingleLine:
                        w = 185;
                        h = Math.Min(30, tbRect.Height - 8);
                        break;
                    case DisplayMode.WithMiniGraph:
                        // Graph is closer to numbers (starts at x=84) and wider (65px wide)
                        w = 155;
                        break;
                    case DisplayMode.CompactTwoLine:
                    default:
                        w = 108;
                        break;
                }
            }

            Size = new Size(w, h);

            if (_settings.DockToTaskbar || _settings.WidgetX < 0 || _settings.WidgetY < 0)
            {
                DockToTaskbarPosition();
            }
            else
            {
                Location = new Point(_settings.WidgetX, _settings.WidgetY);
            }
        }

        public void DockToTaskbarPosition()
        {
            Rectangle tbRect = GetTaskbarRectangle();
            int x, y;

            if (tbRect.Width > tbRect.Height)
            {
                // Horizontal taskbar (Bottom or Top)
                y = tbRect.Top + (tbRect.Height - Height) / 2;

                // Dynamically find where the tray area begins so we NEVER overlap the chevron or tray icons!
                int trayLeft = GetTrayLeftEdge(tbRect);
                x = trayLeft - Width - 10;
                if (x < tbRect.Left + 20) x = tbRect.Left + 20;
            }
            else
            {
                // Vertical taskbar (Left or Right)
                x = tbRect.Left + (tbRect.Width - Width) / 2;
                y = tbRect.Bottom - Height - 280;
                if (y < tbRect.Top + 20) y = tbRect.Top + 20;
            }

            Location = new Point(x, y);
            _settings.WidgetX = x;
            _settings.WidgetY = y;
            _settings.DockToTaskbar = true;
            _settings.Save();

            RenderLayeredWindow();
        }

        public static int GetTrayLeftEdge(Rectangle tbRect)
        {
            // Default safe distance: in typical Windows 10/11 taskbars with clock,
            // action center, battery, network, audio, and chevron overflow,
            // the notification area width is approximately 310 - 340px.
            int fallbackLeft = tbRect.Right - 330;

            try
            {
                IntPtr trayHwnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
                if (trayHwnd != IntPtr.Zero)
                {
                    IntPtr notifyHwnd = NativeMethods.FindWindowEx(trayHwnd, IntPtr.Zero, "TrayNotifyWnd", null);
                    if (notifyHwnd != IntPtr.Zero)
                    {
                        NativeMethods.RECT nr;
                        if (NativeMethods.GetWindowRect(notifyHwnd, out nr) && nr.Left > tbRect.Left && nr.Left < tbRect.Right)
                        {
                            return nr.Left;
                        }
                    }
                }
            }
            catch { }

            return fallbackLeft;
        }

        public static Rectangle GetTaskbarRectangle()
        {
            var screen = Screen.PrimaryScreen;
            var b = screen.Bounds;
            var w = screen.WorkingArea;

            if (w.Top > b.Top)
                return new Rectangle(b.Left, b.Top, b.Width, w.Top - b.Top);
            if (w.Bottom < b.Bottom)
                return new Rectangle(b.Left, w.Bottom, b.Width, b.Bottom - w.Bottom);
            if (w.Left > b.Left)
                return new Rectangle(b.Left, b.Top, w.Left - b.Left, b.Height);
            if (w.Right < b.Right)
                return new Rectangle(w.Right, b.Top, b.Right - w.Right, b.Height);

            return new Rectangle(b.Left, b.Bottom - 48, b.Width, 48);
        }

        private void OnTrafficUpdated(TrafficSample sample)
        {
            if (IsDisposed || !IsHandleCreated) return;

            // Check if taskbar dimensions changed (e.g. user toggled small taskbar icons)
            _checkTbCounter++;
            if (_checkTbCounter >= 2)
            {
                _checkTbCounter = 0;
                Rectangle currentTb = GetTaskbarRectangle();
                if (currentTb != _lastTbRect)
                {
                    ApplySizeAndPosition();
                }
            }

            string upText = FormatHelper.FormatSpeed(_engine.CurrentUploadSpeed, _settings.Unit, _settings.DecimalPlaces);
            string downText = FormatHelper.FormatSpeed(_engine.CurrentDownloadSpeed, _settings.Unit, _settings.DecimalPlaces);
            string upTotal = FormatHelper.FormatBytes(_engine.SessionBytesSent);
            string downTotal = FormatHelper.FormatBytes(_engine.SessionBytesReceived);

            string tip = string.Format(
                "NetTrafficMon\n" +
                "Adapter: {0}\n" +
                "IP: {1}\n" +
                "--------------------\n" +
                "▲ Upload:   {2} (Total: {3})\n" +
                "▼ Download: {4} (Total: {5})\n" +
                "--------------------\n" +
                "Double-click: Details & Graph\n" +
                "Right-click: Settings",
                _engine.ActiveAdapterName,
                _engine.ActiveAdapterIPv4,
                upText, upTotal,
                downText, downTotal);

            _toolTip.SetToolTip(this, tip);

            NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);

            RenderLayeredWindow();
        }

        private void OnWidgetLocationChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated)
            {
                RenderLayeredWindow();
            }
        }

        private void OnWidgetMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !_settings.LockPosition)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, 0);

                _settings.WidgetX = Location.X;
                _settings.WidgetY = Location.Y;

                Rectangle tb = GetTaskbarRectangle();
                _settings.DockToTaskbar = Bounds.IntersectsWith(tb);
                _settings.Save();

                RenderLayeredWindow();
            }
        }

        private void OnWidgetMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _contextMenu != null)
            {
                _contextMenu.Show(Cursor.Position);
            }
        }

        private void OnWidgetDoubleClick(object sender, EventArgs e)
        {
            if (_onDoubleClickAction != null)
            {
                _onDoubleClickAction();
            }
        }

        public void RenderLayeredWindow()
        {
            if (!IsHandleCreated || IsDisposed) return;
            if (Width <= 0 || Height <= 0) return;

            IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);

            NativeMethods.BITMAPINFO bi = new NativeMethods.BITMAPINFO();
            bi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(NativeMethods.BITMAPINFOHEADER));
            bi.bmiHeader.biWidth = Width;
            bi.bmiHeader.biHeight = -Height; // Top-down
            bi.bmiHeader.biPlanes = 1;
            bi.bmiHeader.biBitCount = 32;
            bi.bmiHeader.biCompression = 0;

            IntPtr pBits;
            IntPtr hDib = NativeMethods.CreateDIBSection(memDc, ref bi, 0, out pBits, IntPtr.Zero, 0);
            IntPtr oldDib = NativeMethods.SelectObject(memDc, hDib);

            try
            {
                using (Bitmap bmp = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppPArgb, pBits))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.Clear(Color.Transparent);

                    // Invisible hit-test background so dragging and clicks work everywhere
                    using (Brush hitTestBrush = new SolidBrush(Color.FromArgb(1, 0, 0, 0)))
                    {
                        g.FillRectangle(hitTestBrush, 0, 0, Width, Height);
                    }

                    int alphaByte = (int)(Math.Max(0.2, Math.Min(1.0, _settings.Opacity)) * 255);
                    Color upColor = FormatHelper.ColorFromHex(_settings.UploadColor, Color.FromArgb(255, 112, 67));
                    Color downColor = FormatHelper.ColorFromHex(_settings.DownloadColor, Color.FromArgb(0, 230, 118));
                    Color textColor = FormatHelper.ColorFromHex(_settings.TextColor, Color.White);

                    // Theme background rendering
                    if (_settings.Theme == AppTheme.Transparent)
                    {
                        // 100% Transparent theme: taskbar shows directly through
                    }
                    else if (_settings.Theme == AppTheme.Light)
                    {
                        // Light theme: crisp light pill
                        Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                        using (GraphicsPath path = CreateRoundedRectangle(rect, 5))
                        {
                            using (Brush bgBrush = new SolidBrush(Color.FromArgb(alphaByte, 240, 242, 245)))
                            {
                                g.FillPath(bgBrush, path);
                            }
                            using (Pen borderPen = new Pen(Color.FromArgb(alphaByte, 205, 210, 215), 1.0f))
                            {
                                g.DrawPath(borderPen, path);
                            }
                        }
                        textColor = Color.FromArgb(25, 25, 28);
                    }
                    else
                    {
                        // Dark theme: sleek dark pill, ZERO white lines or white corners
                        Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                        Color userBg = FormatHelper.ColorFromHex(_settings.BackgroundColor, Color.FromArgb(26, 26, 30));
                        using (GraphicsPath path = CreateRoundedRectangle(rect, 5))
                        {
                            using (Brush bgBrush = new SolidBrush(Color.FromArgb(alphaByte, userBg.R, userBg.G, userBg.B)))
                            {
                                g.FillPath(bgBrush, path);
                            }
                            using (Pen borderPen = new Pen(Color.FromArgb(Math.Min(255, alphaByte + 20), 45, 45, 52), 1.0f))
                            {
                                g.DrawPath(borderPen, path);
                            }
                        }
                    }

                    string upStr = FormatHelper.FormatSpeed(_engine.CurrentUploadSpeed, _settings.Unit, _settings.DecimalPlaces);
                    string downStr = FormatHelper.FormatSpeed(_engine.CurrentDownloadSpeed, _settings.Unit, _settings.DecimalPlaces);
                    bool isTransparent = (_settings.Theme == AppTheme.Transparent);

                    Rectangle tbRect = GetTaskbarRectangle();
                    bool isSmall = _settings.AutoSmallTaskbar && IsSmallTaskbar(tbRect);

                    if (isSmall)
                    {
                        // Small taskbar: render horizontally
                        if (_settings.Mode == DisplayMode.WithMiniGraph)
                        {
                            DrawHorizontalWithMiniGraph(g, upStr, downStr, upColor, downColor, textColor, isTransparent);
                        }
                        else
                        {
                            DrawSingleLine(g, upStr, downStr, upColor, downColor, textColor, isTransparent);
                        }
                    }
                    else
                    {
                        // Normal taskbar
                        if (_settings.Mode == DisplayMode.SingleLine)
                        {
                            DrawSingleLine(g, upStr, downStr, upColor, downColor, textColor, isTransparent);
                        }
                        else if (_settings.Mode == DisplayMode.WithMiniGraph)
                        {
                            DrawWithMiniGraph(g, upStr, downStr, upColor, downColor, textColor, isTransparent);
                        }
                        else
                        {
                            DrawCompactTwoLine(g, upStr, downStr, upColor, downColor, textColor, isTransparent);
                        }
                    }
                }

                NativeMethods.SIZE size = new NativeMethods.SIZE { cx = Width, cy = Height };
                NativeMethods.POINT pointSource = new NativeMethods.POINT { X = 0, Y = 0 };
                NativeMethods.POINT topPos = new NativeMethods.POINT { X = Left, Y = Top };
                NativeMethods.BLENDFUNCTION blend = new NativeMethods.BLENDFUNCTION
                {
                    BlendOp = NativeMethods.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = NativeMethods.AC_SRC_ALPHA
                };

                NativeMethods.UpdateLayeredWindow(
                    Handle,
                    screenDc,
                    ref topPos,
                    ref size,
                    memDc,
                    ref pointSource,
                    0,
                    ref blend,
                    NativeMethods.ULW_ALPHA);
            }
            finally
            {
                NativeMethods.SelectObject(memDc, oldDib);
                NativeMethods.DeleteObject(hDib);
                NativeMethods.DeleteDC(memDc);
                NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        private void DrawCompactTwoLine(Graphics g, string upStr, string downStr, Color upColor, Color downColor, Color textColor, bool isTransparent)
        {
            int startX = 6;
            int row1Y = 3;
            int row2Y = 19;

            if (isTransparent)
            {
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                {
                    g.DrawString("▲", _fontArrow, shadowBrush, startX + 1, row1Y + 1);
                    g.DrawString(upStr, _fontSpeed, shadowBrush, startX + 14, row1Y + 1);
                    g.DrawString("▼", _fontArrow, shadowBrush, startX + 1, row2Y + 1);
                    g.DrawString(downStr, _fontSpeed, shadowBrush, startX + 14, row2Y + 1);
                }
            }

            // Upload row
            using (Brush arrowBrush = new SolidBrush(upColor))
            {
                g.DrawString("▲", _fontArrow, arrowBrush, startX, row1Y);
            }
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(upStr, _fontSpeed, textBrush, startX + 13, row1Y);
            }

            // Download row
            using (Brush arrowBrush = new SolidBrush(downColor))
            {
                g.DrawString("▼", _fontArrow, arrowBrush, startX, row2Y);
            }
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(downStr, _fontSpeed, textBrush, startX + 13, row2Y);
            }
        }

        private void DrawSingleLine(Graphics g, string upStr, string downStr, Color upColor, Color downColor, Color textColor, bool isTransparent)
        {
            int y = (Height - 16) / 2;

            if (isTransparent)
            {
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                {
                    g.DrawString("▲", _fontArrow, shadowBrush, 7, y + 1);
                    g.DrawString(upStr, _fontSpeed, shadowBrush, 21, y + 1);
                    g.DrawString("▼", _fontArrow, shadowBrush, 97, y + 1);
                    g.DrawString(downStr, _fontSpeed, shadowBrush, 111, y + 1);
                }
            }

            // Upload
            int x = 6;
            using (Brush arrowBrush = new SolidBrush(upColor))
            {
                g.DrawString("▲", _fontArrow, arrowBrush, x, y);
            }
            x += 14;
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(upStr, _fontSpeed, textBrush, x, y);
            }

            // Download
            x = 96;
            using (Brush arrowBrush = new SolidBrush(downColor))
            {
                g.DrawString("▼", _fontArrow, arrowBrush, x, y);
            }
            x += 14;
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(downStr, _fontSpeed, textBrush, x, y);
            }
        }

        private void DrawWithMiniGraph(Graphics g, string upStr, string downStr, Color upColor, Color downColor, Color textColor, bool isTransparent)
        {
            // Draw 2 lines of speed numbers
            DrawCompactTwoLine(g, upStr, downStr, upColor, downColor, textColor, isTransparent);

            // Position graph close ("mepet") right after speed numbers (at x = 84 instead of 119!)
            int graphX = 84;
            int graphY = 5;
            int graphW = Math.Max(40, Width - graphX - 6); // 65px wide!
            int graphH = Height - 10;

            DrawGraphCanvas(g, graphX, graphY, graphW, graphH, upColor, downColor, isTransparent);
        }

        private void DrawHorizontalWithMiniGraph(Graphics g, string upStr, string downStr, Color upColor, Color downColor, Color textColor, bool isTransparent)
        {
            int y = (Height - 16) / 2;

            if (isTransparent)
            {
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                {
                    g.DrawString("▲", _fontArrow, shadowBrush, 7, y + 1);
                    g.DrawString(upStr, _fontSpeed, shadowBrush, 21, y + 1);
                    g.DrawString("▼", _fontArrow, shadowBrush, 93, y + 1);
                    g.DrawString(downStr, _fontSpeed, shadowBrush, 107, y + 1);
                }
            }

            // Upload
            int x = 6;
            using (Brush arrowBrush = new SolidBrush(upColor))
            {
                g.DrawString("▲", _fontArrow, arrowBrush, x, y);
            }
            x += 14;
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(upStr, _fontSpeed, textBrush, x, y);
            }

            // Download
            x = 92;
            using (Brush arrowBrush = new SolidBrush(downColor))
            {
                g.DrawString("▼", _fontArrow, arrowBrush, x, y);
            }
            x += 14;
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(downStr, _fontSpeed, textBrush, x, y);
            }

            // Mini graph next to download
            int graphX = 172;
            int graphY = 4;
            int graphW = Math.Max(40, Width - graphX - 6);
            int graphH = Height - 8;

            DrawGraphCanvas(g, graphX, graphY, graphW, graphH, upColor, downColor, isTransparent);
        }

        private void DrawGraphCanvas(Graphics g, int graphX, int graphY, int graphW, int graphH, Color upColor, Color downColor, bool isTransparent)
        {
            Rectangle graphRect = new Rectangle(graphX, graphY, graphW, graphH);

            if (!isTransparent)
            {
                using (Brush graphBg = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                {
                    g.FillRectangle(graphBg, graphRect);
                }
                using (Pen graphBorder = new Pen(Color.FromArgb(50, 60, 60, 68), 1.0f))
                {
                    g.DrawRectangle(graphBorder, graphRect);
                }
            }
            else
            {
                // In transparent mode, subtle outline without solid gray block
                using (Pen graphBorder = new Pen(Color.FromArgb(70, 75, 75, 85), 1.0f))
                {
                    g.DrawRectangle(graphBorder, graphRect);
                }
            }

            var hist = _engine.History;
            if (hist == null || hist.Count < 2) return;

            int count = Math.Min(hist.Count, 24);
            int startIndex = hist.Count - count;

            double maxSpeed = 1024; // floor 1 KB/s
            for (int i = startIndex; i < hist.Count; i++)
            {
                var s = hist[i];
                if (s.DownloadSpeed > maxSpeed) maxSpeed = s.DownloadSpeed;
                if (s.UploadSpeed > maxSpeed) maxSpeed = s.UploadSpeed;
            }

            PointF[] downPoints = new PointF[count];
            PointF[] upPoints = new PointF[count];

            float step = (float)graphW / Math.Max(1, count - 1);

            for (int i = 0; i < count; i++)
            {
                var s = hist[startIndex + i];
                float px = graphX + i * step;
                float pyDown = (float)(graphY + graphH - (s.DownloadSpeed / maxSpeed) * (graphH - 2));
                float pyUp = (float)(graphY + graphH - (s.UploadSpeed / maxSpeed) * (graphH - 2));

                downPoints[i] = new PointF(px, Math.Max(graphY + 1, Math.Min(graphY + graphH - 1, pyDown)));
                upPoints[i] = new PointF(px, Math.Max(graphY + 1, Math.Min(graphY + graphH - 1, pyUp)));
            }

            using (Pen downPen = new Pen(Color.FromArgb(220, downColor), 1.2f))
            {
                g.DrawLines(downPen, downPoints);
            }
            using (Pen upPen = new Pen(Color.FromArgb(220, upColor), 1.2f))
            {
                g.DrawLines(upPen, upPoints);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_fontArrow != null) _fontArrow.Dispose();
                if (_fontSpeed != null) _fontSpeed.Dispose();
                if (_toolTip != null) _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
