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
