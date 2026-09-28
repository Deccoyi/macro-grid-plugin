using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Audio inputs: mute and volume.

public sealed class ObsSetMuteAction(ObsConnection obs) : ObsOptionsAction(obs)
{
    public const string TypeId = "obs.setMute";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Set mute";
    public override string? Icon => "volume-x";
    public override IReadOnlyList<SettingField> Fields =>
    [
        new("inputName", "Audio source", SettingFieldKind.Select) { OptionsSource = "audioInputs" },
        new("mode", "Mode", SettingFieldKind.Segmented) { Options = [new("mute", "Mute"), new("unmute", "Unmute")], Default = "mute" },
    ];

    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        if (!TryGetAudioInput(settings, out var inputName)) return Task.CompletedTask;
        // `muted` (legacy v0.1.0 settings) still read for backward compatibility with existing profiles.
        var muted = settings["mode"] is { } mode ? mode.GetValue<string>() != "unmute" : (settings["muted"]?.GetValue<bool>() ?? true);
        return Obs.RequestAsync("SetInputMute", new JsonObject { ["inputName"] = inputName, ["inputMuted"] = muted }, cancellationToken);
    }

    public static JsonObject Settings(string inputName, bool muted) => new() { ["inputName"] = inputName, ["mode"] = muted ? "mute" : "unmute" };
}

public sealed class ObsToggleMuteAction(ObsConnection obs) : ObsOptionsAction(obs)
{
    public const string TypeId = "obs.toggleMute";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Toggle mute";
    public override string? Icon => "volume-1";
    public override IReadOnlyList<SettingField> Fields => [new("inputName", "Audio source", SettingFieldKind.Select) { OptionsSource = "audioInputs" }];

    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        if (!TryGetAudioInput(settings, out var inputName)) return Task.CompletedTask;
        return Obs.RequestAsync("ToggleInputMute", new JsonObject { ["inputName"] = inputName }, cancellationToken);
    }

    public static JsonObject Settings(string inputName) => new() { ["inputName"] = inputName };
}

/// <summary>Sets an OBS audio input's volume. Settings: { "inputName": string, "volume": number } — volume is
/// 0..100 (%), converted to obs-websocket's 0..1 linear multiplier. The slider/knob's live dragged value
/// (<see cref="ActionContext.Value"/>) overrides the static setting, same pattern as core.setVolume.</summary>
public sealed class ObsSetVolumeAction(ObsConnection obs) : ObsOptionsAction(obs)
{
    public const string TypeId = "obs.setVolume";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Volume";
    public override string? Icon => "volume-2";
    public override IReadOnlyList<SettingField> Fields =>
    [
        new("inputName", "Audio source", SettingFieldKind.Select) { OptionsSource = "audioInputs" },
        new("volume", "Volume (%)", SettingFieldKind.Slider) { Min = 0, Max = 100, Step = 1, Default = 100 },
    ];

    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        if (!TryGetAudioInput(settings, out var inputName)) return Task.CompletedTask;
        var percent = context.Value ?? settings["volume"]?.GetValue<double>() ?? 100;
        var mul = Math.Clamp(percent / 100, 0, 1);
        return Obs.RequestAsync("SetInputVolume", new JsonObject { ["inputName"] = inputName, ["inputVolumeMul"] = mul }, cancellationToken);
    }

    public static JsonObject Settings(string inputName, double volumePercent) => new() { ["inputName"] = inputName, ["volume"] = volumePercent };
}

/// <summary>Nudges an OBS audio input's volume by a relative dB step. Settings: { "inputName": string, "stepDb": number }</summary>
public sealed class ObsAdjustVolumeAction(ObsConnection obs) : ObsOptionsAction(obs)
{
    public const string TypeId = "obs.adjustVolume";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Adjust volume (±dB)";
    public override string? Description => "Raises/lowers relative to the current level";
    public override string? Icon => "volume-2";
    public override IReadOnlyList<SettingField> Fields =>
    [
        new("inputName", "Audio source", SettingFieldKind.Select) { OptionsSource = "audioInputs" },
        new("stepDb", "Step (dB, negative = lower)", SettingFieldKind.Number) { Step = 0.5, Default = 3 },
    ];

    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        if (!TryGetAudioInput(settings, out var inputName)) return Task.CompletedTask;
        var step = settings["stepDb"]?.GetValue<double>() ?? 0;
        return Obs.RequestAsync("SetInputVolume", new JsonObject { ["inputName"] = inputName, ["inputVolumeDb"] = Obs.Cache.GetInputVolumeDb(inputName) + step }, cancellationToken);
    }
}
