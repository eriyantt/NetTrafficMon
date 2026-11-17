using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace NetTrafficMon
{
    public class DetailsForm : Form
    {
        private readonly TrafficEngine _engine;
        private readonly AppSettings _settings;

        private Label _lblDownloadSpeed;
        private Label _lblDownloadPeak;
        private Label _lblDownloadTotal;

        private Label _lblUploadSpeed;
        private Label _lblUploadPeak;
        private Label _lblUploadTotal;

        private Label _lblAdapterName;
        private Label _lblAdapterIp;
        private Label _lblAdapterMac;
        private Label _lblAdapterSpeed;
        private Label _lblUptime;

        private Panel _graphPanel;

        public DetailsForm(TrafficEngine engine, AppSettings settings)
        {
            _engine = engine;
            _settings = settings;

            InitializeComponent();

            _engine.TrafficUpdated += OnTrafficUpdated;
            FormClosing += (s, e) =>
            {
                _engine.TrafficUpdated -= OnTrafficUpdated;
            };
        }

        private void InitializeComponent()
        {
            Text = "NetTrafficMon - Network Details & Activity";
            Size = new Size(620, 560);
            MinimumSize = new Size(580, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(24, 24, 27);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            Icon = CreateFormIcon();

            // Main layout container
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(16),
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f)); // Speed cards
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Graph
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f)); // Adapter info
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));  // Buttons

            // --- Row 0: Speed Cards (Download & Upload) ---
            var speedCardsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            speedCardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            speedCardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // Download Card (Green)
            var cardDownload = CreateCardPanel(Color.FromArgb(16, 185, 129, 30));
            cardDownload.Margin = new Padding(0, 0, 8, 0);
            var dlTitle = new Label { Text = "▼ DOWNLOAD", ForeColor = Color.FromArgb(0, 230, 118), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 10) };
            _lblDownloadSpeed = new Label { Text = "0.0 KB/s", ForeColor = Color.White, Font = new Font("Segoe UI", 16f, FontStyle.Bold), AutoSize = true, Location = new Point(10, 28) };
            _lblDownloadPeak = new Label { Text = "Peak: 0.0 KB/s", ForeColor = Color.FromArgb(180, 180, 190), Font = new Font("Segoe UI", 8.0f), AutoSize = true, Location = new Point(12, 64) };
            _lblDownloadTotal = new Label { Text = "Total: 0 B", ForeColor = Color.FromArgb(180, 180, 190), Font = new Font("Segoe UI", 8.0f), AutoSize = true, Location = new Point(12, 82) };
            cardDownload.Controls.Add(dlTitle);
            cardDownload.Controls.Add(_lblDownloadSpeed);
            cardDownload.Controls.Add(_lblDownloadPeak);
            cardDownload.Controls.Add(_lblDownloadTotal);

            // Upload Card (Orange)
            var cardUpload = CreateCardPanel(Color.FromArgb(249, 115, 22, 30));
            cardUpload.Margin = new Padding(8, 0, 0, 0);
            var upTitle = new Label { Text = "▲ UPLOAD", ForeColor = Color.FromArgb(255, 112, 67), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 10) };
            _lblUploadSpeed = new Label { Text = "0.0 KB/s", ForeColor = Color.White, Font = new Font("Segoe UI", 16f, FontStyle.Bold), AutoSize = true, Location = new Point(10, 28) };
            _lblUploadPeak = new Label { Text = "Peak: 0.0 KB/s", ForeColor = Color.FromArgb(180, 180, 190), Font = new Font("Segoe UI", 8.0f), AutoSize = true, Location = new Point(12, 64) };
            _lblUploadTotal = new Label { Text = "Total: 0 B", ForeColor = Color.FromArgb(180, 180, 190), Font = new Font("Segoe UI", 8.0f), AutoSize = true, Location = new Point(12, 82) };
            cardUpload.Controls.Add(upTitle);
            cardUpload.Controls.Add(_lblUploadSpeed);
            cardUpload.Controls.Add(_lblUploadPeak);
            cardUpload.Controls.Add(_lblUploadTotal);

            speedCardsPanel.Controls.Add(cardDownload, 0, 0);
            speedCardsPanel.Controls.Add(cardUpload, 1, 0);
            mainLayout.Controls.Add(speedCardsPanel, 0, 0);

            // --- Row 1: Real-time Graph ---
            var graphCard = CreateCardPanel(Color.FromArgb(32, 32, 36));
            graphCard.Margin = new Padding(0, 0, 0, 10);
            _graphPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 24)
            };
            _graphPanel.Paint += OnGraphPaint;
            graphCard.Controls.Add(_graphPanel);
            mainLayout.Controls.Add(graphCard, 0, 1);

            // --- Row 2: Adapter Info ---
            var adapterCard = CreateCardPanel(Color.FromArgb(32, 32, 36));
            adapterCard.Margin = new Padding(0, 0, 0, 10);

            var infoTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12, 8, 12, 8)
            };
            infoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            infoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            _lblAdapterName = new Label { Text = "Adapter: ...", ForeColor = Color.White, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold), AutoSize = true };
            _lblAdapterIp = new Label { Text = "IPv4: ...", ForeColor = Color.FromArgb(200, 200, 210), AutoSize = true };
            _lblAdapterMac = new Label { Text = "MAC: ...", ForeColor = Color.FromArgb(160, 160, 170), AutoSize = true };
            _lblAdapterSpeed = new Label { Text = "Link: ...", ForeColor = Color.FromArgb(160, 160, 170), AutoSize = true };
            _lblUptime = new Label { Text = "Session Uptime: ...", ForeColor = Color.FromArgb(160, 160, 170), AutoSize = true };

            infoTable.Controls.Add(_lblAdapterName, 0, 0);
            infoTable.Controls.Add(_lblUptime, 1, 0);
            infoTable.Controls.Add(_lblAdapterIp, 0, 1);
            infoTable.Controls.Add(_lblAdapterSpeed, 1, 1);
            infoTable.Controls.Add(_lblAdapterMac, 0, 2);

            adapterCard.Controls.Add(infoTable);
            mainLayout.Controls.Add(adapterCard, 0, 2);

            // --- Row 3: Action Buttons ---
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0)
            };

            var btnClose = new Button
            {
                Text = "Close",
                Width = 90,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            var btnReset = new Button
            {
                Text = "Reset Session",
                Width = 110,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += (s, e) =>
            {
                _engine.ResetSessionStats();
                UpdateLabels();
                _graphPanel.Invalidate();
            };

            var btnSettings = new Button
            {
                Text = "Windows Network Settings",
                Width = 180,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Click += (s, e) =>
            {
                try { Process.Start("ms-settings:network"); } catch { }
            };

            buttonsPanel.Controls.Add(btnClose);
            buttonsPanel.Controls.Add(btnReset);
            buttonsPanel.Controls.Add(btnSettings);
            mainLayout.Controls.Add(buttonsPanel, 0, 3);

            Controls.Add(mainLayout);
            UpdateLabels();
        }

        private Panel CreateCardPanel(Color bgColor)
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgColor
            };
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                }
            };
            return p;
        }

        private void OnTrafficUpdated(TrafficSample sample)
        {
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke((Action)(() =>
            {
                UpdateLabels();
                _graphPanel.Invalidate();
            }));
        }

        private void UpdateLabels()
        {
            _lblDownloadSpeed.Text = FormatHelper.FormatSpeed(_engine.CurrentDownloadSpeed, _settings.Unit, _settings.DecimalPlaces);
            _lblDownloadPeak.Text = "Peak: " + FormatHelper.FormatSpeed(_engine.PeakDownloadSpeed, _settings.Unit, _settings.DecimalPlaces);
            _lblDownloadTotal.Text = "Total Received: " + FormatHelper.FormatBytes(_engine.SessionBytesReceived);

            _lblUploadSpeed.Text = FormatHelper.FormatSpeed(_engine.CurrentUploadSpeed, _settings.Unit, _settings.DecimalPlaces);
            _lblUploadPeak.Text = "Peak: " + FormatHelper.FormatSpeed(_engine.PeakUploadSpeed, _settings.Unit, _settings.DecimalPlaces);
            _lblUploadTotal.Text = "Total Sent: " + FormatHelper.FormatBytes(_engine.SessionBytesSent);

            _lblAdapterName.Text = "Adapter: " + (string.IsNullOrEmpty(_engine.ActiveAdapterName) ? "None" : _engine.ActiveAdapterName);
            _lblAdapterIp.Text = "IPv4: " + (string.IsNullOrEmpty(_engine.ActiveAdapterIPv4) ? "N/A" : _engine.ActiveAdapterIPv4);
            _lblAdapterMac.Text = "MAC: " + (string.IsNullOrEmpty(_engine.ActiveAdapterMac) ? "N/A" : _engine.ActiveAdapterMac);

            if (_engine.ActiveAdapterSpeedBps > 0)
            {
                _lblAdapterSpeed.Text = "Link: " + FormatHelper.FormatSpeed(_engine.ActiveAdapterSpeedBps / 8.0, SpeedUnit.BitsPerSecond, 0);
            }
            else
            {
                _lblAdapterSpeed.Text = "Link: N/A";
            }

            TimeSpan uptime = DateTime.Now - _engine.StartTime;
            _lblUptime.Text = string.Format("Uptime: {0:D2}:{1:D2}:{2:D2}", (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds);
        }

        private void OnGraphPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = _graphPanel.Width;
            int h = _graphPanel.Height;
            if (w < 50 || h < 50) return;

            int leftPadding = 60;
            int rightPadding = 15;
            int topPadding = 25;
            int bottomPadding = 25;

            int plotW = w - leftPadding - rightPadding;
            int plotH = h - topPadding - bottomPadding;

            // Draw grid & background
            using (var brush = new SolidBrush(Color.FromArgb(18, 18, 22)))
            {
                g.FillRectangle(brush, leftPadding, topPadding, plotW, plotH);
            }

            // Find max speed in history
            var hist = _engine.History;
            double maxSpeed = 1024 * 10; // floor at 10 KB/s
            for (int i = 0; i < hist.Count; i++)
            {
                if (hist[i].DownloadSpeed > maxSpeed) maxSpeed = hist[i].DownloadSpeed;
                if (hist[i].UploadSpeed > maxSpeed) maxSpeed = hist[i].UploadSpeed;
            }

            // Draw horizontal grid lines (4 lines)
            using (var gridPen = new Pen(Color.FromArgb(35, 255, 255, 255), 1f) { DashStyle = DashStyle.Dash })
            using (var font = new Font("Segoe UI", 7.5f))
            using (var fontBrush = new SolidBrush(Color.FromArgb(140, 140, 150)))
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = topPadding + (plotH / 4f) * i;
                    g.DrawLine(gridPen, leftPadding, y, leftPadding + plotW, y);

                    double val = maxSpeed * (1.0 - i / 4.0);
                    string label = FormatHelper.FormatSpeed(val, _settings.Unit, 0);
                    var size = g.MeasureString(label, font);
                    g.DrawString(label, font, fontBrush, leftPadding - size.Width - 6, y - size.Height / 2);
                }

                // Legend at top
                var legendFont = new Font("Segoe UI", 8.0f, FontStyle.Bold);
                using (var dlBrush = new SolidBrush(Color.FromArgb(0, 230, 118)))
                using (var upBrush = new SolidBrush(Color.FromArgb(255, 112, 67)))
                {
                    g.FillRectangle(dlBrush, leftPadding + 10, 8, 12, 8);
                    g.DrawString("Download", legendFont, fontBrush, leftPadding + 26, 6);

                    g.FillRectangle(upBrush, leftPadding + 110, 8, 12, 8);
                    g.DrawString("Upload", legendFont, fontBrush, leftPadding + 126, 6);
                }

                // X-axis time marks (last 60s, 45s, 30s, 15s, now)
                string[] times = { "-60s", "-45s", "-30s", "-15s", "0s" };
                for (int i = 0; i < 5; i++)
                {
                    float x = leftPadding + (plotW / 4f) * i;
                    string tStr = times[i];
                    var size = g.MeasureString(tStr, font);
                    g.DrawString(tStr, font, fontBrush, x - size.Width / 2, topPadding + plotH + 5);
                }
            }

            if (hist.Count < 2) return;

            // Plot curves
            int count = Math.Min(hist.Count, 60);
            int startIndex = hist.Count - count;

            PointF[] dlPoints = new PointF[count];
            PointF[] upPoints = new PointF[count];

            float xStep = (float)plotW / 59f;

            for (int i = 0; i < count; i++)
            {
                var s = hist[startIndex + i];
                // Align right: the most recent sample is at the far right
                float px = leftPadding + (59 - (count - 1 - i)) * xStep;

                float pyDl = (float)(topPadding + plotH - (s.DownloadSpeed / maxSpeed) * plotH);
                float pyUp = (float)(topPadding + plotH - (s.UploadSpeed / maxSpeed) * plotH);

                pyDl = Math.Max(topPadding, Math.Min(topPadding + plotH, pyDl));
                pyUp = Math.Max(topPadding, Math.Min(topPadding + plotH, pyUp));

                dlPoints[i] = new PointF(px, pyDl);
                upPoints[i] = new PointF(px, pyUp);
            }

            // Fill Download curve area
            using (var dlFillPath = new GraphicsPath())
            {
                dlFillPath.AddLine(dlPoints[0].X, topPadding + plotH, dlPoints[0].X, dlPoints[0].Y);
                for (int i = 1; i < count; i++) dlFillPath.AddLine(dlPoints[i - 1], dlPoints[i]);
                dlFillPath.AddLine(dlPoints[count - 1].X, dlPoints[count - 1].Y, dlPoints[count - 1].X, topPadding + plotH);
                dlFillPath.CloseFigure();

                using (var brush = new SolidBrush(Color.FromArgb(40, 0, 230, 118)))
                {
                    g.FillPath(brush, dlFillPath);
                }
            }

            // Fill Upload curve area
            using (var upFillPath = new GraphicsPath())
            {
                upFillPath.AddLine(upPoints[0].X, topPadding + plotH, upPoints[0].X, upPoints[0].Y);
                for (int i = 1; i < count; i++) upFillPath.AddLine(upPoints[i - 1], upPoints[i]);
                upFillPath.AddLine(upPoints[count - 1].X, upPoints[count - 1].Y, upPoints[count - 1].X, topPadding + plotH);
                upFillPath.CloseFigure();

                using (var brush = new SolidBrush(Color.FromArgb(40, 255, 112, 67)))
                {
                    g.FillPath(brush, upFillPath);
                }
            }

            // Draw lines
            using (var dlPen = new Pen(Color.FromArgb(0, 230, 118), 2.0f))
            {
                g.DrawLines(dlPen, dlPoints);
            }
            using (var upPen = new Pen(Color.FromArgb(255, 112, 67), 2.0f))
            {
                g.DrawLines(upPen, upPoints);
            }
        }

        private static Icon CreateFormIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var b = new SolidBrush(Color.FromArgb(0, 230, 118)))
                {
                    g.FillEllipse(b, 2, 2, 28, 28);
                }
                IntPtr hIcon = bmp.GetHicon();
                Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
                NativeMethods.DestroyIcon(hIcon);
                return icon;
            }
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }
}
