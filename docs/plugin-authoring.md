# Writing a plugin

A plugin adds actions (things a widget can do), variables (live values a widget can show) and small extras (a settings
page, status bar items, icon packs) to the Macro Grid server. There are two kinds:

| | C# plugin | JavaScript plugin |
|---|---|---|
| `kind` in `plugin.json` | `"csharp"` | `"js"` |
| Trust | Full trust. It runs inside the server process with full .NET access, isolated only so that it cannot break other plugins. | Sandboxed. No .NET access, only a small `host` object, and only the permissions the user approved. |
| Good for | Real integrations (a websocket client, a device driver) | Small scripts (poll a local HTTP API, publish a variable, add an action) |
| Needs a build | Yes (a DLL) | No (one script) |

Only install C# plugins you trust: they can do anything the server can do.

The working examples in this repository are [OBS/](../OBS/) (a full C# integration), [PLCIcons/](../PLCIcons/) (a C#
icon pack) and [HelloJs/](../HelloJs/) (a small JavaScript plugin). Read [CONTRIBUTING.md](../CONTRIBUTING.md) for the
rules that apply to every plugin in this repository (independent versioning, isolation, changelogs).

## 1. Folder and installation

The server looks for plugins in `%AppData%\MacroGrid\plugins\<folder>\`. A folder is a plugin if it contains a
`plugin.json`. The folder name does not matter; the identity is the `id` in the manifest.

Install from the editor: **Plugins → Manage Plugins… → Install from Folder…** and pick a folder that contains
`plugin.json` (for a C# plugin, the build output folder such as `src\bin\Debug\net10.0\`). The folder is copied to
`plugins\<id>\` and loaded immediately, with no restart. Installing a folder whose `id` is already installed replaces
that plugin (files the plugin wrote into its own folder, such as `settings.json`, are kept). The same window can reload
and remove a plugin.

A plugin is loaded, reloaded and unloaded while the server runs. See [Lifecycle](#6-lifecycle).

## 2. `plugin.json`

```json
{
  "id": "obs",
  "name": "OBS Control",
  "version": "0.2.0",
  "sdkVersion": "^0.3.0",
  "minServerVersion": "0.1.0",
  "entry": "MacroGrid.Plugin.Obs.dll",
  "kind": "csharp",
  "permissions": null
}
```

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | Unique, stable id. Used in the folder name, in action types and variable names, and for approvals. The server refuses a second plugin with the same id. |
| `name` | yes | Display name in the Plugins window. |
| `version` | yes | The plugin's own semantic version, independent of the server's. |
| `sdkVersion` | yes | The plugin SDK range the plugin was built against, a caret range such as `^0.3.0`. While the SDK is `0.x`, `^0.3.0` matches only `0.3.x`. If the server's SDK does not satisfy it the plugin is listed as *Incompatible* and not loaded. |
| `minServerVersion` | yes | The oldest server version the plugin needs. A older server lists the plugin as *Incompatible*. |
| `entry` | yes | C#: the entry DLL's file name. JavaScript: the script (usually `index.js`). |
| `kind` | yes | `"csharp"` or `"js"`. |
| `defaultLanguage` | no | The language the plugin's own texts are written in, such as `"en"` (the default). See [Languages](#languages-defaultlanguage-and-locales). |
| `permissions` | no | JavaScript only: the permissions the script needs (see [JavaScript plugins](#7-javascript-plugins)). |

The SDK version is `PluginSdk.Version` in `MacroGrid.Plugin.Abstractions`; see the server repository's
`docs/versioning.md` for what counts as a breaking change.

## 3. Status of a plugin in the editor


The Plugins window lists every folder that has a `plugin.json`:

- **Loaded**: running, its actions and variables are available.
- **Incompatible**: `sdkVersion` or `minServerVersion` is not satisfied by this server.
- **Needs approval**: a JavaScript plugin whose declared permissions the user has not approved yet. It does not run until they do.
- **Error**: `plugin.json` could not be parsed, the entry file is missing, the id is already used by another installed plugin, an action type is already registered, a permission is unknown, `Initialize` (or the script's first run) failed, or a JavaScript plugin was switched off after failing repeatedly. The message is shown under the name. **Reload** tries again.

## 4. Writing a C# plugin

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
    <PackageReference Include="MacroGrid.Plugin.Abstractions" Version="0.3.1"
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
gets a gear button in the Plugins window and its status item opens the page. Example: `ObsSettingsPage` in `OBS/src/ObsSettings.cs`.

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

## 5. Actions, widgets and dynamic values in one picture

A widget's events (press, release, long press, double tap, toggle on/off, value change) each run a list of actions in order.
An action's settings are the values of its `Fields` form. Variables flow the other way: providers publish values, and widgets
show them in text or use them in conditional rules (color, text, icon, animation). See the server repository's README for the
user's side of this.

## 6. Lifecycle

Plugins are loaded when the server starts and can be installed, reloaded and removed at any time from the Plugins window,
without restarting the server. What that means for your code:

- **`Initialize` can run many times in one server run** (install, reload, replacing a plugin with a newer version), each time on
  a fresh instance in a fresh assembly load context. Keep state in your objects, not in `static` fields that must survive.
- **Stop everything you started.** On unload the server cancels the token given to your `IVariableProvider.RunAsync` and waits up
  to 5 seconds, then calls `Dispose` / `DisposeAsync` on your plugin instance and on every action, provider, settings page and
  icon pack you registered, if they implement `IDisposable` / `IAsyncDisposable`. Close sockets and stop timers and threads there.
  Whatever keeps running keeps your assembly in memory until the server restarts (a warning is logged; the plugin is
  deregistered either way).
- **Your variables are removed on unload** for you, and your status items and registrations are dropped.
- **Your assemblies are loaded from memory**, so your files are never locked and can be replaced while the plugin runs. The
  price: `Assembly.Location` is empty inside a plugin. Use `IPluginHost.DataDirectory` to find your own files.
- **Action types must be unique.** If one of yours is already registered (by the server or another plugin) the whole plugin fails
  to load as *Error* and nothing of it stays registered.
- Each plugin has its own assembly load context, so two plugins can use different versions of the same library. The one shared
  assembly is `MacroGrid.Plugin.Abstractions` (see project setup).

## 7. JavaScript plugins

A plugin with `"kind": "js"` is one script (`entry`, usually `index.js`) that runs in a sandbox inside the server. A complete
example is [HelloJs/](../HelloJs/).

### Permissions

Declare what the script needs in `plugin.json`. The user sees the list in the Plugins window and must approve it before the
script runs (status *Needs approval*). The approval is for that exact set: an update that asks for more waits for approval again.
Removing a plugin forgets its approval.

| Permission | Lets the script |
|---|---|
| `variables` | read any variable and publish its own |
| `actions` | register actions |
| `input` | press key combinations and type text on the PC |
| `http:<host>:<port>` | send HTTP requests to exactly that host and port (one entry per target, e.g. `http:localhost:4455`) |

Timers, settings pages, status items and logging need no permission. An unknown permission string makes the plugin an *Error*.
A call without its permission throws an ordinary JavaScript `Error` that the script can catch.

### The `host` object

Everything goes through the global, read-only `host`. There is no `require`, no `fetch`, no file access and no access to .NET.

```js
host.log(message)

host.variables.set(name, value)      // number, string, boolean or null
host.variables.get(name)
host.variables.remove(name)
host.variables.describe([{ name, description, example, category }])   // list them in the editor's variable picker

host.registerAction({ type, name, category, description, icon, fields, run(context, settings) {} })
// context: { deviceId, pageId, widgetId, value }; settings: the values of the action's fields
// fields: the same shape as the C# SettingField, e.g. { key, label, kind: 'Text' | 'Number' | 'Bool' | 'Select' | ..., default, min, max, options }

host.settings.page(fields)           // adds a settings page (stored in settings.json in the plugin folder)
host.settings.get()                  // the current values as an object
host.status(id, text, level)         // status bar item; level: 'Idle' | 'Ok' | 'Busy' | 'Warning' | 'Error'

host.input.hotkey('ctrl+shift+m')    // needs 'input'
host.input.type('hello')             // needs 'input'

host.http.get(url, { headers })          // needs http:<host>:<port>; returns { status, body } (body is text), synchronous
host.http.post(url, body, { headers })   // the body is sent as JSON

const id = host.every(ms, fn)        // repeat; the shortest interval is 100 ms
host.after(ms, fn)                   // once
host.cancel(id)
host.permissions                     // what was granted
```

Rules the server enforces:

- **Names are yours.** Variable names and action types must start with `<plugin id>.`, so a plugin can never overwrite
  `system.cpu` or another plugin's values.
- **Register at the top level.** Actions, the settings page and variable descriptions must be registered while the script first
  runs; registrations made later from a callback are ignored. Variables can be set at any time.
- **Time and memory are limited per call** (start-up, each action, each timer tick): 2 seconds, 32 MB, 2 million statements and
  a recursion depth of 100. A call that goes over fails with an error; the plugin keeps running. HTTP requests time out after
  5 seconds, responses are capped at 1 MB and redirects are not followed.
- **One thing at a time.** The script runs on its own thread, one call at a time, so a slow plugin never blocks the server or
  another plugin. `host.http` blocks the plugin's own other callbacks while it waits. At most 20 timers per plugin; ticks that
  pile up while the script is busy are dropped.
- **A plugin that fails 5 times in a row is switched off** (status *Error* with the last message). Reload starts it again.

There is no `async`/`await` host API yet and no way for a plugin to draw its own widget (a `plugin-html` widget is planned).

## 8. Limits of the current SDK

- The SDK is a NuGet package (`MacroGrid.Plugin.Abstractions`); the server's copy is the one used at runtime.
- A plugin cannot add a widget type.
- The server runs on Windows only, so plugins are Windows-only in practice.
