# Plugin basics

A plugin adds actions (things a widget can do), variables (live values a widget can show) and small extras (a settings page,
status bar items, icon packs) to the Macro Grid server.

## Kinds

| | C# plugin | JavaScript plugin |
|---|---|---|
| `kind` in `plugin.json` | `"csharp"` | `"js"` |
| Trust | Full trust. It runs inside the server process with full .NET access, isolated only so that it cannot break other plugins. | Sandboxed. No .NET access, only a small `host` object, and only the permissions the user approved. |
| Good for | Real integrations (a websocket client, a device driver) | Small scripts (poll a local HTTP API, publish a variable, add an action) |
| Needs a build | Yes (a DLL) | No (one script) |

::: danger Only install C# plugins you trust
They can do anything the server can do. A plugin folder is one kind or the other, never both.
:::

The working examples are [OBS](/guides/obs-plugin) (a full C# integration), [PLC Icons](/guides/icon-packs) (a C# icon pack)
and [HelloJs](/tutorials/js-hello-world) (a small JavaScript plugin).

## Folder layout

The server looks for plugins in `%AppData%\MacroGrid\plugins\<folder>\`. A folder is a plugin if it contains a `plugin.json`.
The folder name does not matter; the identity is the `id` in the manifest.

A JavaScript plugin:

```
hello-js/
├── plugin.json
└── index.js
```

A C# plugin, as it is installed (the build output folder; the build copies `plugin.json` next to the DLL):

```
HelloCSharp/
├── plugin.json
└── HelloCSharp.dll
```

In the source repository, each plugin also keeps a README, a `LICENSE` and two changelogs next to `plugin.json`; see
[Repository rules](/guides/repo-rules).

## Install a plugin

1. In the editor open **Plugins, Manage Plugins...**
2. Choose **Install from Folder...** and pick a folder that contains `plugin.json` (for a C# plugin, the build output folder such
   as `src\bin\Debug\net10.0\`).
3. The folder is copied to `plugins\<id>\` and loaded immediately, with no restart.

Installing a folder whose `id` is already installed replaces that plugin. Files the plugin wrote into its own folder, such as
`settings.json`, are kept. The same window can reload and remove a plugin.

## Status of a plugin

The Plugins window lists every folder that has a `plugin.json`:

- **Loaded**: running, its actions and variables are available.
- **Incompatible**: `macroGrid` asks for a newer Macro Grid than this one, or for another MAJOR (the message says which).
- **Needs approval**: a JavaScript plugin whose declared permissions the user has not approved yet. It does not run until they do.
- **Error**: `plugin.json` could not be parsed, the entry file is missing, the id is already used by another installed plugin, an
  action type is already registered, a permission is unknown, `Initialize` (or the script's first run) failed, or a JavaScript
  plugin was switched off after failing repeatedly. The message is shown under the name. **Reload** tries again.

## Lifecycle

Plugins are loaded when the server starts and can be installed, reloaded and removed at any time, without restarting the
server. What that means for your code:

- **`Initialize` can run many times in one server run** (install, reload, replacing a plugin with a newer version), each time
  on a fresh instance in a fresh assembly load context. Keep state in your objects, not in `static` fields that must survive.
- **Stop everything you started.** On unload the server cancels the token given to your `IVariableProvider.RunAsync` and waits
  up to 5 seconds, then calls `Dispose` / `DisposeAsync` on your plugin instance and on every action, provider, settings page
  and icon pack you registered, if they implement `IDisposable` / `IAsyncDisposable`. Close sockets and stop timers and threads
  there. Whatever keeps running keeps your assembly in memory until the server restarts (a warning is logged; the plugin is
  deregistered either way).
- **Your variables are removed on unload** for you, and your status items and registrations are dropped.
- **Your assemblies are loaded from memory**, so your files are never locked and can be replaced while the plugin runs. The
  price: `Assembly.Location` is empty inside a plugin. Use `IPluginHost.DataDirectory` to find your own files.
- **Action types must be unique.** If one of yours is already registered (by the server or another plugin) the whole plugin
  fails to load as *Error* and nothing of it stays registered.
- Each plugin has its own assembly load context, so two plugins can use different versions of the same library. The one shared
  assembly is `MacroGrid.Plugin.Abstractions`.

## Actions, widgets and variables in one picture

A widget's events (press, release, long press, double tap, toggle on/off, value change) each run a list of actions in order.
An action's settings are the values of its form. Variables flow the other way: providers publish values, and widgets show them
in text or use them in conditional rules (color, text, icon, animation).

## Limits of the current SDK

- A plugin cannot add a widget type or draw its own widget (a `plugin-html` widget is planned).
- The server runs on Windows only, so plugins are Windows-only in practice.
- There is no `async`/`await` host API for JavaScript plugins yet.
