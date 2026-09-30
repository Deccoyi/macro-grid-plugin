# Plugin basics

A plugin adds actions (things a widget can do), variables (live values a widget can show) and small extras (a settings page,
status bar items, icon packs) to the Macro Grid server.

## Kinds

| | JavaScript plugin | C# plugin (official plugins only) |
|---|---|---|
| `kind` in `plugin.json` | `"js"` | `"csharp"` |
| Who can ship it | Anyone | Only the official plugins in the [macro-grid-plugin](https://github.com/Deccoyi/macro-grid-plugin) repository |
| Trust | Sandboxed. No .NET access, only a small `host` object, and only the permissions the user approved. | Full trust, inside the server process. It loads only with a valid official signature, checked every time. |
| Good for | Small scripts (poll a local HTTP API, publish a variable, add an action) | Real integrations (a websocket client, a device driver) |
| Needs a build | No (one script) | Yes, and the maintainer signs it |

::: warning Third-party plugins are JavaScript
The server refuses to install or load a C# plugin that is not officially signed, whatever the source (Discover, an added source, a
pasted link or a folder). A plugin folder is one kind or the other, never both.
:::

The working examples are [HelloJs](/tutorials/js-hello-world) (a small JavaScript plugin) and, for reading, the official [OBS](/guides/obs-plugin)
plugin (a C# integration).

## Folder layout

The server looks for plugins in `%AppData%\MacroGrid\plugins\<folder>\`. A folder is a plugin if it contains a `plugin.json`.
The folder name does not matter; the identity is the `id` in the manifest.

A JavaScript plugin:

```
hello-js/
├── plugin.json
└── index.js
```

In the source repository, each plugin also keeps a README, a `LICENSE` and two changelogs next to `plugin.json`; see
[Repository rules](/guides/repo-rules).

## Install a plugin

1. In the editor open **Plugins, Manage Plugins...**
2. Choose **Install from Folder...** and pick a folder that contains `plugin.json`.
3. The folder is copied to `plugins\<id>\` and loaded immediately, with no restart.

Installing a folder whose `id` is already installed replaces that plugin. Files the plugin wrote into its own folder, such as
`settings.json`, are kept. The same window can reload and remove a plugin.

## Status of a plugin

The Plugins window lists every folder that has a `plugin.json`:

- **Loaded**: running, its actions and variables are available.
- **Incompatible**: `minMacroGrid` asks for a newer Macro Grid than this one, or for another MAJOR (the message says which).
- **Needs approval**: a JavaScript plugin whose declared permissions the user has not approved yet. It does not run until they do.
- **Not allowed**: a C# plugin that is not officially signed, or whose files no longer match its signature. It never runs; the message says why and you can only remove it.
- **Error**: `plugin.json` could not be parsed, the entry file is missing, the id is already used by another installed plugin, an
  action type is already registered, a permission is unknown, `Initialize` (or the script's first run) failed, or a JavaScript
  plugin was switched off after failing repeatedly. The message is shown under the name. **Reload** tries again.

## Lifecycle

Plugins are loaded when the server starts and can be installed, reloaded and removed at any time, without restarting the
server. For a JavaScript plugin that means:

- **The script runs once per load** (start, install, reload, replacing a plugin with a newer version). Register actions, the settings
  page and variable descriptions at the top level of the script.
- **Timers and requests stop on unload.** Whatever the script started with `host.every` or `host.after` is cancelled when the plugin is
  unloaded, and its variables and status items are removed for you.
- **Action types must be unique.** If one of yours is already registered (by the server or another plugin) the whole plugin
  fails to load as *Error* and nothing of it stays registered.
- **Files.** A JavaScript plugin has no file access of its own; the values of its settings page are stored for it in `settings.json` in its folder (at most 64 KB).

## Actions, widgets and variables in one picture

A widget's events (press, release, long press, double tap, toggle on/off, value change) each run a list of actions in order.
An action's settings are the values of its form. Variables flow the other way: providers publish values, and widgets show them
in text or use them in conditional rules (color, text, icon, animation).

## Limits of the current SDK

- A plugin's script cannot draw. A plugin can ship its own widgets (separate scripts that run in a sandboxed worker in the app, with no network access), but it cannot add a widget type of its own.
- The server runs on Windows only, so plugins are Windows-only in practice.
