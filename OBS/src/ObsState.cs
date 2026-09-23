using System.Text.Json.Nodes;
using MacroStation.Plugin.Abstractions;

namespace MacroStation.Plugin.Obs;

public sealed record ObsSceneItem(string SourceName, int SceneItemId, bool IsGroup, bool Enabled, string? ParentGroup);

public sealed record ObsInputInfo(string Name, string Kind, bool IsAudio);

/// <summary>
/// In-memory cache of everything OBS knows about the current session — scenes, inputs, scene items
/// (groups flattened, with a depth limit so a pathological group cycle can't loop forever), profiles,
/// transitions, and the handful of live booleans (mute, item visibility) actions and variables both read.
/// Filled once with a handful of batched requests on connect (see <see cref="RefreshAllAsync"/>), then
/// kept in sync incrementally from events. Every dropdown and every
/// `obs.*` variable is served from here, so none of it waits on a live OBS round-trip.
/// </summary>
public sealed class ObsState
{
    private const int MaxGroupDepth = 8;

    private readonly Lock _lock = new();
    private List<string> _scenes = [];
    private Dictionary<string, ObsInputInfo> _inputs = new(StringComparer.Ordinal);
    private Dictionary<string, List<ObsSceneItem>> _sceneItems = new(StringComparer.Ordinal);
    private List<string> _profiles = [];
    private List<string> _sceneCollections = [];
    private List<string> _transitions = [];
    private Dictionary<string, bool> _inputMuted = new(StringComparer.Ordinal);
    private Dictionary<string, double> _inputVolumeDb = new(StringComparer.Ordinal);
    private bool _hasData;

    /// <summary>True once <see cref="RefreshAllAsync"/> has completed at least once for the current
    /// connection — dropdowns show "OBS'e bağlı değil" instead of a silently empty list until then.</summary>
    public bool HasData { get { lock (_lock) return _hasData; } }

    public string? CurrentScene { get; private set; }
    public string? CurrentPreviewScene { get; private set; }
    public string? CurrentProfile { get; private set; }
    public string? CurrentSceneCollection { get; private set; }
    public string? CurrentTransition { get; private set; }
    public bool StudioMode { get; private set; }

    public IReadOnlyList<string> Scenes { get { lock (_lock) return [.. _scenes]; } }
    public IReadOnlyList<string> AudioInputNames { get { lock (_lock) return [.. _inputs.Values.Where(i => i.IsAudio).Select(i => i.Name)]; } }
    public IReadOnlyList<string> AllInputNames { get { lock (_lock) return [.. _inputs.Keys]; } }
    public IReadOnlyList<string> TextInputNames { get { lock (_lock) return [.. _inputs.Values.Where(i => i.Kind.Contains("text", StringComparison.OrdinalIgnoreCase)).Select(i => i.Name)]; } }
    public IReadOnlyList<string> Profiles { get { lock (_lock) return [.. _profiles]; } }
    public IReadOnlyList<string> Transitions { get { lock (_lock) return [.. _transitions]; } }

    public IReadOnlyList<ObsSceneItem> SceneItems(string sceneName)
    {
        lock (_lock) return _sceneItems.TryGetValue(sceneName, out var items) ? [.. items] : [];
    }

    public bool IsAudioInput(string name) { lock (_lock) return _inputs.TryGetValue(name, out var i) && i.IsAudio; }
    public bool GetInputMuted(string name) { lock (_lock) return _inputMuted.GetValueOrDefault(name); }
    public double GetInputVolumeDb(string name) { lock (_lock) return _inputVolumeDb.GetValueOrDefault(name); }

    /// <summary>Full resync — called once on connect and again whenever the scene collection changes
    /// (which swaps out the entire scene/input graph from under us).</summary>
    public async Task RefreshAllAsync(ObsClient client, CancellationToken cancellationToken)
    {
        var results = await client.RequestBatchAsync(
        [
            ("GetSceneList", null),
            ("GetInputList", null),
            ("GetSpecialInputs", null),
            ("GetProfileList", null),
            ("GetSceneCollectionList", null),
            ("GetStudioModeEnabled", null),
            ("GetCurrentSceneTransition", null),
        ], cancellationToken);

        var byType = results.ToLookup(r => r.RequestType);

        var scenes = new List<string>();
        if (byType["GetSceneList"].FirstOrDefault() is { Success: true } sceneList)
        {
            CurrentScene = sceneList.ResponseData.TryGetString("currentProgramSceneName");
            CurrentPreviewScene = sceneList.ResponseData.TryGetString("currentPreviewSceneName");
            foreach (var node in sceneList.ResponseData.TryGetArray("scenes") ?? [])
            {
                if ((node as JsonObject).TryGetString("sceneName") is { } name) scenes.Add(name);
            }
            scenes.Reverse(); // obs-websocket returns scenes in reverse (top-of-stack-first) order.
        }

        var inputs = new Dictionary<string, ObsInputInfo>(StringComparer.Ordinal);
        var capsUnknown = new List<string>();
        if (byType["GetInputList"].FirstOrDefault() is { Success: true } inputList)
        {
            foreach (var node in inputList.ResponseData.TryGetArray("inputs") ?? [])
            {
                var obj = node as JsonObject;
                var name = obj.TryGetString("inputName");
                if (name is null) continue;
                var kind = obj.TryGetString("inputKind") ?? "";
                // OBS_SOURCE_AUDIO = 1<<1; older obs-websocket may omit the caps field entirely, in which
                // case IsAudio starts false and a probe batch below fills it in from GetInputMute.
                var caps = obj?["inputKindCaps"];
                var isAudio = caps is not null && (obj.TryGetInt("inputKindCaps") & 0b10) != 0;
                if (caps is null) capsUnknown.Add(name);
                inputs[name] = new ObsInputInfo(name, kind, isAudio);
            }
        }

        if (byType["GetProfileList"].FirstOrDefault() is { Success: true } profileList)
        {
            CurrentProfile = profileList.ResponseData.TryGetString("currentProfileName");
        }
        var profiles = (byType["GetProfileList"].FirstOrDefault().ResponseData?.TryGetArray("profiles"))
            ?.Select(n => n?.GetValue<string>()).Where(s => s is not null).Select(s => s!).ToList() ?? [];

        var sceneCollections = new List<string>();
        if (byType["GetSceneCollectionList"].FirstOrDefault() is { Success: true } sccList)
        {
            CurrentSceneCollection = sccList.ResponseData.TryGetString("currentSceneCollectionName");
            foreach (var node in sccList.ResponseData.TryGetArray("sceneCollections") ?? [])
            {
                if (node?.GetValue<string>() is { } name) sceneCollections.Add(name);
            }
        }

        if (byType["GetStudioModeEnabled"].FirstOrDefault() is { Success: true } studio)
            StudioMode = studio.ResponseData.TryGetBool("studioModeEnabled");

        if (byType["GetCurrentSceneTransition"].FirstOrDefault() is { Success: true } transition)
            CurrentTransition = transition.ResponseData.TryGetString("transitionName");

        // Inputs without a usable inputKindCaps (older obs-websocket) are probed individually — a mute
        // query only succeeds on an audio-capable input.
        if (capsUnknown.Count > 0)
        {
            var probes = await client.RequestBatchAsync(
                [.. capsUnknown.Select(name => ("GetInputMute", (JsonObject?)new JsonObject { ["inputName"] = name }))],
                cancellationToken);
            for (var i = 0; i < probes.Count && i < capsUnknown.Count; i++)
            {
                if (probes[i].Success) inputs[capsUnknown[i]] = inputs[capsUnknown[i]] with { IsAudio = true };
            }
        }

        var muted = new Dictionary<string, bool>(StringComparer.Ordinal);
        var volumeDb = new Dictionary<string, double>(StringComparer.Ordinal);
        var audioNames = inputs.Values.Where(i => i.IsAudio).Select(i => i.Name).ToList();
        if (audioNames.Count > 0)
        {
            var muteResults = await client.RequestBatchAsync(
                [.. audioNames.Select(name => ("GetInputMute", (JsonObject?)new JsonObject { ["inputName"] = name }))], cancellationToken);
            var volumeResults = await client.RequestBatchAsync(
                [.. audioNames.Select(name => ("GetInputVolume", (JsonObject?)new JsonObject { ["inputName"] = name }))], cancellationToken);
            for (var i = 0; i < audioNames.Count; i++)
            {
                if (i < muteResults.Count && muteResults[i].Success) muted[audioNames[i]] = muteResults[i].ResponseData.TryGetBool("inputMuted");
                if (i < volumeResults.Count && volumeResults[i].Success) volumeDb[audioNames[i]] = volumeResults[i].ResponseData.TryGetDouble("inputVolumeDb");
            }
        }

        var sceneItems = new Dictionary<string, List<ObsSceneItem>>(StringComparer.Ordinal);
        foreach (var scene in scenes)
            sceneItems[scene] = await FetchSceneItemsAsync(client, scene, cancellationToken);

        var transitions = new List<string>();
        var transitionList = await client.RequestAsync("GetSceneTransitionList", null, cancellationToken);
        foreach (var node in transitionList.TryGetArray("transitions") ?? [])
        {
            if ((node as JsonObject).TryGetString("transitionName") is { } name) transitions.Add(name);
        }

        lock (_lock)
        {
            _scenes = scenes;
            _inputs = inputs;
            _sceneItems = sceneItems;
            _profiles = profiles;
            _sceneCollections = sceneCollections;
            _transitions = transitions;
            _inputMuted = muted;
            _inputVolumeDb = volumeDb;
            _hasData = true;
        }
    }

    private async Task<List<ObsSceneItem>> FetchSceneItemsAsync(ObsClient client, string sceneName, CancellationToken cancellationToken)
    {
        var result = new List<ObsSceneItem>();
        await CollectSceneItemsAsync(client, sceneName, parentGroup: null, depth: 0, visited: [sceneName], result, cancellationToken);
        return result;
    }

    private async Task CollectSceneItemsAsync(ObsClient client, string containerName, string? parentGroup, int depth, HashSet<string> visited, List<ObsSceneItem> into, CancellationToken cancellationToken)
    {
        if (depth > MaxGroupDepth) return;
        JsonObject response;
        try
        {
            response = await client.RequestAsync(parentGroup is null ? "GetSceneItemList" : "GetGroupSceneItemList",
                new JsonObject { ["sceneName"] = containerName }, cancellationToken);
        }
        catch (ObsRequestException)
        {
            return; // scene/group vanished mid-refresh — leave it out rather than fail the whole cache build.
        }

        foreach (var node in response.TryGetArray("sceneItems") ?? [])
        {
            var obj = node as JsonObject;
            var sourceName = obj.TryGetString("sourceName");
            if (sourceName is null) continue;
            var isGroup = obj.TryGetBool("isGroup");
            var item = new ObsSceneItem(sourceName, obj.TryGetInt("sceneItemId"), isGroup, obj.TryGetBool("sceneItemEnabled", true), parentGroup);
            into.Add(item);

            if (isGroup && visited.Add(sourceName))
                await CollectSceneItemsAsync(client, sourceName, sourceName, depth + 1, visited, into, cancellationToken);
        }
    }

    // ---- incremental updates from events ----

    public void OnSceneCreated(string sceneName) { lock (_lock) { if (!_scenes.Contains(sceneName)) _scenes.Add(sceneName); _sceneItems.TryAdd(sceneName, []); } }
    public void OnSceneRemoved(string sceneName) { lock (_lock) { _scenes.Remove(sceneName); _sceneItems.Remove(sceneName); } }
    public void OnSceneRenamed(string oldName, string newName)
    {
        lock (_lock)
        {
            var idx = _scenes.IndexOf(oldName);
            if (idx >= 0) _scenes[idx] = newName;
            if (_sceneItems.Remove(oldName, out var items)) _sceneItems[newName] = items;
            if (CurrentScene == oldName) CurrentScene = newName;
            if (CurrentPreviewScene == oldName) CurrentPreviewScene = newName;
        }
    }
    public void OnCurrentSceneChanged(string sceneName) => CurrentScene = sceneName;
    public void OnCurrentPreviewSceneChanged(string sceneName) => CurrentPreviewScene = sceneName;
    public void OnStudioModeChanged(bool enabled) => StudioMode = enabled;
    public void OnCurrentProfileChanged(string name) => CurrentProfile = name;
    public void OnCurrentTransitionChanged(string name) => CurrentTransition = name;

    public void OnInputCreated(string name, string kind, bool isAudio)
    {
        lock (_lock) _inputs[name] = new ObsInputInfo(name, kind, isAudio);
    }
    public void OnInputRemoved(string name)
    {
        lock (_lock) { _inputs.Remove(name); _inputMuted.Remove(name); _inputVolumeDb.Remove(name); }
    }
    public void OnInputRenamed(string oldName, string newName)
    {
        lock (_lock)
        {
            if (_inputs.Remove(oldName, out var info)) _inputs[newName] = info with { Name = newName };
            if (_inputMuted.Remove(oldName, out var m)) _inputMuted[newName] = m;
            if (_inputVolumeDb.Remove(oldName, out var v)) _inputVolumeDb[newName] = v;
        }
    }
    public void OnInputMuteStateChanged(string name, bool muted) { lock (_lock) _inputMuted[name] = muted; }
    public void OnInputVolumeChanged(string name, double volumeDb) { lock (_lock) _inputVolumeDb[name] = volumeDb; }

    /// <summary>Called on disconnect so a dropdown shows "OBS'e bağlı değil" instead of a stale scene/
    /// input list from the previous session (which may no longer be accurate once OBS is reachable again).</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _scenes = [];
            _inputs.Clear();
            _sceneItems.Clear();
            _profiles = [];
            _sceneCollections = [];
            _transitions = [];
            _inputMuted.Clear();
            _inputVolumeDb.Clear();
            _hasData = false;
        }
        CurrentScene = null;
        CurrentPreviewScene = null;
        CurrentProfile = null;
        CurrentSceneCollection = null;
        CurrentTransition = null;
        StudioMode = false;
    }

    public void OnSceneItemEnableStateChanged(string sceneName, int sceneItemId, bool enabled)
    {
        lock (_lock)
        {
            if (!_sceneItems.TryGetValue(sceneName, out var items)) return;
            var idx = items.FindIndex(i => i.SceneItemId == sceneItemId);
            if (idx >= 0) items[idx] = items[idx] with { Enabled = enabled };
        }
    }
}
