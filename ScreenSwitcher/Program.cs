namespace ScreenSwitcher;

class Program
{
    static async Task Main(string[] args)
    {
        var passwordFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "obs-password.txt");
        var password = File.ReadAllText(passwordFile).Trim();

        var obs = new OBSService();
        await obs.ConnectAsync("ws://localhost:4455", password);

        var currentScene = obs.GetCurrentScene();
        var sceneItems = obs.GetSceneItems(currentScene);

        // Build lookup table: source name -> item ID
        var sourceToId = sceneItems.ToDictionary(x => x.SourceName, x => x.ItemId);

        // Configure monitor mappings (TODO: Replace these handles with your actual ones)
        var config = new MonitorConfig();
        config.AddMapping(new IntPtr(65626), "Display-0-0");
        config.AddMapping(new IntPtr(65634), "Display-0-1");
        config.AddMapping(new IntPtr(65624), "Display-1-1");
        config.AddMapping(new IntPtr(65630), "Display-2-0");
        config.AddMapping(new IntPtr(65628), "Display-2-2");
        config.AddMapping(new IntPtr(65632), "Display-3-0");
        config.AddMapping(new IntPtr(1604914021), "Display2-1");

        Console.WriteLine("ScreenSwitcher running... Move your mouse between monitors!");
        Console.WriteLine("Press Ctrl+C to exit\n");

        var notifier = new ScreenChangeNotifier();

        notifier.ScreenChanged += (monitorHandle) =>
        {
            var activeSource = config.GetSourceForMonitor(monitorHandle);
            if (activeSource == null) return;

            // Hide all toggle sources
            foreach (var toggleSource in config.GetAllToggleSources())
            {
                if (sourceToId.TryGetValue(toggleSource, out var itemId))
                {
                    obs.SetSourceVisible(currentScene, itemId, false);
                }
            }

            // Show only the active source
            if (sourceToId.TryGetValue(activeSource, out var activeItemId))
            {
                obs.SetSourceVisible(currentScene, activeItemId, true);
                Console.WriteLine($"Switched to: {activeSource}");
            }
        };

        await notifier.StartAsync(100);
    }
}
