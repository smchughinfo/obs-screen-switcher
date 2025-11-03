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

        foreach (var item in sceneItems)
        {
            Console.WriteLine($"{item.SourceName} (ID: {item.ItemId})");
        }

        // Test toggle first source
        var firstSource = sceneItems[0];
        var currentState = obs.GetSourceVisible(currentScene, firstSource.ItemId);

        obs.SetSourceVisible(currentScene, firstSource.ItemId, !currentState);
        await Task.Delay(1000);
        obs.SetSourceVisible(currentScene, firstSource.ItemId, currentState);

        obs.Disconnect();
    }
}
