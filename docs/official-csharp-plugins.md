# Official C# plugins (maintainer guide)

Only the official plugins in this repository are written in C#. A C# plugin runs inside the server process with full .NET access and
Macro Grid cannot limit what it does, so the server loads a C# plugin only when it carries a valid signature of the official plugin key,
checked every time it loads (see `PluginTrustVerifier` in the server repository). Plugins by other authors are JavaScript plugins; the
public site and the tutorials describe those only. This document is for the maintainer: how an official C# plugin is written, built and
signed, and the SDK it compiles against. It is not published on the site.

## Signing an official release

`scripts/release-plugin.ps1` builds the plugin, adds the licence files, and writes `signature.json` and `signature.sig` into the package
root (`scripts/sign-package-contents.cs`): `signature.json` lists the plugin's `id`, `version` and `kind` and the SHA-256 of every file,
and `signature.sig` is the signature over its exact bytes made with the plugin-signing key that stays on the maintainer's PC. It then
zips the folder and signs the zip as before (the zip signature protects the download; `signature.json` protects the installed folder).
The steps, the key and the order of a release are in [release.md](release.md) and in the server repository's release guide.

A development build of the server (built from source, Debug configuration) also loads an unsigned C# plugin, for local work. A released
server never does.

The rest of this document is the C# material that used to be in the public guides.

## Writing a C# plugin

### Project setup

Reference the SDK package and copy `plugin.json` into the build output, so the output folder is directly installable.
The SDK is published on NuGet as `MacroGrid.Plugin.Abstractions` (use the version that matches the server's SDK version,
see the compatibility notes). Developers who work on the server and a plugin at the same time can switch to the sibling
project instead, see [using-the-sdk-package.md](using-the-sdk-package.md).

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>MyCompany.Plugin.Ping</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MacroGrid.Plugin.Abstractions" Version="1.0.0"
                      PrivateAssets="all" ExcludeAssets="runtime" />
  </ItemGroup>
  <ItemGroup>
    <None Include="..\plugin.json" Link="plugin.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

`ExcludeAssets="runtime"` keeps a copy of the SDK out of your output: the server and every plugin must share
the server's copy of `MacroGrid.Plugin.Abstractions` (otherwise `is IActionHandler` checks fail because the same type from
two copies is two different types). Never ship your own copy.

### The entry point

The server finds exactly one class implementing `IPlugin` in the entry assembly, creates it with a parameterless
constructor and calls `Initialize` once. Everything you register there is applied when `Initialize` returns.

```csharp
using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

public sealed class PingPlugin : IPlugin
{
    public void Initialize(IPluginHost host)
    {
        host.RegisterAction(new PingAction());
    }
}

public sealed class PingAction : IActionHandler
{
    public string Type => "ping.ping";            // unique: "<plugin id>.<name>" by convention
    public string DisplayName => "Ping";

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken)
    {
        // context.DeviceId / PageId / WidgetId say what was pressed; context.Value is the slider or knob value
        // for a value-change event; context.Device navigates pages and profiles of that one phone.
        return Task.CompletedTask;
    }
}
```

`IPluginHost` gives you:

| Member | Purpose |
|---|---|
| `ServerVersion`, `SdkVersion` | The running server's and SDK's versions. |
| `DataDirectory` | The plugin's own install folder (`%AppData%\MacroGrid\plugins\<id>\`), writable. Keep your files here. |
| `Log(message)` | Writes a line to the server's plugin log, prefixed with your id. Use it for rare events (connection changes, errors), not for polling. |
| `RegisterAction(handler)` | Adds an action type. |
| `RegisterVariableProvider(provider)` | Adds a background source of variables. If the same object implements `IVariableCatalogSource` it is also listed in the editor's variable picker. |
| `RegisterSettingsPage(page)` | Adds a settings form to the Plugins window. |
| `CreateStatusItem(id)` | Creates an entry you own in the editor's status bar. |
| `RegisterIconPack(pack)` | Adds icons to the editor's icon picker. |

If `Initialize` throws, the server catches it, shows the plugin as *Error* and keeps running.

An action's exceptions are caught by the server too: a failing action is logged and its message is shown on the phone and
in the editor's status bar, so throw a clear message instead of failing silently. Actions of one phone run one after the
other; a slow action delays that phone's next action, not other phones.

### Action forms (`IActionDescriptor`, `SettingField`)

If the handler also implements `IActionDescriptor`, the editor lists it under `Category` with its `Description` and
`Icon` (a Lucide icon name) and draws its settings form from `Fields`. No React code is needed:

```csharp
public sealed class SetSceneAction : IActionHandler, IActionDescriptor, IOptionsSource
{
    public string Type => "myplugin.setScene";
    public string DisplayName => "Set scene";
    public string Category => "My plugin";
    public string? Description => "Switches to a scene.";
    public string? Icon => "clapperboard";

    public IReadOnlyList<SettingField> Fields =>
    [
        new("scene", "Scene", SettingFieldKind.Select) { OptionsSource = "scenes" },
        new("volume", "Volume (%)", SettingFieldKind.Slider) { Min = 0, Max = 100, Default = 100 },
        new("text", "Label", SettingFieldKind.Text) { AllowVariables = true },
    ];

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken ct) =>
        Task.FromResult(sourceId == "scenes"
            ? new OptionsResult([new SettingOption("main", "Main"), new SettingOption("brb", "Be right back")])
            : new OptionsResult([], "Unknown source"));

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken ct) { /* ... */ return Task.CompletedTask; }
}
```

`SettingFieldKind` is `Text`, `Password`, `Number`, `Slider`, `Bool`, `Select` or `Segmented`. Useful `SettingField` options:
`Description`, `Placeholder`, `Default`, `Min`/`Max`/`Step`, `Options` (a fixed `SettingOption[]`), `OptionsSource` (a dynamic
list served by `IOptionsSource`), `DependsOn` (keys whose current form values are passed to `GetOptionsAsync`),
`AllowVariables` (lets the user insert `{variables}`; you receive the raw template and resolve it yourself),
`VisibleWhen`. If `OptionsResult.Error` is set the editor shows that message and an empty list.

### Settings page

Implement `IPluginSettingsPage` (`Fields`, `Load()`, `Save(values)`) and register it with `host.RegisterSettingsPage`.
The editor draws the form from `Fields`; `Load` and `Save` are your bridge to disk (usually a `settings.json` in
`DataDirectory`). A page that also implements `IOptionsSource` can serve dynamic dropdowns. A plugin with a settings page
gets a gear button in the Plugins window and its status item opens the page. Example: `ObsSettingsPage` in `WebSocketBridgeForOBS/src/ObsSettings.cs`.

### Variables

Implement `IVariableProvider.RunAsync(IVariableStore store, CancellationToken ct)`: it runs for as long as the plugin is
loaded and should return only when `ct` is cancelled. Call `store.Set(name, value)` whenever a value changes (a value equal
to the current one is ignored). If `RunAsync` throws, the server logs it and restarts the provider after 5 seconds; a provider
that returns normally is not restarted.

- Widgets read variables in text as `{obs.stream.duration}` and can format them: `{system.cpu|0}%`, `{system.time|HH:mm}`.
- For a name that depends on user data (an OBS input that can be deleted or renamed) call `store.Remove(name)` when it disappears,
  otherwise it stays in the store and the variable picker forever.
- Implement `IVariableCatalogSource.Describe()` on the same object to list your variables (`VariableInfo`: name, description, example
  and a category the picker groups by) in the editor's picker, so users do not have to guess names.
- When the plugin is unloaded, every variable it set is removed automatically.

### Languages (`defaultLanguage` and `locales/`)

Write every text the user sees (action names and descriptions, category names, variable descriptions, form labels, option labels,
status texts) in one language, the plugin's *default language*, and say which one it is with `"defaultLanguage": "en"` in `plugin.json`
(when the field is missing the default language is English). To offer another language, put `locales/<language>.json` next to
`plugin.json`, for example `locales/tr.json`: one JSON object that maps the default-language text to its translation.

```json
{
  "Not connected to OBS": "OBS'e bağlı değil",
  "Audio source": "Ses kaynağı"
}
```

The server translates these texts when it hands them to the editor, using the language the person chose in Preferences (Turkish or English
today). If the language has no file, or a text has no entry, the text is shown as the plugin wrote it, so a plugin never shows an empty label
and works without any locale file. This works the same for C# and JavaScript plugins. Texts that are put together at run time (for example
`$"{scene} - visible"`) cannot be looked up and stay in the default language. Make the locale files travel with the build output
(`<None Include="..\locales\*.json" Link="locales\%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />`).

### Status bar item

`var item = host.CreateStatusItem("connection"); item.Update("Connected", StatusLevel.Ok);` shows a colored entry on the right of
the editor's status bar (`Idle`, `Ok`, `Busy`, `Warning`, `Error`, optionally an icon and a tooltip). Update it when the state
changes, not at a high rate. Clicking it opens the plugin's settings page, if it has one.

### Icon packs

Implement `IIconPackSource` (`Id`, `DisplayName`, `IconNames`, `GetIconSvg(name)`) and call `host.RegisterIconPack`. The pack
appears as its own category in the icon picker. Use `stroke="currentColor"` in the SVGs: the editor colors the icon by setting
`color` on the root `<svg>`. Embedding the SVGs in the DLL (`<EmbeddedResource Include="icons\*.svg" />`) is the simplest.
Example: [PLCIcons/](../PLCIcons/).

### Talking to a phone

`ActionContext.Device` (`IDeviceController`) navigates the one phone that triggered the action: `ShowPageAsync`, `NextPageAsync`,
`PreviousPageAsync`, `BackAsync`, `SwitchProfileAsync`.

## The C# SDK interfaces


The SDK is the NuGet package `MacroGrid.Plugin.Abstractions` (namespace `MacroGrid.Plugin.Abstractions`, version `1.2.0` at the time of writing, the same number as Macro Grid, exposed at
run time as `PluginSdk.Version`). The signatures below are those of the SDK source in the
[server repository](https://github.com/Deccoyi/macro-grid/tree/main/src/MacroGrid.Plugin.Abstractions). Within one MAJOR the SDK only grows: members are added, never removed or changed (see [Compatibility](/basics/compatibility)).

### Entry point

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
    IPluginSecrets Secrets { get; }
}

public interface IPluginSecrets
{
    string Protect(string secret);
    string? Unprotect(string protectedSecret);
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
| `Secrets` | `Protect` encrypts a secret (for example a password) before you write it to your own settings file, `Unprotect` reads it back. It uses the server's own protection (DPAPI for the current Windows user): a copied data folder no longer carries a usable secret. Optional: plugins that store plain JSON keep working. |

### Actions

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

```csharp
public interface IReleaseAwareAction   // optional, on the same class as IActionHandler
{
    Task ReleaseAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken);
}
```

An action that implements `IReleaseAwareAction` is told when the button it was pressed with is let go: the server calls `ReleaseAsync`, with the
settings the `press` binding ran with, when the widget's `release` event fires, before that event's own bindings run. Use it for "play while held" or
"hold to talk".

An action's exceptions are caught by the server: the failure is logged and its message is shown on the phone and in the editor's
status bar. Actions of one phone run one after the other.

### Forms

```csharp
public enum SettingFieldKind { Text, Password, Number, Slider, Bool, Select, Segmented, File, List, Button, Notice }

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
    public string? FileFilter { get; init; }         // File: a WinForms file filter
    public SettingField[]? ItemFields { get; init; } // List: the schema of one row
    public string? Command { get; init; }            // Button: the command id
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

public interface ISettingsCommandHandler   // optional, on the same class as IPluginSettingsPage
{
    Task<string?> RunCommandAsync(string command, JsonObject values, CancellationToken cancellationToken);
}
```

See [Settings pages](/guides/settings-pages) for what each option does.

### Variables and status

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

public enum VariableType { Text, Number, Boolean, Duration, DateTime }

public sealed record VariableInfo(string Name, string Description, string Example, string Category)
{
    public VariableType Type { get; init; } = VariableType.Text;   // what the live value is
    public string? Unit { get; init; }                             // unit of a number, such as "%"
    public IReadOnlyList<string>? Values { get; init; }            // allowed values of a fixed-choice text variable
}

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

### Icon packs

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

### Services the server implements

`IInputService` (`SendKeyCombo(KeyCombo)`, `TypeText(string)`) and `IAudioService` (`GetMasterVolume`, `SetMasterVolume`, `GetMuted`,
`SetMuted`) are part of the SDK assembly for the server's own actions. A plugin is not handed them through `IPluginHost` today.

### Manifest types

`PluginManifest` (a record with `Id`, `Name`, `Version`, `MacroGrid`, `SdkVersion` and `MinServerVersion` (legacy), `Entry`, `Kind`, `Permissions`) and
`PluginKind` (`Csharp`, `Js`) mirror [`plugin.json`](/reference/manifest).

### Disposal

If your plugin instance or anything you registered implements `IDisposable` or `IAsyncDisposable`, the server disposes it on unload
after cancelling your `RunAsync` token and waiting up to 5 seconds. See the [lifecycle](/basics/#lifecycle).

## Using the SDK package


C# plugins compile against `MacroGrid.Plugin.Abstractions`. There are two ways to get it; the plugin projects in this
repository support both.

### Default: NuGet package

Nothing to set up. `dotnet build` restores `MacroGrid.Plugin.Abstractions` (version in `MacroGridSdkVersion`,
`Directory.Build.props`) from nuget.org, so a clean clone of this repo builds on its own. `nuget.config` lists nuget.org only.

In your own plugin project:

```xml
<PackageReference Include="MacroGrid.Plugin.Abstractions" Version="1.0.0"
                  PrivateAssets="all" ExcludeAssets="runtime" />
```

`ExcludeAssets="runtime"` keeps the SDK dll out of the plugin's output. The server and all plugins share the server's copy
of that assembly; shipping your own makes type checks like `is IActionHandler` fail. Test projects are the exception: they
need the dll at runtime, so they reference the package without `ExcludeAssets`.

### Working on both repos: local SDK

If you have the server repo checked out next to this one (`..\macro-grid`) and are changing the SDK and a plugin together,
build against the sibling source instead of the package:

```powershell
dotnet build WebSocketBridgeForOBS/src -p:UseLocalSdk=true
dotnet test WebSocketBridgeForOBS/tests/MacroGrid.Plugin.Obs.Tests -p:UseLocalSdk=true
```

`UseLocalSdk` defaults to `false`. To make it stick on your machine, pass it from an environment variable
(`$env:UseLocalSdk = "true"`), and do not commit a changed default.

### Testing an unpublished SDK build

Pack the SDK into a folder and add that folder as a source:

```powershell
dotnet pack ..\macro-grid\src\MacroGrid.Plugin.Abstractions -c Release -o C:\local-feed
dotnet build WebSocketBridgeForOBS/src -p:RestoreSources=C:\local-feed
```

Alternatively uncomment the `local-sdk` line in `nuget.config` (do not commit that).

### Versions

Package version = the Macro Grid version = `PluginSdk.Version` in the server repo (one number for both). A plugin's `plugin.json` says
`"minMacroGrid": "1.0.0"`: the oldest Macro Grid it runs on. The build checks that it has the same MAJOR as `MacroGridSdkVersion` and is not
newer than it; the minor and patch may be lower, which lets the plugin run on more servers. When you start to use something added in a newer
MINOR, bump `MacroGridSdkVersion` in `Directory.Build.props` and raise `minMacroGrid` in that plugin's `plugin.json` to match. After a MAJOR
release of Macro Grid, every plugin is rebuilt against it.

