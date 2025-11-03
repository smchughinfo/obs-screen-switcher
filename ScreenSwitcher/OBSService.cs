using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;

namespace ScreenSwitcher;

public class OBSService
{
    private readonly OBSWebsocket _obs;
    private readonly TaskCompletionSource<bool> _connectionComplete;

    public OBSService()
    {
        _obs = new OBSWebsocket();
        _connectionComplete = new TaskCompletionSource<bool>();

        _obs.Connected += (sender, e) => _connectionComplete.TrySetResult(true);
        _obs.Disconnected += (sender, e) => _connectionComplete.TrySetResult(false);
    }

    public async Task ConnectAsync(string url, string password)
    {
        _obs.ConnectAsync(url, password);

        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(_connectionComplete.Task, timeoutTask);

        if (completedTask == timeoutTask || !await _connectionComplete.Task)
            throw new Exception("Failed to connect to OBS");
    }

    public void SetSourceVisible(string sceneName, int itemId, bool visible)
    {
        _obs.SetSceneItemEnabled(sceneName, itemId, visible);
    }

    public bool GetSourceVisible(string sceneName, int itemId)
    {
        return _obs.GetSceneItemEnabled(sceneName, itemId);
    }

    public List<SceneItemDetails> GetSceneItems(string sceneName)
    {
        return _obs.GetSceneItemList(sceneName);
    }

    public string GetCurrentScene()
    {
        return _obs.GetCurrentProgramScene();
    }

    public void Disconnect()
    {
        if (_obs.IsConnected)
            _obs.Disconnect();
    }
}
