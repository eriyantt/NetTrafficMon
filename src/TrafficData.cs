using System;
using System.Drawing;

namespace NetTrafficMon
{
    public enum SpeedUnit
    {
        BytesPerSecond = 0,
        BitsPerSecond = 1
    }

    public enum DisplayMode
    {
        CompactTwoLine = 0,
        SingleLine = 1,
        WithMiniGraph = 2
    }

    public enum AppTheme
    {
        Dark = 0,
        Light = 1,
        Transparent = 2
    }

    public class TrafficSample
    {
        public DateTime Timestamp { get; set; }
        public double UploadSpeed { get; set; }   // in Bytes/sec
        public double DownloadSpeed { get; set; } // in Bytes/sec
        public long TotalBytesSent { get; set; }
        public long TotalBytesReceived { get; set; }
    }

    public class AdapterInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string IPv4 { get; set; }
        public string MacAddress { get; set; }
        public long LinkSpeedBps { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? Description : Name;
        }
    }

    public static class FormatHelper
    {
        public static string FormatSpeed(double bytesPerSec, SpeedUnit unit, int decimals)
        {
            if (bytesPerSec < 0) bytesPerSec = 0;
            string format = decimals <= 0 ? "0" : ("0." + new string('0', decimals));

            if (unit == SpeedUnit.BitsPerSecond)
            {
                double bitsPerSec = bytesPerSec * 8.0;
                if (bitsPerSec < 1000)
                    return string.Format("{0:0} bps", bitsPerSec);
                if (bitsPerSec < 1000000)
                    return string.Format("{0:" + format + "} Kbps", bitsPerSec / 1000.0);
                if (bitsPerSec < 1000000000)
                    return string.Format("{0:" + format + "} Mbps", bitsPerSec / 1000000.0);
                return string.Format("{0:" + format + "} Gbps", bitsPerSec / 1000000000.0);
            }
            else
            {
                if (bytesPerSec < 1024)
                    return string.Format("{0:0} B/s", bytesPerSec);
                if (bytesPerSec < 1024 * 1024)
                    return string.Format("{0:" + format + "} KB/s", bytesPerSec / 1024.0);
                if (bytesPerSec < 1024 * 1024 * 1024)
                    return string.Format("{0:" + format + "} MB/s", bytesPerSec / (1024.0 * 1024.0));
                return string.Format("{0:" + format + "} GB/s", bytesPerSec / (1024.0 * 1024.0 * 1024.0));
            }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) bytes = 0;
            if (bytes < 1024)
                return bytes + " B";
            if (bytes < 1024 * 1024)
                return string.Format("{0:0.1} KB", bytes / 1024.0);
            if (bytes < 1024 * 1024 * 1024)
                return string.Format("{0:0.1} MB", bytes / (1024.0 * 1024.0));
            return string.Format("{0:0.2} GB", bytes / (1024.0 * 1024.0 * 1024.0));
        }

        public static Color ColorFromHex(string hex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hex)) return defaultColor;
            try
            {
                hex = hex.Trim().TrimStart('#');
                if (hex.Length == 6)
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return Color.FromArgb(r, g, b);
                }
                if (hex.Length == 8)
                {
                    int a = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int r = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(4, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
            }
            catch { }
            return defaultColor;
        }

        public static string ColorToHex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }
    }
}
