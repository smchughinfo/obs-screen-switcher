using OBSWebsocketDotNet;
using System.Text.Json;

namespace ScreenSwitcher;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== OBS WebSocket Connection Test ===\n");

        // Read password from file
        var passwordFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "obs-password.txt");
        if (!File.Exists(passwordFile))
        {
            Console.WriteLine($"ERROR: Password file not found at {passwordFile}");
            return;
        }

        var password = File.ReadAllText(passwordFile).Trim();
        Console.WriteLine($"✓ Password loaded from file");
        Console.WriteLine($"  Password length: {password.Length} characters");
        Console.WriteLine($"  First 4 chars: {(password.Length >= 4 ? password.Substring(0, 4) : password)}...");

        // Create OBS WebSocket client
        var obs = new OBSWebsocket();
        var connectionComplete = new TaskCompletionSource<bool>();

        // Setup event handlers
        obs.Connected += (sender, e) =>
        {
            Console.WriteLine("✓ Connected to OBS WebSocket");
            connectionComplete.TrySetResult(true);
        };

        obs.Disconnected += (sender, e) =>
        {
            var disconnectInfo = e as OBSWebsocketDotNet.Communication.ObsDisconnectionInfo;
            Console.WriteLine($"✗ Disconnected from OBS WebSocket");
            if (disconnectInfo != null)
            {
                Console.WriteLine($"  Reason: {disconnectInfo.DisconnectReason}");
                //Console.WriteLine($"  Type: {disconnectInfo.Type}");
            }
            connectionComplete.TrySetResult(false);
        };

        try
        {
            // Connect to OBS
            Console.WriteLine("\nConnecting to OBS at ws://localhost:4455...");
            obs.ConnectAsync("ws://localhost:4455", password);

            // Wait for connection to fully establish (with timeout)
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(connectionComplete.Task, timeoutTask);

            if (completedTask == timeoutTask || !await connectionComplete.Task)
            {
                Console.WriteLine("✗ Connection timeout or failed");
                return;
            }

            if (!obs.IsConnected)
            {
                Console.WriteLine("✗ Failed to connect. Is OBS running with WebSocket enabled?");
                return;
            }

            // Get version info
            var version = obs.GetVersion();
            Console.WriteLine($"\n=== OBS Info ===");
            Console.WriteLine($"OBS Version: {version.OBSStudioVersion}");
            Console.WriteLine($"WebSocket Version: {version.PluginVersion}");

            // List all scenes
            Console.WriteLine($"\n=== Available Scenes ===");
            var scenes = obs.ListScenes();
            foreach (var scene in scenes)
            {
                Console.WriteLine($"- {scene.Name}");
            }

            // Get current scene
            var currentScene = obs.GetCurrentProgramScene();
            Console.WriteLine($"\n=== Current Scene ===");
            Console.WriteLine($"Active: {currentScene}");

            // List sources in current scene
            Console.WriteLine($"\n=== Sources in '{currentScene}' ===");
            var sceneItems = obs.GetSceneItemList(currentScene);
            foreach (var item in sceneItems)
            {
                Console.WriteLine($"- [{item.SourceName}]");
                Console.WriteLine($"  Item ID: {item.ItemId}");
                Console.WriteLine($"  Type: {item.SourceType}");
            }

            // Test: Toggle first source if available
            if (sceneItems.Count > 0)
            {
                var firstSource = sceneItems[0];
                Console.WriteLine($"\n=== Testing Source Toggle ===");
                Console.WriteLine($"Toggling source: {firstSource.SourceName} (ID: {firstSource.ItemId})");

                // Get current enabled state
                var currentState = obs.GetSceneItemEnabled(currentScene, firstSource.ItemId);
                Console.WriteLine($"Current state: {(currentState ? "Visible" : "Hidden")}");

                // Toggle off
                obs.SetSceneItemEnabled(currentScene, firstSource.ItemId, !currentState);
                Console.WriteLine($"Toggled to: {(!currentState ? "Visible" : "Hidden")}");

                await Task.Delay(1500);

                // Toggle back on
                obs.SetSceneItemEnabled(currentScene, firstSource.ItemId, currentState);
                Console.WriteLine($"Restored to: {(currentState ? "Visible" : "Hidden")}");
            }

            Console.WriteLine($"\n✓ Test completed successfully!");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            if (obs.IsConnected)
            {
                obs.Disconnect();
                Console.WriteLine("\n✓ Disconnected from OBS");
            }
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
