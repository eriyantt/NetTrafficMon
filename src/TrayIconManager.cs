using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NetTrafficMon
{
    public class TrayIconManager : IDisposable
    {
        private readonly TrafficEngine _engine;
        private readonly AppSettings _settings;
        private readonly TaskbarWidget _widget;

        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private DetailsForm _detailsForm;

        public ContextMenuStrip ContextMenu { get { return _contextMenu; } }

        public TrayIconManager(TrafficEngine engine, AppSettings settings, TaskbarWidget widget)
        {
            _engine = engine;
            _settings = settings;
            _widget = widget;

            InitializeContextMenu();
            InitializeTrayIcon();

            _engine.TrafficUpdated += OnTrafficUpdated;
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateAppIcon(),
                Text = "NetTrafficMon - Realtime Traffic Monitor",
                Visible = true,
                ContextMenuStrip = _contextMenu
            };

            _notifyIcon.DoubleClick += (s, e) => ShowDetailsWindow();
            _notifyIcon.Click += (s, e) =>
            {
                var me = e as MouseEventArgs;
                if (me != null && me.Button == MouseButtons.Left)
                {
                    if (_widget.Visible)
                    {
                        _widget.BringToFront();
                    }
                    else
                    {
                        _widget.Show();
                    }
                }
            };
        }

        public void InitializeContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.RenderMode = ToolStripRenderMode.System;

            // Title
            var titleItem = new ToolStripMenuItem("NetTrafficMon v1.0") { Enabled = false, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _contextMenu.Items.Add(titleItem);
            _contextMenu.Items.Add(new ToolStripSeparator());

            // Details & Graph
            var detailsItem = new ToolStripMenuItem("Network Details & Live Graph...", null, (s, e) => ShowDetailsWindow());
            detailsItem.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            _contextMenu.Items.Add(detailsItem);
            _contextMenu.Items.Add(new ToolStripSeparator());

            // Adapter submenu
            var adapterMenu = new ToolStripMenuItem("Network Adapter");
            _contextMenu.Items.Add(adapterMenu);
            BuildAdapterSubmenu(adapterMenu);

            // Display Mode submenu
            var modeMenu = new ToolStripMenuItem("Display Layout");
            var itemModeCompact = new ToolStripMenuItem("Compact 2-Line (Stacked)", null, (s, e) => SetDisplayMode(DisplayMode.CompactTwoLine));
            var itemModeSingle = new ToolStripMenuItem("Horizontal Single Line", null, (s, e) => SetDisplayMode(DisplayMode.SingleLine));
            var itemModeGraph = new ToolStripMenuItem("With Mini Graph (Sparkline)", null, (s, e) => SetDisplayMode(DisplayMode.WithMiniGraph));

            itemModeCompact.Checked = _settings.Mode == DisplayMode.CompactTwoLine;
            itemModeSingle.Checked = _settings.Mode == DisplayMode.SingleLine;
            itemModeGraph.Checked = _settings.Mode == DisplayMode.WithMiniGraph;

            modeMenu.DropDownItems.AddRange(new ToolStripItem[] { itemModeCompact, itemModeSingle, itemModeGraph });
            _contextMenu.Items.Add(modeMenu);

            // Speed Unit submenu
            var unitMenu = new ToolStripMenuItem("Speed Unit");
            var itemUnitBytes = new ToolStripMenuItem("Bytes/s (KB/s, MB/s)", null, (s, e) => SetSpeedUnit(SpeedUnit.BytesPerSecond));
            var itemUnitBits = new ToolStripMenuItem("Bits/s (Kbps, Mbps)", null, (s, e) => SetSpeedUnit(SpeedUnit.BitsPerSecond));
            itemUnitBytes.Checked = _settings.Unit == SpeedUnit.BytesPerSecond;
            itemUnitBits.Checked = _settings.Unit == SpeedUnit.BitsPerSecond;
            unitMenu.DropDownItems.AddRange(new ToolStripItem[] { itemUnitBytes, itemUnitBits });
            _contextMenu.Items.Add(unitMenu);

            // Theme submenu
            var themeMenu = new ToolStripMenuItem("Theme");
            var itemThemeDark = new ToolStripMenuItem("Dark Fluent", null, (s, e) => SetTheme(AppTheme.Dark));
            var itemThemeLight = new ToolStripMenuItem("Light", null, (s, e) => SetTheme(AppTheme.Light));
            var itemThemeTrans = new ToolStripMenuItem("Transparent", null, (s, e) => SetTheme(AppTheme.Transparent));
            itemThemeDark.Checked = _settings.Theme == AppTheme.Dark;
            itemThemeLight.Checked = _settings.Theme == AppTheme.Light;
            itemThemeTrans.Checked = _settings.Theme == AppTheme.Transparent;
            themeMenu.DropDownItems.AddRange(new ToolStripItem[] { itemThemeDark, itemThemeLight, itemThemeTrans });
            _contextMenu.Items.Add(themeMenu);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Dock to Taskbar
            var dockItem = new ToolStripMenuItem("Dock to Taskbar", null, (s, e) =>
            {
                _widget.DockToTaskbarPosition();
                _settings.DockToTaskbar = true;
                _settings.Save();
                InitializeContextMenu();
            })
            { Checked = _settings.DockToTaskbar };
            _contextMenu.Items.Add(dockItem);

            // Lock Position
            var lockItem = new ToolStripMenuItem("Lock Position", null, (s, e) =>
            {
                _settings.LockPosition = !_settings.LockPosition;
                _settings.Save();
                InitializeContextMenu();
            })
            { Checked = _settings.LockPosition };
            _contextMenu.Items.Add(lockItem);

            // Reset Position
            var resetPosItem = new ToolStripMenuItem("Reset Widget to Taskbar Position", null, (s, e) =>
            {
                _widget.DockToTaskbarPosition();
            });
            _contextMenu.Items.Add(resetPosItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Settings...
            var settingsItem = new ToolStripMenuItem("Settings...", null, (s, e) => ShowSettingsWindow());
            _contextMenu.Items.Add(settingsItem);

            // Run at startup
            var startupItem = new ToolStripMenuItem("Launch on Windows Startup", null, (s, e) =>
            {
                bool newState = !AppSettings.IsRunOnStartupEnabled();
                AppSettings.SetRunOnStartup(newState);
                _settings.StartWithWindows = newState;
                _settings.Save();
                InitializeContextMenu();
            })
            { Checked = AppSettings.IsRunOnStartupEnabled() };
            _contextMenu.Items.Add(startupItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Exit
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit());
            _contextMenu.Items.Add(exitItem);

            _widget.SetContextMenu(_contextMenu);
        }

        private void BuildAdapterSubmenu(ToolStripMenuItem parent)
        {
            parent.DropDownItems.Clear();

            var autoItem = new ToolStripMenuItem("All Active Adapters (Combined)", null, (s, e) =>
            {
                _settings.SelectedAdapterId = "auto";
                _settings.Save();
                _engine.UpdateAdapterMetadata();
                InitializeContextMenu();
            })
            { Checked = string.IsNullOrEmpty(_settings.SelectedAdapterId) || _settings.SelectedAdapterId == "auto" };
            parent.DropDownItems.Add(autoItem);
            parent.DropDownItems.Add(new ToolStripSeparator());

            var adapters = _engine.GetAvailableAdapters();
            foreach (var a in adapters)
            {
                string text = a.Name + " (" + a.IPv4 + ")";
                string id = a.Id;
                var item = new ToolStripMenuItem(text, null, (s, e) =>
                {
                    _settings.SelectedAdapterId = id;
                    _settings.Save();
                    _engine.UpdateAdapterMetadata();
                    InitializeContextMenu();
                })
                { Checked = _settings.SelectedAdapterId == id };
                parent.DropDownItems.Add(item);
            }
        }

        private void SetDisplayMode(DisplayMode mode)
        {
            _settings.Mode = mode;
            _settings.Save();
            _widget.ApplyThemeAndLayout();
            InitializeContextMenu();
        }

        private void SetSpeedUnit(SpeedUnit unit)
        {
            _settings.Unit = unit;
            _settings.Save();
            _widget.RenderLayeredWindow();
            InitializeContextMenu();
        }

        private void SetTheme(AppTheme theme)
        {
            _settings.Theme = theme;
            _settings.Save();
            _widget.ApplyThemeAndLayout();
            InitializeContextMenu();
        }

        public void ShowDetailsWindow()
        {
            if (_detailsForm == null || _detailsForm.IsDisposed)
            {
                _detailsForm = new DetailsForm(_engine, _settings);
                _detailsForm.Show();
            }
            else
            {
                _detailsForm.BringToFront();
                _detailsForm.Activate();
            }
        }

        public void ShowSettingsWindow()
        {
            using (var sf = new SettingsForm(_engine, _settings, () =>
            {
                _widget.ApplyThemeAndLayout();
                InitializeContextMenu();
            }))
            {
                sf.ShowDialog();
            }
        }

        private void OnTrafficUpdated(TrafficSample sample)
        {
            if (_notifyIcon == null) return;

            string upText = FormatHelper.FormatSpeed(_engine.CurrentUploadSpeed, _settings.Unit, 1);
            string downText = FormatHelper.FormatSpeed(_engine.CurrentDownloadSpeed, _settings.Unit, 1);

            // Windows notify icon text limit is 63 chars (Win7/10 limit)
            string tip = string.Format("NetTrafficMon\n▲ {0} | ▼ {1}", upText, downText);
            if (tip.Length >= 64) tip = tip.Substring(0, 63);

            _notifyIcon.Text = tip;
        }

        private static Icon CreateAppIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Background circle
                using (var bgBrush = new SolidBrush(Color.FromArgb(24, 24, 28)))
                {
                    g.FillEllipse(bgBrush, 1, 1, 30, 30);
                }
                using (var pen = new Pen(Color.FromArgb(80, 80, 95), 1.5f))
                {
                    g.DrawEllipse(pen, 1, 1, 30, 30);
                }

                // Up arrow (Orange)
                using (var upBrush = new SolidBrush(Color.FromArgb(255, 112, 67)))
                {
                    Point[] upArrow = new Point[] {
                        new Point(11, 7),
                        new Point(7, 13),
                        new Point(10, 13),
                        new Point(10, 22),
                        new Point(12, 22),
                        new Point(12, 13),
                        new Point(15, 13)
                    };
                    g.FillPolygon(upBrush, upArrow);
                }

                // Down arrow (Green)
                using (var downBrush = new SolidBrush(Color.FromArgb(0, 230, 118)))
                {
                    Point[] downArrow = new Point[] {
                        new Point(21, 25),
                        new Point(17, 19),
                        new Point(20, 19),
                        new Point(20, 10),
                        new Point(22, 10),
                        new Point(22, 19),
                        new Point(25, 19)
                    };
                    g.FillPolygon(downBrush, downArrow);
                }

                IntPtr hIcon = bmp.GetHicon();
                Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
                NativeMethods.DestroyIcon(hIcon);
                return icon;
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            if (_contextMenu != null)
            {
                _contextMenu.Dispose();
                _contextMenu = null;
            }
        }
    }
}
