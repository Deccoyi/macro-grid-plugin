# C# SDK interfaces

The SDK is the NuGet package `MacroGrid.Plugin.Abstractions` (namespace `MacroGrid.Plugin.Abstractions`, version `1.0.0`, the same number as Macro Grid, exposed at
run time as `PluginSdk.Version`). The signatures below are those of the SDK source in the
[server repository](https://github.com/Deccoyi/macro-grid/tree/main/src/MacroGrid.Plugin.Abstractions). The SDK is `0.x`: a minor
version may change them.

## Entry point

```csharp
public interface IPlugin
{
    void Initialize(IPluginHost host);
}
```

The server finds exactly one implementation in the entry assembly, creates it with a parameterless constructor and calls
`Initialize` once. Registrations are applied after `Initialize` returns.

```csharp
public interface IPluginHost
{
    string ServerVersion { get; }
    string SdkVersion { get; }
    string DataDirectory { get; }
    void Log(string message);
    void RegisterAction(IActionHandler handler);
    void RegisterVariableProvider(IVariableProvider provider);
    void RegisterSettingsPage(IPluginSettingsPage page);
    IPluginStatusItem CreateStatusItem(string id);
    void RegisterIconPack(IIconPackSource iconPack);
}
```

| Member | Purpose |
|---|---|
| `ServerVersion`, `SdkVersion` | The running server's and SDK's versions. |
| `DataDirectory` | The plugin's own install folder (`%AppData%\MacroGrid\plugins\<id>\`), writable. Keep your files here. |
| `Log(message)` | A line in the server's plugin log, prefixed with your id. Use it for rare events, not polling. |
| `RegisterAction` | Adds an action type. |
| `RegisterVariableProvider` | Adds a background source of variables. If the same object implements `IVariableCatalogSource` it is also listed in the variable picker. |
| `RegisterSettingsPage` | Adds a settings form to the Plugins window. |
| `CreateStatusItem(id)` | Creates an entry you own in the editor's status bar. Call once per logical status and reuse it. |
| `RegisterIconPack` | Adds icons to the editor's icon picker. |

## Actions

```csharp
public interface IActionHandler
{
    string Type { get; }          // unique, "<plugin id>.<name>" by convention
    string DisplayName { get; }
    Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken);
}

public sealed record ActionContext(string DeviceId, string PageId, string WidgetId, IDeviceController Device, double? Value = null);
```

`Value` is only set for a `valueChange` dispatch (a slider or knob drag commit). A widget's events are `press`, `release`,
`longPress`, `doubleTap`, `toggleOn`, `toggleOff` and `valueChange`.

```csharp
public interface IActionDescriptor      // optional, on the same class as IActionHandler
{
    string Category { get; }
    string? Description { get; }
    string? Icon { get; }               // a Lucide icon name
    IReadOnlyList<SettingField> Fields { get; }
}

public interface IDeviceController      // ActionContext.Device: the one phone that triggered the action
{
    Task ShowPageAsync(string pageId);
    Task NextPageAsync();
    Task PreviousPageAsync();
    Task BackAsync();
    Task SwitchProfileAsync(string profileId);
}
```

An action's exceptions are caught by the server: the failure is logged and its message is shown on the phone and in the editor's
status bar. Actions of one phone run one after the other.

## Forms

```csharp
public enum SettingFieldKind { Text, Password, Number, Slider, Bool, Select, Segmented }

public sealed record SettingField(string Key, string Label, SettingFieldKind Kind)
{
    public string? Description { get; init; }
    public string? Placeholder { get; init; }
    public JsonNode? Default { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Step { get; init; }
    public SettingOption[]? Options { get; init; }
    public string? OptionsSource { get; init; }
    public string[]? DependsOn { get; init; }
    public bool AllowVariables { get; init; }
    public string? VisibleWhen { get; init; }
}

public sealed record SettingOption(string Value, string Label, string? Group = null, string? Icon = null);
public sealed record OptionsResult(IReadOnlyList<SettingOption> Options, string? Error = null);

public interface IOptionsSource
{
    Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken);
}

public interface IPluginSettingsPage
{
    IReadOnlyList<SettingField> Fields { get; }
    JsonObject Load();
    void Save(JsonObject values);
}
```

See [Settings pages](/guides/settings-pages) for what each option does.

## Variables and status

```csharp
public interface IVariableStore
{
    void Set(string name, object? value);
    object? Get(string name);
    void Remove(string name);
}

public interface IVariableProvider
{
    Task RunAsync(IVariableStore store, CancellationToken cancellationToken);
}

public sealed record VariableInfo(string Name, string Description, string Example, string Category);

public interface IVariableCatalogSource
{
    IEnumerable<VariableInfo> Describe();
}

public enum StatusLevel { Idle, Ok, Busy, Warning, Error }

public interface IPluginStatusItem
{
    void Update(string text, StatusLevel level, string? icon = null, string? tooltip = null);
}
```

`RunAsync` runs for as long as the plugin is loaded and should return only when the token is cancelled. If it throws, the server
logs it and restarts the provider after 5 seconds; a provider that returns normally is not restarted. A value equal to the current
one is ignored by `Set`. Every variable a plugin set is removed when it is unloaded. See [Tutorial 3](/tutorials/live-data).

## Icon packs

```csharp
public interface IIconPackSource
{
    string Id { get; }
    string DisplayName { get; }
    IReadOnlyList<string> IconNames { get; }
    string? GetIconSvg(string name);
}
```

See [Icon packs](/guides/icon-packs).

## Services the server implements

`IInputService` (`SendKeyCombo(KeyCombo)`, `TypeText(string)`) and `IAudioService` (`GetMasterVolume`, `SetMasterVolume`, `GetMuted`,
`SetMuted`) are part of the SDK assembly for the server's own actions. A plugin is not handed them through `IPluginHost` today.

## Manifest types

`PluginManifest` (a record with `Id`, `Name`, `Version`, `MacroGrid`, `SdkVersion` and `MinServerVersion` (legacy), `Entry`, `Kind`, `Permissions`) and
`PluginKind` (`Csharp`, `Js`) mirror [`plugin.json`](/reference/manifest).

## Disposal

If your plugin instance or anything you registered implements `IDisposable` or `IAsyncDisposable`, the server disposes it on unload
after cancelling your `RunAsync` token and waiting up to 5 seconds. See the [lifecycle](/basics/#lifecycle).
