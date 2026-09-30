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

A plugin can draw its own widgets: see section 8.

## 8. Custom widgets

A plugin can add its own widgets to the Toolbox. The widget's code is one JavaScript file that runs on the phone (or in the browser deck, or in the
editor's preview) in a **worker with no network access** and draws to a **canvas**. It never touches the page around it, so a slow or broken widget cannot
freeze the deck. Data comes from the plugin on the PC, over a small `macroGrid` object. The working example is [HelloGauge/](../HelloGauge/). Needs Macro
Grid 1.4.0 or newer (`minMacroGrid` of the plugin).

### In `plugin.json`

```json
"widgets": [
  { "id": "gauge", "name": "Gauge", "description": "An animated gauge.", "category": "Gauges",
    "entry": "widgets/gauge.js", "icon": "widgets/gauge.svg", "assets": ["widgets/needle.png"], "size": { "w": 2, "h": 2 },
    "fps": 30, "interactive": true,
    "options": { "keepLoaded": { "default": false } },
    "settings": [ { "key": "source", "label": "Value", "kind": "Variable" }, { "key": "max", "label": "Maximum", "kind": "Number" } ] }
]
```

- `entry`: one script, at most 2 MB (1 MB for a JavaScript plugin, which is not verified). Bundle your libraries into it. `assets`: png, jpeg, webp images and woff2 fonts, at most 4 MB per plugin. At most 16 widgets per plugin.
- `fps`: the most frames per second the widget may draw, 1 to 60, 15 if left out (30 at most for a JavaScript plugin). Draw only when something changes; a widget that has nothing to animate should draw nothing. Measured on a recent phone, a widget that draws continuously at 15 fps costs about 3% of one processor core plus some graphics memory that grows while it runs; a widget that is idle costs close to nothing. Eight widgets that redraw all the time hold roughly twice the memory of eight that draw only on change, so animate only what needs it (the Hello Gauge example draws only while its needle moves).
- `icon`: an SVG file for the widget's Toolbox tile, for example `"widgets/gauge.svg"`. It is optional; without it the tile shows a puzzle piece. The editor uses only the **shape** of the image (its opacity) and paints it in the theme's color, so it looks like the built-in icons in dark and light themes. Recommended: a 24 x 24 `viewBox`, one solid color (any color; black is fine), strokes about 2 units wide with round ends, some space around the drawing, and at most 8 KB. Colors: the palette does not matter for the icon, because the editor repaints it in the current theme's text color (the same gray as the built-in icons, brighter on hover); only the shape counts. Do not rely on gradients or several colors: they are shown as a single color. For the widget's own drawing, `info.theme` (`dark` or `light`) tells you which colors to pick. An icon may not contain scripts, embedded pages or images (`<script>`, `<foreignObject>`, `<image>`, `<use>`, `<a>`), event handlers (`onload=...`), `href` or `src` attributes, or links; a widget whose icon breaks these rules is left out with a message in the Error List.
- `settings`: the same fields as everywhere, plus `Variable`: the person picks a variable in the editor and the widget may read that one. `Password` and `File` fields are not allowed (widget settings are saved in profiles, which people share).
- `options`: extra abilities you declare and the person approves when installing the plugin: `keepLoaded` (the widget stays loaded when its page is left), `storage` (a small store for the widget's own data, see below), `notifications`. A widget can use nothing it did not declare.
- `storage` is not a file system. With it, `await macroGrid.storage.get(key)` (gives `null` if there is nothing), `set(key, value)` and `remove(key)` keep JSON values on the device that shows the widget: keys are 1 to 64 characters, at most 64 keys and 256 KB in total per widget, and one call carries at most 16 KB. Nothing is sent to the PC or shared between devices. The person can clear it ("Clear widget data" in the editor's inspector). A value that is too big is refused with `quota_exceeded`.
- A widget that fails a check is left out with a message in the Error List; the plugin and its other widgets keep working.

### The `macroGrid` object (inside the widget)

The script is wrapped in an `async` function, so `await` and `return` work at the top. The only other API is `macroGrid`:

| Member | Meaning |
|---|---|
| `await macroGrid.ready` | `{ canvas, width, height, dpr, settings, bindings, mode, locale, theme }`. `canvas` is an `OffscreenCanvas`; get `"2d"` or `"webgl"` from it. `bindings` maps each `Variable` setting to the variable the person chose. `mode` is `"edit"` in the editor's preview. |
| `macroGrid.frame(cb)` | Runs `cb` on the next frame, at most at the widget's `fps`, never while paused. `requestAnimationFrame` and timers follow the same pause. |
| `macroGrid.onResize(cb)` | New size (`width`, `height`, `dpr`); set `canvas.width` and `canvas.height` yourself. |
| `macroGrid.subscribe(names)`, `macroGrid.onVariables(cb)` | Ask for variables (your plugin's own, and the bound ones; at most 32). `cb` gets all values seen so far. |
| `macroGrid.onEvent(name, cb)` | An event your plugin pushed with `host.widgets.post`. |
| `await macroGrid.request(data)` | Send a message to your plugin and wait for the answer (see below). At most 16 KB, 10 a second. |
| `await macroGrid.run(action, settings)` | Run one of your plugin's actions. A key press or typing by the action only works right after a real touch on this widget (one run per touch). |
| `macroGrid.onSettings(cb)` | The person changed the widget's settings. |
| `macroGrid.onPointer(cb)` | Only for `interactive` widgets: `{ phase: "down" \| "move" \| "up" \| "cancel", x, y }` in canvas pixels. |
| `macroGrid.onVisibility(cb)` | `"paused"` or `"resumed"`. |
| `await macroGrid.image(name)` | A package image as `ImageBitmap`; `await macroGrid.font(name, family)` adds a package font. |
| `macroGrid.error(message)` | Report a problem to the Error List (uncaught errors are reported for you). |

There is **no** `fetch`, `XMLHttpRequest`, `WebSocket`, `importScripts`, nested worker, browser storage or DOM, and `eval` does not work.
WebAssembly does. A plugin gets web data on the PC: your plugin script (`host.http`) fetches it and the widget asks for it with `macroGrid.request`.

### In the plugin script

```js
host.widgets.onMessage((message) => {          // message: { widget, deviceId, pageId, widgetId, settings, data }
  return { temperature: 21 };                  // a value or a promise; a thrown error becomes a failed request
});
host.widgets.post('gauge', 'weather', { t: 21 }, { retain: true });  // event to every placed 'gauge'; retain keeps the last one per name
```

`host.widgets.post(widget, name, data, { widgetId, deviceId, retain })` narrows the target; a retained event is given to a widget that comes on screen later. At most 20 events a second and 64 KB each.

### Limits and behavior worth knowing

- Only the widgets of the page that is shown run; a widget's worker is stopped when its page is left (unless it declared `keepLoaded`). At most 8 workers run at once on a device, and the frame rates of all running widgets are scaled down together when their sum passes 120.
- A widget that stops answering for 3 seconds, or uses more than half a core (a quarter for a JavaScript plugin) over 10 seconds, is stopped; it is started once more after 10 seconds and then waits for the person.
- A widget can run out of memory like any web code. The app survives and starts again, and tells the person which plugin's widgets were running and that they were switched off on that device.
- A JavaScript plugin's widgets show an "Unverified" mark and run with the lower limits above.
- Libraries that can draw to an `OffscreenCanvas` without a page around them (chart libraries, 2D engines) work when bundled into the one script; libraries that need a DOM or `eval` do not.

## 9. Limits of the current SDK

- The SDK is a NuGet package (`MacroGrid.Plugin.Abstractions`); the server's copy is the one used at runtime.
- A plugin adds widgets of its own (section 8), not new built-in widget types.
- The server runs on Windows only, so plugins are Windows-only in practice.
