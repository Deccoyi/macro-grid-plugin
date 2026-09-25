using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Sound;

/// <summary>Shared "sound" dropdown, sourced live from the current settings so a renamed or newly added
/// sound shows up without reopening the action's settings.</summary>
internal static class SoundOptionsSource
{
    public static OptionsResult GetSounds(SoundEngine engine)
    {
        var options = engine.Settings.Sounds
            .Select(s => new SettingOption(s.Id, s.Name.Length > 0 ? s.Name : Path.GetFileNameWithoutExtension(s.File)))
            .ToArray();
        return new OptionsResult(options);
    }
}

/// <summary>Plays a sound. Settings: <c>sound</c> (id), <c>playMode</c> (full/hold/toggle),
/// <c>stopStyle</c> (default/immediate/fade, applied on a hold release or a toggle stop).</summary>
public sealed class SoundPlayAction(SoundEngine engine) : IActionHandler, IActionDescriptor, IOptionsSource, IReleaseAwareAction
{
    public const string TypeId = "sound.play";
    public string Type => TypeId;
    public string DisplayName => "Sound: Play";
    public string Category => "Sound";
    public string? Description => "Plays, or for \"hold\"/\"toggle\", also stops a sound";
    public string? Icon => "play";

    public IReadOnlyList<SettingField> Fields =>
    [
        new("sound", "Sound", SettingFieldKind.Select) { OptionsSource = "sounds" },
        new("playMode", "Play mode", SettingFieldKind.Segmented)
        {
            Options = [new("full", "Full"), new("hold", "Hold"), new("toggle", "Toggle")],
            Default = "full",
        },
        new("stopStyle", "Stop style", SettingFieldKind.Segmented)
        {
            Options = [new("default", "Default"), new("immediate", "Immediate"), new("fade", "Fade")],
            Default = "default",
            Description = "Used when \"Hold\" is released or \"Toggle\" stops the sound",
        },
    ];

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        Task.FromResult(sourceId == "sounds" ? SoundOptionsSource.GetSounds(engine) : new OptionsResult([], $"Unknown options source: {sourceId}"));

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var soundId = settings["sound"]?.GetValue<string>();
        if (string.IsNullOrEmpty(soundId)) return Task.CompletedTask;

        var playMode = settings["playMode"]?.GetValue<string>() ?? "full";
        if (playMode == "toggle" && engine.IsPlaying(soundId))
        {
            var stopStyle = settings["stopStyle"]?.GetValue<string>() ?? "default";
            engine.Stop(new VoiceFilter(SoundId: soundId, IsPreview: false), stopStyle);
            return Task.CompletedTask;
        }

        if (engine.Settings.OverlapMode == "cut")
            engine.StopAll(engine.Settings.StopStyle);

        engine.Play(soundId, context);
        return Task.CompletedTask;
    }

    /// <summary>Only in "hold" mode: stops the voices this exact widget (on this exact device/page) started.</summary>
    public Task ReleaseAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        if ((settings["playMode"]?.GetValue<string>() ?? "full") != "hold") return Task.CompletedTask;

        var soundId = settings["sound"]?.GetValue<string>();
        if (string.IsNullOrEmpty(soundId)) return Task.CompletedTask;

        var stopStyle = settings["stopStyle"]?.GetValue<string>() ?? "default";
        engine.Stop(new VoiceFilter(SoundId: soundId, DeviceId: context.DeviceId, PageId: context.PageId, WidgetId: context.WidgetId, IsPreview: false), stopStyle);
        return Task.CompletedTask;
    }
}

/// <summary>Stops one sound or every sound. Settings: <c>target</c> (all/one), <c>sound</c> (id, only for
/// "one"), <c>stopStyle</c> (default/immediate/fade).</summary>
public sealed class SoundStopAction(SoundEngine engine) : IActionHandler, IActionDescriptor, IOptionsSource
{
    public const string TypeId = "sound.stop";
    public string Type => TypeId;
    public string DisplayName => "Sound: Stop";
    public string Category => "Sound";
    public string? Description => null;
    public string? Icon => "square";

    public IReadOnlyList<SettingField> Fields =>
    [
        new("target", "Stop", SettingFieldKind.Segmented)
        {
            Options = [new("all", "All sounds"), new("one", "One sound")],
            Default = "all",
        },
        new("sound", "Sound", SettingFieldKind.Select) { OptionsSource = "sounds", VisibleWhen = "target=one" },
        new("stopStyle", "Stop style", SettingFieldKind.Segmented)
        {
            Options = [new("default", "Default"), new("immediate", "Immediate"), new("fade", "Fade")],
            Default = "default",
        },
    ];

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        Task.FromResult(sourceId == "sounds" ? SoundOptionsSource.GetSounds(engine) : new OptionsResult([], $"Unknown options source: {sourceId}"));

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var stopStyle = settings["stopStyle"]?.GetValue<string>() ?? "default";
        var target = settings["target"]?.GetValue<string>() ?? "all";

        if (target == "one")
        {
            var soundId = settings["sound"]?.GetValue<string>();
            if (string.IsNullOrEmpty(soundId)) return Task.CompletedTask;
            engine.Stop(new VoiceFilter(SoundId: soundId, IsPreview: false), stopStyle);
        }
        else
        {
            engine.StopAll(stopStyle);
        }
        return Task.CompletedTask;
    }
}

/// <summary>Sets the master volume. Settings: <c>mode</c> (set/adjust/slider), <c>value</c> (0-100, for
/// "set"), <c>step</c> (±, for "adjust"). "slider" reads the widget's live dragged value
/// (<see cref="ActionContext.Value"/>), same pattern as <c>core.setVolume</c>.</summary>
public sealed class SoundSetMasterVolumeAction(SoundEngine engine) : IActionHandler, IActionDescriptor
{
    public const string TypeId = "sound.setMasterVolume";
    public string Type => TypeId;
    public string DisplayName => "Sound: Master volume";
    public string Category => "Sound";
    public string? Description => null;
    public string? Icon => "volume-2";

    public IReadOnlyList<SettingField> Fields =>
    [
        new("mode", "Mode", SettingFieldKind.Segmented)
        {
            Options = [new("set", "Set"), new("adjust", "Adjust"), new("slider", "Slider/knob")],
            Default = "set",
        },
        new("value", "Volume (%)", SettingFieldKind.Slider) { Min = 0, Max = 100, Step = 1, Default = 100, VisibleWhen = "mode=set" },
        new("step", "Step (%, negative = lower)", SettingFieldKind.Number) { Step = 1, Default = 10, VisibleWhen = "mode=adjust" },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var mode = settings["mode"]?.GetValue<string>() ?? "set";
        var percent = mode switch
        {
            "slider" => context.Value ?? engine.GetMasterVolume(),
            "adjust" => engine.GetMasterVolume() + (settings["step"]?.GetValue<double>() ?? 0),
            _ => settings["value"]?.GetValue<double>() ?? 100,
        };
        engine.SetMasterVolume(percent);
        return Task.CompletedTask;
    }
}
