using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;

namespace NetTrafficMon
{
    public class TrafficEngine
    {
        private readonly Timer _timer;
        private readonly Stopwatch _stopwatch;
        private long _prevBytesSent;
        private long _prevBytesReceived;
        private bool _isFirstTick = true;

        public AppSettings Settings { get; private set; }
        public List<TrafficSample> History { get; private set; }
        public const int MaxHistoryCount = 60;

        public double CurrentUploadSpeed { get; private set; }
        public double CurrentDownloadSpeed { get; private set; }
        public double PeakUploadSpeed { get; private set; }
        public double PeakDownloadSpeed { get; private set; }
        public long SessionBytesSent { get; private set; }
        public long SessionBytesReceived { get; private set; }
        public DateTime StartTime { get; private set; }

        public string ActiveAdapterName { get; private set; }
        public string ActiveAdapterIPv4 { get; private set; }
        public string ActiveAdapterMac { get; private set; }
        public string ActiveAdapterDescription { get; private set; }
        public long ActiveAdapterSpeedBps { get; private set; }

        public event Action<TrafficSample> TrafficUpdated;

        public TrafficEngine(AppSettings settings)
        {
            Settings = settings;
            History = new List<TrafficSample>();
            StartTime = DateTime.Now;

            _stopwatch = new Stopwatch();
            _timer = new Timer();
            _timer.Interval = Math.Max(250, Settings.RefreshIntervalMs);
            _timer.Tick += OnTimerTick;

            UpdateAdapterMetadata();
        }

        public void Start()
        {
            _stopwatch.Restart();
            _timer.Start();
            PollTraffic(); // Initial poll
        }

        public void Stop()
        {
            _timer.Stop();
            _stopwatch.Stop();
        }

        public void UpdateInterval(int intervalMs)
        {
            Settings.RefreshIntervalMs = intervalMs;
            _timer.Interval = Math.Max(250, intervalMs);
        }

        public void ResetSessionStats()
        {
            SessionBytesSent = 0;
            SessionBytesReceived = 0;
            PeakUploadSpeed = 0;
            PeakDownloadSpeed = 0;
            History.Clear();
            StartTime = DateTime.Now;
        }

        public List<AdapterInfo> GetAvailableAdapters()
        {
            var list = new List<AdapterInfo>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    string ip = "N/A";
                    try
                    {
                        var ipProps = ni.GetIPProperties();
                        foreach (var u in ipProps.UnicastAddresses)
                        {
                            if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                ip = u.Address.ToString();
                                break;
                            }
                        }
                    }
                    catch { }

                    string mac = "N/A";
                    try
                    {
                        var bytes = ni.GetPhysicalAddress().GetAddressBytes();
                        if (bytes.Length > 0)
                        {
                            mac = string.Join(":", bytes.Select(b => b.ToString("X2")).ToArray());
                        }
                    }
                    catch { }

                    list.Add(new AdapterInfo
                    {
                        Id = ni.Id,
                        Name = ni.Name,
                        Description = ni.Description,
                        Type = ni.NetworkInterfaceType.ToString(),
                        Status = ni.OperationalStatus.ToString(),
                        IPv4 = ip,
                        MacAddress = mac,
                        LinkSpeedBps = ni.Speed
                    });
                }
            }
            catch { }
            return list;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            PollTraffic();
        }

        private void PollTraffic()
        {
            double elapsedSec = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            long totalSent = 0;
            long totalReceived = 0;
            NetworkInterface primaryNi = null;
            long highestTraffic = -1;

            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    // Filter if specific adapter selected
                    if (!string.IsNullOrEmpty(Settings.SelectedAdapterId) &&
                        Settings.SelectedAdapterId != "auto" &&
                        ni.Id != Settings.SelectedAdapterId)
                    {
                        continue;
                    }

                    try
                    {
                        var stats = ni.GetIPStatistics();
                        long inBytes = stats.BytesReceived;
                        long outBytes = stats.BytesSent;

                        totalSent += outBytes;
                        totalReceived += inBytes;

                        long sum = inBytes + outBytes;
                        if (sum > highestTraffic)
                        {
                            highestTraffic = sum;
                            primaryNi = ni;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Update primary adapter info
            if (primaryNi != null)
            {
                ActiveAdapterName = primaryNi.Name;
                ActiveAdapterDescription = primaryNi.Description;
                ActiveAdapterSpeedBps = primaryNi.Speed;

                try
                {
                    foreach (var u in primaryNi.GetIPProperties().UnicastAddresses)
                    {
                        if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ActiveAdapterIPv4 = u.Address.ToString();
                            break;
                        }
                    }
                }
                catch { }

                try
                {
                    var bytes = primaryNi.GetPhysicalAddress().GetAddressBytes();
                    if (bytes.Length > 0)
                    {
                        ActiveAdapterMac = string.Join(":", bytes.Select(b => b.ToString("X2")).ToArray());
                    }
                }
                catch { }
            }

            if (_isFirstTick)
            {
                _prevBytesSent = totalSent;
                _prevBytesReceived = totalReceived;
                _isFirstTick = false;
                return;
            }

            // Normal delta calculation
            long deltaSent = totalSent - _prevBytesSent;
            long deltaReceived = totalReceived - _prevBytesReceived;

            _prevBytesSent = totalSent;
            _prevBytesReceived = totalReceived;

            // Guard against system sleep or counter overflow anomalies
            if (deltaSent < 0 || deltaReceived < 0 || elapsedSec <= 0 || elapsedSec > 10.0)
            {
                return;
            }

            CurrentUploadSpeed = deltaSent / elapsedSec;
            CurrentDownloadSpeed = deltaReceived / elapsedSec;

            if (CurrentUploadSpeed > PeakUploadSpeed) PeakUploadSpeed = CurrentUploadSpeed;
            if (CurrentDownloadSpeed > PeakDownloadSpeed) PeakDownloadSpeed = CurrentDownloadSpeed;

            SessionBytesSent += deltaSent;
            SessionBytesReceived += deltaReceived;

            var sample = new TrafficSample
            {
                Timestamp = DateTime.Now,
                UploadSpeed = CurrentUploadSpeed,
                DownloadSpeed = CurrentDownloadSpeed,
                TotalBytesSent = SessionBytesSent,
                TotalBytesReceived = SessionBytesReceived
            };

            History.Add(sample);
            if (History.Count > MaxHistoryCount)
            {
                History.RemoveAt(0);
            }

            if (TrafficUpdated != null)
            {
                TrafficUpdated(sample);
            }
        }

        public void UpdateAdapterMetadata()
        {
            var adapters = GetAvailableAdapters();
            var active = adapters.FirstOrDefault(a => a.Status == "Up");
            if (active != null)
            {
                ActiveAdapterName = active.Name;
                ActiveAdapterDescription = active.Description;
                ActiveAdapterIPv4 = active.IPv4;
                ActiveAdapterMac = active.MacAddress;
                ActiveAdapterSpeedBps = active.LinkSpeedBps;
            }
            else
            {
                ActiveAdapterName = "No Active Network";
                ActiveAdapterDescription = "";
                ActiveAdapterIPv4 = "127.0.0.1";
                ActiveAdapterMac = "";
                ActiveAdapterSpeedBps = 0;
            }
        }
    }
}
