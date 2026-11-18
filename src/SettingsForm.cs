using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetTrafficMon
{
    public class SettingsForm : Form
    {
        private readonly TrafficEngine _engine;
        private readonly AppSettings _settings;
        private readonly Action _onSettingsApplied;

        private ComboBox _cmbAdapters;
        private ComboBox _cmbDisplayMode;
        private ComboBox _cmbSpeedUnit;
        private ComboBox _cmbRefreshRate;
        private NumericUpDown _numDecimals;
        private ComboBox _cmbTheme;
        private TrackBar _trackOpacity;
        private Label _lblOpacityVal;

        private Button _btnUploadColor;
        private Button _btnDownloadColor;
        private Color _currentUploadColor;
        private Color _currentDownloadColor;

        private CheckBox _chkDockTaskbar;
        private CheckBox _chkAutoSmallTaskbar;
        private CheckBox _chkLockPosition;
        private CheckBox _chkStartup;

        public SettingsForm(TrafficEngine engine, AppSettings settings, Action onSettingsApplied)
        {
            _engine = engine;
            _settings = settings;
            _onSettingsApplied = onSettingsApplied;

            _currentUploadColor = FormatHelper.ColorFromHex(_settings.UploadColor, Color.FromArgb(255, 112, 67));
            _currentDownloadColor = FormatHelper.ColorFromHex(_settings.DownloadColor, Color.FromArgb(0, 230, 118));

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "NetTrafficMon - Settings";
            Size = new Size(460, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(28, 28, 32);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.0f);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(16)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Tab 1: General & Network
            var tabGeneral = new TabPage("General & Network") { BackColor = Color.FromArgb(28, 28, 32) };
            PopulateGeneralTab(tabGeneral);
            tabControl.TabPages.Add(tabGeneral);

            // Tab 2: Appearance & Style
            var tabAppearance = new TabPage("Appearance & Colors") { BackColor = Color.FromArgb(28, 28, 32) };
            PopulateAppearanceTab(tabAppearance);
            tabControl.TabPages.Add(tabAppearance);

            mainPanel.Controls.Add(tabControl, 0, 0);

            // Bottom Buttons
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0)
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Width = 85,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 52),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => Close();

            var btnSave = new Button
            {
                Text = "Save & Apply",
                Width = 110,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 160, 90),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += OnSaveClicked;

            var btnReset = new Button
            {
                Text = "Defaults",
                Width = 85,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 52),
                ForeColor = Color.FromArgb(190, 190, 200),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += OnResetDefaults;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnReset);

            mainPanel.Controls.Add(btnPanel, 0, 1);
            Controls.Add(mainPanel);
        }

        private void PopulateGeneralTab(TabPage tab)
        {
            var p = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(12)
            };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

            int r = 0;

            // Network Adapter
            p.Controls.Add(new Label { Text = "Network Adapter:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            _cmbAdapters = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
            _cmbAdapters.Items.Add("All Active Adapters (Combined)");
            var adapters = _engine.GetAvailableAdapters();
            int selIdx = 0;
            for (int i = 0; i < adapters.Count; i++)
            {
                _cmbAdapters.Items.Add(adapters[i].Name + " (" + adapters[i].Description + ")");
                if (adapters[i].Id == _settings.SelectedAdapterId) selIdx = i + 1;
            }
            _cmbAdapters.SelectedIndex = selIdx;
            p.Controls.Add(_cmbAdapters, 1, r++);

            // Speed Unit
            p.Controls.Add(new Label { Text = "Speed Unit:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            _cmbSpeedUnit = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
            _cmbSpeedUnit.Items.Add("Bytes/s (B/s, KB/s, MB/s)");
            _cmbSpeedUnit.Items.Add("Bits/s (bps, Kbps, Mbps)");
            _cmbSpeedUnit.SelectedIndex = (int)_settings.Unit;
            p.Controls.Add(_cmbSpeedUnit, 1, r++);

            // Refresh Interval
            p.Controls.Add(new Label { Text = "Refresh Rate:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            _cmbRefreshRate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
            _cmbRefreshRate.Items.Add("Fast (500 ms)");
            _cmbRefreshRate.Items.Add("Normal (1000 ms)");
            _cmbRefreshRate.Items.Add("Power Saving (2000 ms)");
            if (_settings.RefreshIntervalMs <= 500) _cmbRefreshRate.SelectedIndex = 0;
            else if (_settings.RefreshIntervalMs <= 1000) _cmbRefreshRate.SelectedIndex = 1;
            else _cmbRefreshRate.SelectedIndex = 2;
            p.Controls.Add(_cmbRefreshRate, 1, r++);

            // Decimal places
            p.Controls.Add(new Label { Text = "Decimal Precision:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            _numDecimals = new NumericUpDown { Minimum = 0, Maximum = 2, Value = _settings.DecimalPlaces, Width = 80 };
            p.Controls.Add(_numDecimals, 1, r++);

            // Dock to Taskbar
            _chkDockTaskbar = new CheckBox
            {
                Text = "Dock automatically to Windows Taskbar",
                Checked = _settings.DockToTaskbar,
                AutoSize = true,
                ForeColor = Color.White
            };
            p.SetColumnSpan(_chkDockTaskbar, 2);
            p.Controls.Add(_chkDockTaskbar, 0, r++);

            // Auto Small Taskbar
            _chkAutoSmallTaskbar = new CheckBox
            {
                Text = "Auto horizontal layout on small taskbar icons",
                Checked = _settings.AutoSmallTaskbar,
                AutoSize = true,
                ForeColor = Color.White
            };
            p.SetColumnSpan(_chkAutoSmallTaskbar, 2);
            p.Controls.Add(_chkAutoSmallTaskbar, 0, r++);

            // Lock Position
            _chkLockPosition = new CheckBox
            {
                Text = "Lock position (disable drag to prevent moving)",
                Checked = _settings.LockPosition,
                AutoSize = true,
                ForeColor = Color.White
            };
            p.SetColumnSpan(_chkLockPosition, 2);
            p.Controls.Add(_chkLockPosition, 0, r++);

            // Run at startup
            _chkStartup = new CheckBox
            {
                Text = "Launch automatically on Windows startup",
                Checked = AppSettings.IsRunOnStartupEnabled(),
                AutoSize = true,
                ForeColor = Color.White
            };
            p.SetColumnSpan(_chkStartup, 2);
            p.Controls.Add(_chkStartup, 0, r++);

            tab.Controls.Add(p);
        }

        private void PopulateAppearanceTab(TabPage tab)
        {
            var p = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(12)
            };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

            int r = 0;

            // Display Mode
            p.Controls.Add(new Label { Text = "Layout Style:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            _cmbDisplayMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
            _cmbDisplayMode.Items.Add("Compact 2-Line (Stacked)");
            _cmbDisplayMode.Items.Add("Horizontal Single Line");
            _cmbDisplayMode.Items.Add("With Mini Graph (Sparkline)");
            _cmbDisplayMode.SelectedIndex = (int)_settings.Mode;
            p.Controls.Add(_cmbDisplayMode, 1, r++);

            // Theme
            p.Controls.Add(new Label { Text = "Theme:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            _cmbTheme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
            _cmbTheme.Items.Add("Dark (Windows 10/11 Fluent)");
            _cmbTheme.Items.Add("Light");
            _cmbTheme.Items.Add("Transparent (Translucent)");
            _cmbTheme.SelectedIndex = (int)_settings.Theme;
            p.Controls.Add(_cmbTheme, 1, r++);

            // Opacity
            p.Controls.Add(new Label { Text = "Widget Opacity:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            var opacityBox = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            _trackOpacity = new TrackBar { Minimum = 30, Maximum = 100, Value = (int)(_settings.Opacity * 100), TickFrequency = 10, Width = 140 };
            _lblOpacityVal = new Label { Text = _trackOpacity.Value + "%", AutoSize = true, Margin = new Padding(4, 6, 0, 0), ForeColor = Color.White };
            _trackOpacity.Scroll += (s, e) => _lblOpacityVal.Text = _trackOpacity.Value + "%";
            opacityBox.Controls.Add(_trackOpacity);
            opacityBox.Controls.Add(_lblOpacityVal);
            p.Controls.Add(opacityBox, 1, r++);

            // Upload Color
            p.Controls.Add(new Label { Text = "Upload Arrow & Accent:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            _btnUploadColor = new Button { Width = 60, Height = 26, FlatStyle = FlatStyle.Flat, BackColor = _currentUploadColor, Cursor = Cursors.Hand };
            _btnUploadColor.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = _currentUploadColor })
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        _currentUploadColor = cd.Color;
                        _btnUploadColor.BackColor = cd.Color;
                    }
                }
            };
            p.Controls.Add(_btnUploadColor, 1, r++);

            // Download Color
            p.Controls.Add(new Label { Text = "Download Arrow & Accent:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White }, 0, r);
            _btnDownloadColor = new Button { Width = 60, Height = 26, FlatStyle = FlatStyle.Flat, BackColor = _currentDownloadColor, Cursor = Cursors.Hand };
            _btnDownloadColor.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = _currentDownloadColor })
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        _currentDownloadColor = cd.Color;
                        _btnDownloadColor.BackColor = cd.Color;
                    }
                }
            };
            p.Controls.Add(_btnDownloadColor, 1, r++);

            tab.Controls.Add(p);
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            // Adapter
            if (_cmbAdapters.SelectedIndex <= 0)
            {
                _settings.SelectedAdapterId = "auto";
            }
            else
            {
                var adapters = _engine.GetAvailableAdapters();
                int idx = _cmbAdapters.SelectedIndex - 1;
                if (idx >= 0 && idx < adapters.Count)
                {
                    _settings.SelectedAdapterId = adapters[idx].Id;
                }
            }

            _settings.Unit = (SpeedUnit)_cmbSpeedUnit.SelectedIndex;

            if (_cmbRefreshRate.SelectedIndex == 0) _settings.RefreshIntervalMs = 500;
            else if (_cmbRefreshRate.SelectedIndex == 1) _settings.RefreshIntervalMs = 1000;
            else _settings.RefreshIntervalMs = 2000;
            _engine.UpdateInterval(_settings.RefreshIntervalMs);

            _settings.DecimalPlaces = (int)_numDecimals.Value;
            _settings.DockToTaskbar = _chkDockTaskbar.Checked;
            _settings.AutoSmallTaskbar = _chkAutoSmallTaskbar.Checked;
            _settings.LockPosition = _chkLockPosition.Checked;

            _settings.Mode = (DisplayMode)_cmbDisplayMode.SelectedIndex;
            _settings.Theme = (AppTheme)_cmbTheme.SelectedIndex;
            _settings.Opacity = _trackOpacity.Value / 100.0;

            _settings.UploadColor = FormatHelper.ColorToHex(_currentUploadColor);
            _settings.DownloadColor = FormatHelper.ColorToHex(_currentDownloadColor);

            // Startup
            AppSettings.SetRunOnStartup(_chkStartup.Checked);
            _settings.StartWithWindows = _chkStartup.Checked;

            _settings.Save();

            if (_onSettingsApplied != null)
            {
                _onSettingsApplied();
            }

            Close();
        }

        private void OnResetDefaults(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset all settings to default values?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var def = new AppSettings();
                _currentUploadColor = FormatHelper.ColorFromHex(def.UploadColor, Color.FromArgb(255, 112, 67));
                _currentDownloadColor = FormatHelper.ColorFromHex(def.DownloadColor, Color.FromArgb(0, 230, 118));
                _btnUploadColor.BackColor = _currentUploadColor;
                _btnDownloadColor.BackColor = _currentDownloadColor;

                _cmbAdapters.SelectedIndex = 0;
                _cmbSpeedUnit.SelectedIndex = (int)def.Unit;
                _cmbRefreshRate.SelectedIndex = 1;
                _numDecimals.Value = def.DecimalPlaces;
                _chkDockTaskbar.Checked = def.DockToTaskbar;
                _chkAutoSmallTaskbar.Checked = def.AutoSmallTaskbar;
                _chkLockPosition.Checked = def.LockPosition;
                _cmbDisplayMode.SelectedIndex = (int)def.Mode;
                _cmbTheme.SelectedIndex = (int)def.Theme;
                _trackOpacity.Value = (int)(def.Opacity * 100);
                _lblOpacityVal.Text = _trackOpacity.Value + "%";
            }
        }
    }
}
