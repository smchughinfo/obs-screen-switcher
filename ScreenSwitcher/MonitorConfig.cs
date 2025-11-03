namespace ScreenSwitcher;

public class MonitorConfig
{
    public Dictionary<IntPtr, string> MonitorToSourceMap { get; } = new();

    public void AddMapping(IntPtr monitorHandle, string displayName)
    {
        MonitorToSourceMap[monitorHandle] = $"T-{displayName}";
    }

    public IEnumerable<string> GetAllToggleSources()
    {
        return MonitorToSourceMap.Values;
    }

    public string? GetSourceForMonitor(IntPtr monitorHandle)
    {
        return MonitorToSourceMap.TryGetValue(monitorHandle, out var source) ? source : null;
    }
}
