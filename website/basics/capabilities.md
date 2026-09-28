# What the SDK can do

This page is the map for plugin developers: what a plugin can and cannot do, which function to call for each thing, how versions work and
where to go next. Every row links to the page with the details.

## Choose a kind

| You want to | Use |
|---|---|
| Poll a local HTTP API, publish a value, add a small action, no build step | a [JavaScript plugin](/tutorials/js-hello-world) (sandboxed, permissions you declare) |
| Keep a socket open, talk to a device, use any .NET library | a [C# plugin](/tutorials/csharp-hello-world) (full trust, needs a build) |

See [Plugin basics](/basics/) for the folder layout, install steps and lifecycle.

## What a plugin can add

| Capability | C# | JavaScript |
|---|---|---|
| **Actions** a widget event can run (press, release, long press, double tap, toggle, value change) | `host.RegisterAction(IActionHandler)` | `host.registerAction({...})` |
| **Live variables** that widgets show in text and use in conditions | `host.RegisterVariableProvider(IVariableProvider)` | `host.variables.set / get / remove` |
| **A variable catalog** (description, type, unit, allowed values for the picker) | `IVariableCatalogSource.Describe()` | `host.variables.describe([...])` |
| **A settings page** drawn by the editor from field declarations | `host.RegisterSettingsPage(IPluginSettingsPage)` | `host.settings.page(fields)` |
| **A status bar item** | `host.CreateStatusItem(id).Update(...)` | `host.status(id, text, level)` |
| **An icon pack** for the icon picker | `host.RegisterIconPack(IIconPackSource)` | not available |
| **Dynamic dropdown options** | `IOptionsSource` | not available |
| **Press and release pairing** ("play while held") | `IReleaseAwareAction` | not available |
| **Buttons in a settings form** (test connection, preview) | `ISettingsCommandHandler` | not available |
| **Protected secrets** (a password in your settings file) | `host.Secrets.Protect / Unprotect` | not available |
| **Press keys, type text** | not handed to plugins today | `host.input.hotkey / type` (permission `input`) |
| **HTTP requests** | your own `HttpClient` | `host.http.get / post` (permission `http:<host>:<port>`) |
| **Timers** | your own | `host.every / after / cancel` |
| **Translations** | `locales/<language>.json` next to `plugin.json` | the same |

Actions also receive an `IDeviceController`, so an action can switch page or profile on the phone that triggered it.

## What a plugin cannot do

- Add a new widget type or draw its own widget. A plugin adds actions, variables, forms, status items and icons; the editor draws everything.
- Run on anything but Windows (the server is Windows-only).
- (JavaScript) use `require`, `fetch`, files, .NET, `async`/`await` or more than 20 timers. See [JavaScript host API](/reference/js-host-api).

## The functions you need, in the order you need them

1. **Describe the plugin:** [`plugin.json`](/reference/manifest) with `id`, `name`, `version`, `minMacroGrid`, `entry`, `kind`.
2. **Start:** C# `IPlugin.Initialize(IPluginHost host)` is called once per load; a JavaScript script runs top to bottom once. Register everything here.
3. **Do work:** an action's `ExecuteAsync(context, settings, token)` (C#) or `run(context, settings)` (JS) runs when a widget event fires.
   `context` says which device, page and widget triggered it.
4. **Publish data:** `IVariableStore.Set(name, value)` from `IVariableProvider.RunAsync(store, token)`, which runs while the plugin is loaded and
   returns when the token is cancelled. Names and action types must start with `<plugin id>.`.
5. **Let the user configure it:** `SettingField` declarations for actions and settings pages ([Settings pages](/guides/settings-pages)).
6. **Stop cleanly:** cancel work on the token, implement `IDisposable` / `IAsyncDisposable` ([lifecycle](/basics/#lifecycle)).

The full signatures are in the [C# SDK reference](/reference/csharp-sdk) and the [JavaScript host API](/reference/js-host-api).

## How versioning works

There are three independent version numbers, all [semantic versions](https://semver.org/):

| What | Where | Who changes it |
|---|---|---|
| Macro Grid editor **and** the plugin SDK | one number, `<Version>` in the server repository (NuGet package `MacroGrid.Plugin.Abstractions`) | the Macro Grid release |
| Your plugin | `version` in your `plugin.json` | you |
| Minimum Macro Grid you need | `minMacroGrid` in your `plugin.json` | you |

- **Within one MAJOR the SDK only grows.** Members are added, never removed or changed, so a plugin built for `1.0.0` runs on every `1.x`.
  A new MAJOR may break plugins and every plugin must be rebuilt.
- **`minMacroGrid` is a minimum.** `"minMacroGrid": "1.2.0"` runs on 1.2.0 up to, not including, 2.0.0. Set it to the oldest version that has everything
  you use, so the plugin runs on as many editors as possible.
- **Bump your own MAJOR** only when a saved profile would silently stop working: a renamed action `type`, a variable name or a setting that changed meaning.
- An incompatible plugin is listed as *Incompatible* with the reason and is not loaded.

Details and the compatibility table: [Compatibility and versioning](/basics/compatibility).

## Where to go next

1. [Getting started](/getting-started/), then [Tutorial 1: JavaScript](/tutorials/js-hello-world) or [Tutorial 2: C#](/tutorials/csharp-hello-world).
2. [Tutorial 3: live data](/tutorials/live-data) for variables.
3. [Settings pages](/guides/settings-pages) for forms, dropdowns, lists, file pickers and secrets.
4. [Debugging and logs](/guides/debugging), then [Publishing your plugin](/guides/publishing).
5. Real examples: [OBS](/guides/obs-plugin) (C#, variables, dynamic options, connection state) and [PLC Icons](/guides/icon-packs).
