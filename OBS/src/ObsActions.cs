using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

/// <summary>Shared dynamic-dropdown resolution for every OBS action's <c>OptionsSource</c> fields — one
/// place instead of duplicating "query the cache, wrap into OptionsResult" in ten action classes. Reads
/// only from <see cref="ObsConnection.Cache"/>, so a dropdown opens instantly even mid-request to OBS.</summary>
internal static class ObsOptionSources
{
    public static Task<OptionsResult> GetAsync(ObsConnection obs, string sourceId, JsonObject currentValues)
    {
        if (!obs.Cache.HasData)
            return Task.FromResult(new OptionsResult([], "Not connected to OBS"));

        IReadOnlyList<SettingOption> options = sourceId switch
        {
            "scenes" => [.. obs.Cache.Scenes.Select(s => new SettingOption(s, s))],
            "audioInputs" => [.. obs.Cache.AudioInputNames.Select(n => new SettingOption(n, n))],
            "transitions" => [.. obs.Cache.Transitions.Select(n => new SettingOption(n, n))],
            "profiles" => [.. obs.Cache.Profiles.Select(n => new SettingOption(n, n))],
            "textInputs" => [.. obs.Cache.TextInputNames.Select(n => new SettingOption(n, n))],
            "sceneItems" => SceneItemOptions(obs, currentValues),
            _ => [],
        };
        return Task.FromResult(new OptionsResult(options, null));
    }

    private static IReadOnlyList<SettingOption> SceneItemOptions(ObsConnection obs, JsonObject currentValues)
    {
        var sceneName = currentValues["sceneName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(sceneName)) return [];
        return [.. obs.Cache.SceneItems(sceneName).Select(i => new SettingOption(i.SourceName, i.SourceName, i.ParentGroup))];
    }
}

/// <summary>Throws when a scene/input/item the action was configured against no longer exists in OBS —
/// e.g. a button set to "open scene X" when scene X has since been deleted. ActionDispatcher catches and
/// logs it (never a silent no-op), so a stale binding is visibly wrong instead of quietly doing nothing.</summary>
internal static class ObsTargetCheck
{
    public static void RequireScene(ObsConnection obs, string sceneName)
    {
        if (!obs.Cache.Scenes.Contains(sceneName))
            throw new InvalidOperationException($"Scene '{sceneName}' no longer exists in OBS.");
    }

    public static void RequireAudioInput(ObsConnection obs, string inputName)
    {
        if (!obs.Cache.AudioInputNames.Contains(inputName))
            throw new InvalidOperationException($"Audio source '{inputName}' no longer exists in OBS.");
    }
}

// ---- scenes / transitions / studio mode ----

public sealed class ObsSetSceneAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.setScene";
    public string Type => TypeId;
    public string DisplayName => "OBS: Switch scene";
    public string Category => "OBS";
    public string? Description => "Changes the program scene";
    public string? Icon => "clapperboard";
    public IReadOnlyList<SettingField> Fields => [new("sceneName", "Scene", SettingFieldKind.Select) { OptionsSource = "scenes" }];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var sceneName = settings["sceneName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(sceneName)) return Task.CompletedTask;
        ObsTargetCheck.RequireScene(obs, sceneName);
        return obs.RequestAsync("SetCurrentProgramScene", new JsonObject { ["sceneName"] = sceneName }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);

    public static JsonObject Settings(string sceneName) => new() { ["sceneName"] = sceneName };
}

public sealed class ObsSetPreviewSceneAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.setPreviewScene";
    public string Type => TypeId;
    public string DisplayName => "OBS: Preview scene";
    public string Category => "OBS";
    public string? Description => "Changes the preview scene in studio mode";
    public string? Icon => "monitor-play";
    public IReadOnlyList<SettingField> Fields => [new("sceneName", "Scene", SettingFieldKind.Select) { OptionsSource = "scenes" }];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var sceneName = settings["sceneName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(sceneName)) return Task.CompletedTask;
        ObsTargetCheck.RequireScene(obs, sceneName);
        return obs.RequestAsync("SetCurrentPreviewScene", new JsonObject { ["sceneName"] = sceneName }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);
}

public sealed class ObsStudioTransitionAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.studioTransition";
    public string Type => TypeId;
    public string DisplayName => "OBS: Trigger studio transition";
    public string Category => "OBS";
    public string? Description => "Sends the preview to the program (studio mode)";
    public string? Icon => "arrow-left-right";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) =>
        obs.RequestAsync("TriggerStudioModeTransition", null, cancellationToken);
}

public sealed class ObsToggleStudioModeAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.toggleStudioMode";
    public string Type => TypeId;
    public string DisplayName => "OBS: Toggle studio mode";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "columns-2";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) =>
        obs.RequestAsync("SetStudioModeEnabled", new JsonObject { ["studioModeEnabled"] = !obs.Cache.StudioMode }, cancellationToken);
}

public sealed class ObsSetTransitionAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.setTransition";
    public string Type => TypeId;
    public string DisplayName => "OBS: Transition type";
    public string Category => "OBS";
    public string? Description => "Changes the active scene transition";
    public string? Icon => "shuffle";
    public IReadOnlyList<SettingField> Fields => [new("transitionName", "Transition", SettingFieldKind.Select) { OptionsSource = "transitions" }];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var name = settings["transitionName"]?.GetValue<string>();
        return string.IsNullOrEmpty(name) ? Task.CompletedTask : obs.RequestAsync("SetCurrentSceneTransition", new JsonObject { ["transitionName"] = name }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);
}

public sealed class ObsSetProfileAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.setProfile";
    public string Type => TypeId;
    public string DisplayName => "OBS: Switch profile";
    public string Category => "OBS";
    public string? Description => "Changes the OBS profile";
    public string? Icon => "user-cog";
    public IReadOnlyList<SettingField> Fields => [new("profileName", "Profile", SettingFieldKind.Select) { OptionsSource = "profiles" }];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var name = settings["profileName"]?.GetValue<string>();
        return string.IsNullOrEmpty(name) ? Task.CompletedTask : obs.RequestAsync("SetCurrentProfile", new JsonObject { ["profileName"] = name }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);
}

// ---- stream / record / virtual cam / replay buffer ----

public sealed class ObsStartStreamAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.startStream";
    public string Type => TypeId;
    public string DisplayName => "OBS: Start streaming";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "radio";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) => obs.RequestAsync("StartStream", null, cancellationToken);
}

public sealed class ObsStopStreamAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.stopStream";
    public string Type => TypeId;
    public string DisplayName => "OBS: Stop streaming";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "square";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) => obs.RequestAsync("StopStream", null, cancellationToken);
}

public sealed class ObsToggleStreamAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.toggleStream";
    public string Type => TypeId;
    public string DisplayName => "OBS: Toggle streaming";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "radio";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) => obs.RequestAsync("ToggleStream", null, cancellationToken);
}

public sealed class ObsStartRecordAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.startRecord";
    public string Type => TypeId;
    public string DisplayName => "OBS: Start recording";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "circle";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) => obs.RequestAsync("StartRecord", null, cancellationToken);
}

public sealed class ObsStopRecordAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.stopRecord";
    public string Type => TypeId;
    public string DisplayName => "OBS: Stop recording";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "square";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) => obs.RequestAsync("StopRecord", null, cancellationToken);
}

public sealed class ObsToggleRecordAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.toggleRecord";
    public string Type => TypeId;
    public string DisplayName => "OBS: Toggle recording";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "circle";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) => obs.RequestAsync("ToggleRecord", null, cancellationToken);
}

/// <summary>Pauses/resumes/toggles OBS recording. Settings: { "mode": "pause"|"resume"|"toggle" }</summary>
public sealed class ObsPauseRecordAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.pauseRecord";
    public string Type => TypeId;
    public string DisplayName => "OBS: Pause recording";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "pause";
    public IReadOnlyList<SettingField> Fields =>
    [
        new("mode", "Mode", SettingFieldKind.Segmented)
        {
            Options = [new("pause", "Pause"), new("resume", "Resume"), new("toggle", "Toggle")],
            Default = "toggle",
        },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var mode = settings["mode"]?.GetValue<string>() ?? "toggle";
        var requestType = mode switch { "pause" => "PauseRecord", "resume" => "ResumeRecord", _ => "ToggleRecordPause" };
        return obs.RequestAsync(requestType, null, cancellationToken);
    }

    public static JsonObject Settings(string mode) => new() { ["mode"] = mode };
}

/// <summary>Starts/stops/toggles the OBS virtual camera. Settings: { "mode": "start"|"stop"|"toggle" }</summary>
public sealed class ObsVirtualCamAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.virtualCam";
    public string Type => TypeId;
    public string DisplayName => "OBS: Virtual camera";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "webcam";
    public IReadOnlyList<SettingField> Fields =>
    [
        new("mode", "Mode", SettingFieldKind.Segmented)
        {
            Options = [new("start", "Start"), new("stop", "Stop"), new("toggle", "Toggle")],
            Default = "toggle",
        },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var mode = settings["mode"]?.GetValue<string>() ?? "toggle";
        var requestType = mode switch { "start" => "StartVirtualCam", "stop" => "StopVirtualCam", _ => "ToggleVirtualCam" };
        return obs.RequestAsync(requestType, null, cancellationToken);
    }

    public static JsonObject Settings(string mode) => new() { ["mode"] = mode };
}

/// <summary>Starts/stops/toggles the OBS replay buffer. Settings: { "mode": "start"|"stop"|"toggle" }</summary>
public sealed class ObsReplayBufferAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.replayBuffer";
    public string Type => TypeId;
    public string DisplayName => "OBS: Replay buffer";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "rewind";
    public IReadOnlyList<SettingField> Fields =>
    [
        new("mode", "Mode", SettingFieldKind.Segmented)
        {
            Options = [new("start", "Start"), new("stop", "Stop"), new("toggle", "Toggle")],
            Default = "toggle",
        },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var mode = settings["mode"]?.GetValue<string>() ?? "toggle";
        var requestType = mode switch { "start" => "StartReplayBuffer", "stop" => "StopReplayBuffer", _ => "ToggleReplayBuffer" };
        return obs.RequestAsync(requestType, null, cancellationToken);
    }

    public static JsonObject Settings(string mode) => new() { ["mode"] = mode };
}

public sealed class ObsSaveReplayAction(ObsConnection obs) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "obs.saveReplay";
    public string Type => TypeId;
    public string DisplayName => "OBS: Save replay";
    public string Category => "OBS";
    public string? Description => "Saves the last few seconds of the replay buffer to disk";
    public string? Icon => "save";
    public IReadOnlyList<SettingField> Fields => [];
    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) => obs.RequestAsync("SaveReplayBuffer", null, cancellationToken);
}

// ---- audio ----

public sealed class ObsSetMuteAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.setMute";
    public string Type => TypeId;
    public string DisplayName => "OBS: Set mute";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "volume-x";
    public IReadOnlyList<SettingField> Fields =>
    [
        new("inputName", "Audio source", SettingFieldKind.Select) { OptionsSource = "audioInputs" },
        new("mode", "Mode", SettingFieldKind.Segmented) { Options = [new("mute", "Mute"), new("unmute", "Unmute")], Default = "mute" },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var inputName = settings["inputName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(inputName)) return Task.CompletedTask;
        ObsTargetCheck.RequireAudioInput(obs, inputName);
        // `muted` (legacy v0.1.0 settings) still read for backward compatibility with existing profiles.
        var muted = settings["mode"] is { } mode ? mode.GetValue<string>() != "unmute" : (settings["muted"]?.GetValue<bool>() ?? true);
        return obs.RequestAsync("SetInputMute", new JsonObject { ["inputName"] = inputName, ["inputMuted"] = muted }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);

    public static JsonObject Settings(string inputName, bool muted) => new() { ["inputName"] = inputName, ["mode"] = muted ? "mute" : "unmute" };
}

public sealed class ObsToggleMuteAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.toggleMute";
    public string Type => TypeId;
    public string DisplayName => "OBS: Toggle mute";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "volume-1";
    public IReadOnlyList<SettingField> Fields => [new("inputName", "Audio source", SettingFieldKind.Select) { OptionsSource = "audioInputs" }];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var inputName = settings["inputName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(inputName)) return Task.CompletedTask;
        ObsTargetCheck.RequireAudioInput(obs, inputName);
        return obs.RequestAsync("ToggleInputMute", new JsonObject { ["inputName"] = inputName }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);

    public static JsonObject Settings(string inputName) => new() { ["inputName"] = inputName };
}

/// <summary>Sets an OBS audio input's volume. Settings: { "inputName": string, "volume": number } — volume is
/// 0..100 (%), converted to obs-websocket's 0..1 linear multiplier. The slider/knob's live dragged value
/// (<see cref="ActionContext.Value"/>) overrides the static setting, same pattern as core.setVolume.</summary>
public sealed class ObsSetVolumeAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.setVolume";
    public string Type => TypeId;
    public string DisplayName => "OBS: Volume";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "volume-2";
    public IReadOnlyList<SettingField> Fields =>
    [
        new("inputName", "Audio source", SettingFieldKind.Select) { OptionsSource = "audioInputs" },
        new("volume", "Volume (%)", SettingFieldKind.Slider) { Min = 0, Max = 100, Step = 1, Default = 100 },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var inputName = settings["inputName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(inputName)) return Task.CompletedTask;
        ObsTargetCheck.RequireAudioInput(obs, inputName);
        var percent = context.Value ?? settings["volume"]?.GetValue<double>() ?? 100;
        var mul = Math.Clamp(percent / 100, 0, 1);
        return obs.RequestAsync("SetInputVolume", new JsonObject { ["inputName"] = inputName, ["inputVolumeMul"] = mul }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);

    public static JsonObject Settings(string inputName, double volumePercent) => new() { ["inputName"] = inputName, ["volume"] = volumePercent };
}

/// <summary>Nudges an OBS audio input's volume by a relative dB step. Settings: { "inputName": string, "stepDb": number }</summary>
public sealed class ObsAdjustVolumeAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.adjustVolume";
    public string Type => TypeId;
    public string DisplayName => "OBS: Adjust volume (±dB)";
    public string Category => "OBS";
    public string? Description => "Raises/lowers relative to the current level";
    public string? Icon => "volume-2";
    public IReadOnlyList<SettingField> Fields =>
    [
        new("inputName", "Audio source", SettingFieldKind.Select) { OptionsSource = "audioInputs" },
        new("stepDb", "Step (dB, negative = lower)", SettingFieldKind.Number) { Step = 0.5, Default = 3 },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var inputName = settings["inputName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(inputName)) return Task.CompletedTask;
        ObsTargetCheck.RequireAudioInput(obs, inputName);
        var step = settings["stepDb"]?.GetValue<double>() ?? 0;
        return obs.RequestAsync("SetInputVolume", new JsonObject { ["inputName"] = inputName, ["inputVolumeDb"] = obs.Cache.GetInputVolumeDb(inputName) + step }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);
}

// ---- scene items / text ----

/// <summary>Shows/hides/toggles a scene item, including one nested inside a group. Settings:
/// { "sceneName": string, "sourceName": string, "parentGroup"?: string, "mode": "show"|"hide"|"toggle" }</summary>
public sealed class ObsSetItemVisibilityAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.setItemVisibility";
    public string Type => TypeId;
    public string DisplayName => "OBS: Item visibility";
    public string Category => "OBS";
    public string? Description => "Shows/hides a scene item (groups included)";
    public string? Icon => "eye";
    public IReadOnlyList<SettingField> Fields =>
    [
        new("sceneName", "Scene", SettingFieldKind.Select) { OptionsSource = "scenes" },
        new("sourceName", "Item", SettingFieldKind.Select) { OptionsSource = "sceneItems", DependsOn = ["sceneName"] },
        new("mode", "Mode", SettingFieldKind.Segmented) { Options = [new("show", "Show"), new("hide", "Hide"), new("toggle", "Toggle")], Default = "toggle" },
    ];

    public async Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var sceneName = settings["sceneName"]?.GetValue<string>();
        var sourceName = settings["sourceName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sourceName)) return;

        var item = obs.Cache.SceneItems(sceneName).FirstOrDefault(i => i.SourceName == sourceName)
            ?? throw new InvalidOperationException($"Item '{sourceName}' is no longer in scene '{sceneName}'.");

        var container = item.ParentGroup ?? sceneName;
        var idResponse = await obs.RequestAsync("GetSceneItemId", new JsonObject { ["sceneName"] = container, ["sourceName"] = sourceName }, cancellationToken);
        var itemId = idResponse["sceneItemId"]?.GetValue<int>() ?? item.SceneItemId;

        var mode = settings["mode"]?.GetValue<string>() ?? "toggle";
        var enabled = mode switch { "show" => true, "hide" => false, _ => !item.Enabled };
        await obs.RequestAsync("SetSceneItemEnabled", new JsonObject { ["sceneName"] = container, ["sceneItemId"] = itemId, ["sceneItemEnabled"] = enabled }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);

    public static JsonObject Settings(string sceneName, string sourceName, string mode) =>
        new() { ["sceneName"] = sceneName, ["sourceName"] = sourceName, ["mode"] = mode };
}

/// <summary>Sets a text source's content. Settings: { "sourceName": string, "text": string } — text supports
/// {variable} templates (see AllowVariables), resolved by the host before ExecuteAsync is called.</summary>
public sealed class ObsSetTextAction(ObsConnection obs) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "obs.setText";
    public string Type => TypeId;
    public string DisplayName => "OBS: Set text source";
    public string Category => "OBS";
    public string? Description => null;
    public string? Icon => "type";
    public IReadOnlyList<SettingField> Fields =>
    [
        new("sourceName", "Text source", SettingFieldKind.Select) { OptionsSource = "textInputs" },
        new("text", "Text", SettingFieldKind.Text) { AllowVariables = true },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var sourceName = settings["sourceName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(sourceName)) return Task.CompletedTask;
        var text = settings["text"]?.GetValue<string>() ?? "";
        return obs.RequestAsync("SetInputSettings", new JsonObject
        {
            ["inputName"] = sourceName,
            ["inputSettings"] = new JsonObject { ["text"] = text },
        }, cancellationToken);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        ObsOptionSources.GetAsync(obs, sourceId, currentValues);
}
