using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace HelloCSharp;

// #region action
public sealed class GreetAction(IPluginHost host, HelloSettingsPage settings, GreetingCounter counter, IPluginStatusItem status)
    : IActionHandler, IActionDescriptor
{
    public string Type => "hellocsharp.greet";
    public string DisplayName => "Greet";

    // IActionDescriptor: how the editor lists the action and draws its form.
    public string Category => "Hello";
    public string? Description => "Logs a greeting and counts it in the hellocsharp.count variable.";
    public string? Icon => "hand";

    public IReadOnlyList<SettingField> Fields =>
    [
        new("times", "Times", SettingFieldKind.Number) { Min = 1, Max = 10, Step = 1, Default = 1 },
        new("note", "Note", SettingFieldKind.Text) { Placeholder = "Optional", AllowVariables = true },
    ];

    public Task ExecuteAsync(ActionContext context, JsonObject actionSettings, CancellationToken cancellationToken)
    {
        var times = actionSettings["times"]?.GetValue<int>() ?? 1;
        var greeting = settings.Current.Greeting;
        if (settings.Current.Loud) greeting = greeting.ToUpperInvariant();

        for (var i = 0; i < times; i++) counter.Increment();

        host.Log($"{greeting}, {settings.Current.Name}! (pressed on device {context.DeviceId})");
        status.Update($"Hello: {counter.Count} greetings", StatusLevel.Ok);
        return Task.CompletedTask;
    }
}
// #endregion action
