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

        var sourceToId = sceneItems
            .Where(x => x.SourceName.StartsWith("T-Monitor-"))
            .ToDictionary(x => x.SourceName, x => x.ItemId);

        Console.WriteLine($"Found {sourceToId.Count} T-Monitor sources");
        Console.WriteLine("ScreenSwitcher running... Move your mouse between monitors!\n");

        var notifier = new ScreenChangeNotifier();

        notifier.ScreenChanged += (monitorInfo) =>
        {
            var activeSource = $"T-Monitor-{monitorInfo.DisplayNumber}";

            Console.WriteLine($"{monitorInfo} -> {activeSource}");

            // Hide all T-Monitor sources
            foreach (var (sourceName, itemId) in sourceToId)
            {
                obs.SetSourceVisible(currentScene, itemId, false);
            }

            // Show active monitor source
            if (sourceToId.TryGetValue(activeSource, out var activeItemId))
            {
                obs.SetSourceVisible(currentScene, activeItemId, true);
            }
        };

        await notifier.StartAsync(100);
    }
}
