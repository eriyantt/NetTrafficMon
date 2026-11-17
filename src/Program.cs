using System;
using System.Threading;
using System.Windows.Forms;

namespace NetTrafficMon
{
    internal static class Program
    {
        private static Mutex _mutex;

        [STAThread]
        private static void Main()
        {
            // Single instance protection
            bool isFirstInstance;
            _mutex = new Mutex(true, "NetTrafficMon_SingleInstance_Guid_98a72b", out isFirstInstance);

            if (!isFirstInstance)
            {
                MessageBox.Show(
                    "NetTrafficMon is already running.\nCheck your system tray (notification area) at the bottom-right of your screen.",
                    "NetTrafficMon",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                NativeMethods.SetProcessDPIAware();
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var settings = AppSettings.Load();
                var engine = new TrafficEngine(settings);
                var widget = new TaskbarWidget(engine, settings);
                var trayManager = new TrayIconManager(engine, settings, widget);

                widget.SetDoubleClickHandler(() => trayManager.ShowDetailsWindow());

                engine.Start();
                widget.Show();

                Application.ApplicationExit += (s, e) =>
                {
                    engine.Stop();
                    settings.Save();
                    trayManager.Dispose();
                };

                Application.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal error starting NetTrafficMon:\n" + ex.Message, "NetTrafficMon Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
            }
        }
    }
}
