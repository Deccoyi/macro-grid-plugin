using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Scenes, transitions, studio mode and profiles.

/// <summary>Shared by the program and preview scene actions: pick a scene, send one request with its name.</summary>
public abstract class ObsSceneAction(ObsConnection obs, string requestType) : ObsOptionsAction(obs)
{
    public override IReadOnlyList<SettingField> Fields => [new("sceneName", "Scene", SettingFieldKind.Select) { OptionsSource = "scenes" }];

    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var sceneName = GetString(settings, "sceneName");
        if (string.IsNullOrEmpty(sceneName)) return Task.CompletedTask;
        ObsTargetCheck.RequireScene(Obs, sceneName);
        return Obs.RequestAsync(requestType, new JsonObject { ["sceneName"] = sceneName }, cancellationToken);
    }
}

/// <summary>Shared by the transition and profile actions: pick a name from a dropdown, send one request with it.</summary>
public abstract class ObsNamedChoiceAction(ObsConnection obs, string settingKey, string requestType) : ObsOptionsAction(obs)
{
    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var name = GetString(settings, settingKey);
        return string.IsNullOrEmpty(name) ? Task.CompletedTask : Obs.RequestAsync(requestType, new JsonObject { [settingKey] = name }, cancellationToken);
    }
}

public sealed class ObsSetSceneAction(ObsConnection obs) : ObsSceneAction(obs, "SetCurrentProgramScene")
{
    public const string TypeId = "obs.setScene";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Switch scene";
    public override string? Description => "Changes the program scene";
    public override string? Icon => "clapperboard";

    public static JsonObject Settings(string sceneName) => new() { ["sceneName"] = sceneName };
}

public sealed class ObsSetPreviewSceneAction(ObsConnection obs) : ObsSceneAction(obs, "SetCurrentPreviewScene")
{
    public const string TypeId = "obs.setPreviewScene";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Preview scene";
    public override string? Description => "Changes the preview scene in studio mode";
    public override string? Icon => "monitor-play";
}

public sealed class ObsStudioTransitionAction(ObsConnection obs) : ObsRequestAction(obs, "TriggerStudioModeTransition")
{
    public const string TypeId = "obs.studioTransition";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Trigger studio transition";
    public override string? Description => "Sends the preview to the program (studio mode)";
    public override string? Icon => "arrow-left-right";
}

public sealed class ObsToggleStudioModeAction(ObsConnection obs) : ObsActionBase(obs)
{
    public const string TypeId = "obs.toggleStudioMode";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Toggle studio mode";
    public override string? Icon => "columns-2";

    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken) =>
        Obs.RequestAsync("SetStudioModeEnabled", new JsonObject { ["studioModeEnabled"] = !Obs.Cache.StudioMode }, cancellationToken);
}

public sealed class ObsSetTransitionAction(ObsConnection obs) : ObsNamedChoiceAction(obs, "transitionName", "SetCurrentSceneTransition")
{
    public const string TypeId = "obs.setTransition";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Transition type";
    public override string? Description => "Changes the active scene transition";
    public override string? Icon => "shuffle";
    public override IReadOnlyList<SettingField> Fields => [new("transitionName", "Transition", SettingFieldKind.Select) { OptionsSource = "transitions" }];
}

public sealed class ObsSetProfileAction(ObsConnection obs) : ObsNamedChoiceAction(obs, "profileName", "SetCurrentProfile")
{
    public const string TypeId = "obs.setProfile";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Switch profile";
    public override string? Description => "Changes the OBS profile";
    public override string? Icon => "user-cog";
    public override IReadOnlyList<SettingField> Fields => [new("profileName", "Profile", SettingFieldKind.Select) { OptionsSource = "profiles" }];
}
