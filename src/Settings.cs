using System;
using System.IO;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace NetTrafficMon
{
    public class AppSettings
    {
        public string SelectedAdapterId { get; set; }
        public int RefreshIntervalMs { get; set; }
        public SpeedUnit Unit { get; set; }
        public DisplayMode Mode { get; set; }
        public AppTheme Theme { get; set; }
        public double Opacity { get; set; }
        public string UploadColor { get; set; }
        public string DownloadColor { get; set; }
        public string TextColor { get; set; }
        public string BackgroundColor { get; set; }
        public bool DockToTaskbar { get; set; }
        public bool LockPosition { get; set; }
        public int WidgetX { get; set; }
        public int WidgetY { get; set; }
        public bool StartWithWindows { get; set; }
        public int DecimalPlaces { get; set; }
        public bool ShowUnit { get; set; }

        public AppSettings()
        {
            SelectedAdapterId = "auto";
            RefreshIntervalMs = 1000;
            Unit = SpeedUnit.BytesPerSecond;
            Mode = DisplayMode.CompactTwoLine;
            Theme = AppTheme.Dark;
            Opacity = 0.95;
            UploadColor = "#FF7043";     // Vibrant Orange
            DownloadColor = "#00E676";   // Vivid Green
            TextColor = "#FFFFFF";       // Crisp White
            BackgroundColor = "#1E1E22"; // Fluent Dark
            DockToTaskbar = true;
            LockPosition = false;
            WidgetX = -1;
            WidgetY = -1;
            StartWithWindows = false;
            DecimalPlaces = 1;
            ShowUnit = true;
        }

        private static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "NetTrafficMon");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "settings.json");
        }

        public static AppSettings Load()
        {
            string path = GetConfigPath();
            // Also check local directory
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            if (File.Exists(localPath)) path = localPath;

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var serializer = new JavaScriptSerializer();
                    var settings = serializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        settings.StartWithWindows = IsRunOnStartupEnabled();
                        return settings;
                    }
                }
                catch { }
            }

            var def = new AppSettings();
            def.StartWithWindows = IsRunOnStartupEnabled();
            return def;
        }

        public void Save()
        {
            try
            {
                string path = GetConfigPath();
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(this);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "NetTrafficMon";

        public static bool IsRunOnStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(AppRegistryName);
                        return val != null;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetRunOnStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = System.Windows.Forms.Application.ExecutablePath;
                            key.SetValue(AppRegistryName, "\"" + exePath + "\"");
                        }
                        else
                        {
                            if (key.GetValue(AppRegistryName) != null)
                            {
                                key.DeleteValue(AppRegistryName, false);
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
