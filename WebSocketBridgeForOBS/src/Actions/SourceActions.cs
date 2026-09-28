using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Scene items and text sources.

/// <summary>Shows/hides/toggles a scene item, including one nested inside a group. Settings:
/// { "sceneName": string, "sourceName": string, "parentGroup"?: string, "mode": "show"|"hide"|"toggle" }</summary>
public sealed class ObsSetItemVisibilityAction(ObsConnection obs) : ObsOptionsAction(obs)
{
    public const string TypeId = "obs.setItemVisibility";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Item visibility";
    public override string? Description => "Shows/hides a scene item (groups included)";
    public override string? Icon => "eye";
    public override IReadOnlyList<SettingField> Fields =>
    [
        new("sceneName", "Scene", SettingFieldKind.Select) { OptionsSource = "scenes" },
        new("sourceName", "Item", SettingFieldKind.Select) { OptionsSource = "sceneItems", DependsOn = ["sceneName"] },
        new("mode", "Mode", SettingFieldKind.Segmented) { Options = [new("show", "Show"), new("hide", "Hide"), new("toggle", "Toggle")], Default = "toggle" },
    ];

    public override async Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var sceneName = GetString(settings, "sceneName");
        var sourceName = GetString(settings, "sourceName");
        if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sourceName)) return;

        var item = Obs.Cache.SceneItems(sceneName).FirstOrDefault(i => i.SourceName == sourceName)
            ?? throw new InvalidOperationException($"Item '{sourceName}' is no longer in scene '{sceneName}'.");

        var container = item.ParentGroup ?? sceneName;
        var idResponse = await Obs.RequestAsync("GetSceneItemId", new JsonObject { ["sceneName"] = container, ["sourceName"] = sourceName }, cancellationToken);
        var itemId = idResponse["sceneItemId"]?.GetValue<int>() ?? item.SceneItemId;

        var mode = GetString(settings, "mode") ?? "toggle";
        var enabled = mode switch { "show" => true, "hide" => false, _ => !item.Enabled };
        await Obs.RequestAsync("SetSceneItemEnabled", new JsonObject { ["sceneName"] = container, ["sceneItemId"] = itemId, ["sceneItemEnabled"] = enabled }, cancellationToken);
    }

    public static JsonObject Settings(string sceneName, string sourceName, string mode) =>
        new() { ["sceneName"] = sceneName, ["sourceName"] = sourceName, ["mode"] = mode };
}

/// <summary>Sets a text source's content. Settings: { "sourceName": string, "text": string } — text supports
/// {variable} templates (see AllowVariables), resolved by the host before ExecuteAsync is called.</summary>
public sealed class ObsSetTextAction(ObsConnection obs) : ObsOptionsAction(obs)
{
    public const string TypeId = "obs.setText";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Set text source";
    public override string? Icon => "type";
    public override IReadOnlyList<SettingField> Fields =>
    [
        new("sourceName", "Text source", SettingFieldKind.Select) { OptionsSource = "textInputs" },
        new("text", "Text", SettingFieldKind.Text) { AllowVariables = true },
    ];

    public override Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        var sourceName = GetString(settings, "sourceName");
        if (string.IsNullOrEmpty(sourceName)) return Task.CompletedTask;
        var text = GetString(settings, "text") ?? "";
        return Obs.RequestAsync("SetInputSettings", new JsonObject
        {
            ["inputName"] = sourceName,
            ["inputSettings"] = new JsonObject { ["text"] = text },
        }, cancellationToken);
    }
}
