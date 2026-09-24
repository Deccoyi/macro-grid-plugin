using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Events: obs-websocket events applied to the cache and the variables.
public sealed partial class ObsConnection
{
    private void HandleEvent(IVariableStore store, string eventType, JsonObject data)
    {
        switch (eventType)
        {
            case "ExitStarted":
                // OBS itself is shutting down — close now instead of waiting for the OS to notice a dead
                // TCP connection. RunAsync's tick loop returns once client.Completion completes.
                _obsExitStarted = true;
                _ = CurrentClient?.DisposeAsync();
                break;

            case "CurrentProgramSceneChanged":
                _cache.OnCurrentSceneChanged(data.TryGetString("sceneName") ?? "");
                store.Set("obs.scene.current", _cache.CurrentScene);
                break;
            case "CurrentPreviewSceneChanged":
                _cache.OnCurrentPreviewSceneChanged(data.TryGetString("sceneName") ?? "");
                store.Set("obs.scene.preview", _cache.CurrentPreviewScene);
                break;
            case "SceneCreated":
                _cache.OnSceneCreated(data.TryGetString("sceneName") ?? "");
                break;
            case "SceneRemoved":
                _cache.OnSceneRemoved(data.TryGetString("sceneName") ?? "");
                break;
            case "SceneNameChanged":
                _cache.OnSceneRenamed(data.TryGetString("oldSceneName") ?? "", data.TryGetString("sceneName") ?? "");
                break;
            case "SceneListChanged":
                break; // individual Created/Removed/NameChanged events already keep the cache in sync.

            case "StudioModeStateChanged":
                _cache.OnStudioModeChanged(data.TryGetBool("studioModeEnabled"));
                store.Set("obs.studioMode", _cache.StudioMode);
                break;
            case "CurrentProfileChanged":
                _cache.OnCurrentProfileChanged(data.TryGetString("profileName") ?? "");
                store.Set("obs.profile.current", _cache.CurrentProfile);
                break;
            case "CurrentSceneTransitionChanged":
                _cache.OnCurrentTransitionChanged(data.TryGetString("transitionName") ?? "");
                store.Set("obs.transition.current", _cache.CurrentTransition);
                break;
            case "CurrentSceneCollectionChanged":
                // A new scene collection swaps out the entire scene/input graph — full resync.
                var client = CurrentClient;
                if (client is not null) _ = ResyncAsync(client, store);
                break;

            case "InputCreated":
                var caps = data.TryGetInt("inputKindCaps");
                _cache.OnInputCreated(data.TryGetString("inputName") ?? "", data.TryGetString("inputKind") ?? "", (caps & 0b10) != 0);
                break;
            case "InputRemoved":
                RemoveInputVariables(store, data.TryGetString("inputName") ?? "");
                _cache.OnInputRemoved(data.TryGetString("inputName") ?? "");
                break;
            case "InputNameChanged":
                RemoveInputVariables(store, data.TryGetString("oldInputName") ?? "");
                _cache.OnInputRenamed(data.TryGetString("oldInputName") ?? "", data.TryGetString("inputName") ?? "");
                PublishInputVariables(store, data.TryGetString("inputName") ?? "");
                break;
            case "InputMuteStateChanged":
            {
                var name = data.TryGetString("inputName") ?? "";
                _cache.OnInputMuteStateChanged(name, data.TryGetBool("inputMuted"));
                if (_cache.IsAudioInput(name)) store.Set($"obs.input.{Slug(name)}.muted", _cache.GetInputMuted(name));
                break;
            }
            case "InputVolumeChanged":
            {
                var name = data.TryGetString("inputName") ?? "";
                _cache.OnInputVolumeChanged(name, data.TryGetDouble("inputVolumeDb"));
                if (_cache.IsAudioInput(name)) store.Set($"obs.input.{Slug(name)}.volumeDb", _cache.GetInputVolumeDb(name));
                break;
            }

            case "SceneItemEnableStateChanged":
            {
                var sceneName = data.TryGetString("sceneName") ?? "";
                var itemId = data.TryGetInt("sceneItemId");
                var enabled = data.TryGetBool("sceneItemEnabled");
                _cache.OnSceneItemEnableStateChanged(sceneName, itemId, enabled);
                var item = _cache.SceneItems(sceneName).FirstOrDefault(i => i.SceneItemId == itemId);
                if (item is not null) store.Set($"obs.item.{Slug(sceneName)}.{Slug(item.SourceName)}.visible", enabled);
                break;
            }

            case "StreamStateChanged":
                store.Set("obs.streaming", data.TryGetBool("outputActive"));
                break;
            case "RecordStateChanged":
                store.Set("obs.recording", data.TryGetBool("outputActive"));
                break;
            case "VirtualcamStateChanged":
                store.Set("obs.virtualcam", data.TryGetBool("outputActive"));
                break;
            case "ReplayBufferStateChanged":
                store.Set("obs.replayBuffer", data.TryGetBool("outputActive"));
                break;
        }
    }

    private async Task ResyncAsync(ObsClient client, IVariableStore store)
    {
        try
        {
            await _cache.RefreshAllAsync(client, CancellationToken.None);
            PublishSceneAndInputVariables(store);
        }
        catch (Exception ex) when (ex is ObsRequestException or TimeoutException or IOException)
        {
            host.Log($"Resync after a scene collection change failed: {ex.Message}");
        }
    }
}
