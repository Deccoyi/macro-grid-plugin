# Writing a plugin

A plugin adds actions (things a widget can do), variables (live values a widget can show) and small extras (a settings
page, status bar items) to the Macro Grid server. **Plugins by other authors are JavaScript plugins**: they run in a sandbox and
get only the permissions the user approved. C# plugins are used only for the official plugins in this repository, which are built and
signed by the maintainer; a C# plugin that is not officially signed is not loaded (see [official-csharp-plugins.md](official-csharp-plugins.md)).

| | JavaScript plugin |
|---|---|
| `kind` in `plugin.json` | `"js"` |
| Trust | Sandboxed. No .NET access, only a small `host` object, and only the permissions the user approved. |
| Good for | Small scripts (poll a local HTTP API, publish a variable, add an action) |
| Needs a build | No (one script) |

The working example in this repository is [HelloJs/](../HelloJs/). Read [CONTRIBUTING.md](../CONTRIBUTING.md) for the
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
  "name": "WebSocketBridge For OBS",
  "version": "0.3.0",
  "minMacroGrid": "1.0.0",
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
| `minMacroGrid` | yes | The oldest Macro Grid the plugin runs on, as `MAJOR.MINOR.PATCH` such as `1.3.0`. **It is a minimum, not an exact match:** the plugin runs on every Macro Grid from that version up to, but not including, the next MAJOR — an older server (say `1.2.1` written but the server is `1.1.1`) does *not* run it. Macro Grid and the plugin SDK share one version, so use the SDK version you build against, or an older one if you use nothing newer. A server that does not fit lists the plugin as *Incompatible* and does not load it. Was called `macroGrid` up to Macro Grid 1.2.x; that name still works, read only when `minMacroGrid` is absent, for at least one MAJOR after the rename. |
| `macroGrid` | no | Legacy: the earlier name of `minMacroGrid`, same meaning and format. Read only when `minMacroGrid` is missing. A plugin that must still run on Macro Grid up to 1.2.x (which reads only this name) writes both, with the same value. |
| `sdkVersion` | no | Legacy, from before Macro Grid 1.0.0. Read only when neither `minMacroGrid` nor `macroGrid` is present: `^0.4.x` then counts as `minMacroGrid: 1.0.0`, older ranges are incompatible. Keep it next to `minMacroGrid` only if the plugin must still load on servers older than 1.0.0. |
| `minServerVersion` | no | Legacy, the same as `sdkVersion`. Macro Grid 1.0.0 and newer ignore it. |
| `entry` | yes | C#: the entry DLL's file name. JavaScript: the script (usually `index.js`). |
| `kind` | yes | `"csharp"` or `"js"`. |
| `defaultLanguage` | no | The language the plugin's own texts are written in, such as `"en"` (the default). See [Languages](#languages-defaultlanguage-and-locales). |
| `permissions` | no | JavaScript only: the permissions the script needs (see [JavaScript plugins](#7-javascript-plugins)). |
| `description` | no | A one-line summary shown in Discover and the Store. Additive; older hosts ignore it. |
| `author` | no | The plugin's author, shown next to `description`. Additive. |
| `homepage` | no | A URL to the plugin's page or source, shown as a link. Additive. |

Macro Grid and the plugin SDK share one version (`PluginSdk.Version` in `MacroGrid.Plugin.Abstractions`). See the server repository's
[versioning guide](https://github.com/Deccoyi/macro-grid/blob/main/docs/guides/versioning.md) for what changes the MAJOR, MINOR and PATCH number.

## 3. Status of a plugin in the editor


The Plugins window lists every folder that has a `plugin.json`:

- **Loaded**: running, its actions and variables are available.
- **Incompatible**: `minMacroGrid` asks for a newer Macro Grid than this one, or for another MAJOR (the message says which).
- **Needs approval**: a JavaScript plugin whose declared permissions the user has not approved yet. It does not run until they do.
- **Error**: `plugin.json` could not be parsed, the entry file is missing, the id is already used by another installed plugin, an action type is already registered, a permission is unknown, `Initialize` (or the script's first run) failed, or a JavaScript plugin was switched off after failing repeatedly. The message is shown under the name. **Reload** tries again.

## 4. Writing a C# plugin

Official C# plugins only: see [official-csharp-plugins.md](official-csharp-plugins.md).

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
