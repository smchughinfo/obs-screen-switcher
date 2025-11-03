using System.Runtime.InteropServices;

namespace ScreenSwitcher;

public class ScreenChangeNotifier
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    public event Action<IntPtr>? ScreenChanged;

    private IntPtr _currentMonitor;
    private bool _running;

    public async Task StartAsync(int pollIntervalMs = 100)
    {
        _running = true;
        _currentMonitor = GetCurrentMonitor();

        while (_running)
        {
            await Task.Delay(pollIntervalMs);

            var newMonitor = GetCurrentMonitor();
            if (newMonitor != _currentMonitor)
            {
                _currentMonitor = newMonitor;
                ScreenChanged?.Invoke(_currentMonitor);
            }
        }
    }

    public void Stop()
    {
        _running = false;
    }

    private IntPtr GetCurrentMonitor()
    {
        GetCursorPos(out POINT point);
        return MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
    }
}
