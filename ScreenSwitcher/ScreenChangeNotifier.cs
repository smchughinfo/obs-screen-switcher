using System.Runtime.InteropServices;

namespace ScreenSwitcher;

public class MonitorInfo
{
    public IntPtr Handle { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string DeviceName { get; set; } = "";
    public bool IsPrimary { get; set; }
    public int DisplayNumber { get; set; }

    public override string ToString()
    {
        var primary = IsPrimary ? " [PRIMARY]" : "";
        return $"Display {DisplayNumber}{primary} ({DeviceName}) - Position: ({Left}, {Top}), Size: {Width}x{Height}";
    }
}

public class ScreenChangeNotifier
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public uint Size;
        public RECT Monitor;
        public RECT WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DISPLAY_DEVICE
    {
        public uint cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const uint MONITORINFOF_PRIMARY = 1;
    private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x1;

    private static Dictionary<string, int>? _deviceToDisplayNumber;
    private static readonly object _initLock = new object();

    public event Action<MonitorInfo>? ScreenChanged;

    private IntPtr _currentMonitorHandle;
    private bool _running;

    public async Task StartAsync(int pollIntervalMs = 100)
    {
        _running = true;
        _currentMonitorHandle = GetCurrentMonitorHandle();

        while (_running)
        {
            await Task.Delay(pollIntervalMs);

            var newMonitorHandle = GetCurrentMonitorHandle();
            if (newMonitorHandle != _currentMonitorHandle)
            {
                _currentMonitorHandle = newMonitorHandle;
                var monitorInfo = GetMonitorInfoFromHandle(_currentMonitorHandle);
                ScreenChanged?.Invoke(monitorInfo);
            }
        }
    }

    public void Stop()
    {
        _running = false;
    }

    private IntPtr GetCurrentMonitorHandle()
    {
        GetCursorPos(out POINT point);
        return MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
    }

    private void InitializeDisplayMapping()
    {
        if (_deviceToDisplayNumber != null) return;

        lock (_initLock)
        {
            if (_deviceToDisplayNumber != null) return;

            _deviceToDisplayNumber = new Dictionary<string, int>();
            uint displayNum = 0;
            int logicalIndex = 1;

            while (true)
            {
                var device = new DISPLAY_DEVICE();
                device.cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE));

                if (!EnumDisplayDevices(null, displayNum, ref device, 0))
                    break;

                if ((device.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
                {
                    _deviceToDisplayNumber[device.DeviceName] = logicalIndex;
                    logicalIndex++;
                }

                displayNum++;
            }
        }
    }

    private MonitorInfo GetMonitorInfoFromHandle(IntPtr hMonitor)
    {
        InitializeDisplayMapping();

        var mi = new MONITORINFOEX();
        mi.Size = (uint)Marshal.SizeOf(typeof(MONITORINFOEX));

        if (!GetMonitorInfo(hMonitor, ref mi))
        {
            throw new Exception($"GetMonitorInfo failed for handle {hMonitor}");
        }

        var displayNumber = _deviceToDisplayNumber!.TryGetValue(mi.DeviceName, out var num) ? num : 0;

        return new MonitorInfo
        {
            Handle = hMonitor,
            Left = mi.Monitor.Left,
            Top = mi.Monitor.Top,
            Width = mi.Monitor.Right - mi.Monitor.Left,
            Height = mi.Monitor.Bottom - mi.Monitor.Top,
            DeviceName = mi.DeviceName,
            IsPrimary = (mi.Flags & MONITORINFOF_PRIMARY) != 0,
            DisplayNumber = displayNumber
        };
    }
}
