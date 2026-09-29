# What the SDK can do

This page is the map for plugin developers: what a plugin can and cannot do, which function to call for each thing, how versions work and
where to go next. Every row links to the page with the details.

## Choose a kind

Plugins by anyone other than the maintainer are [JavaScript plugins](/tutorials/js-hello-world): sandboxed, with the permissions you
declare and the user approves. C# plugins exist only as official, signed plugins; the server does not load any other.

See [Plugin basics](/basics/) for the folder layout, install steps and lifecycle.

## What a JavaScript plugin can add

| Capability | JavaScript |
|---|---|
| **Actions** a widget event can run (press, release, long press, double tap, toggle, value change) | `host.registerAction({...})` |
| **Live variables** that widgets show in text and use in conditions | `host.variables.set / get / remove` |
| **A variable catalog** (description, type, unit, allowed values for the picker) | `host.variables.describe([...])` |
| **A settings page** drawn by the editor from field declarations | `host.settings.page(fields)` |
| **A status bar item** | `host.status(id, text, level)` |
| **Press keys, type text** (only while handling a button press, with limits) | `host.input.hotkey / type` (permission `input`) |
| **HTTP requests** | `host.http.get / post / getAsync / postAsync` (permission `http:<host>:<port>`) |
| **Timers** | `host.every / after / cancel` |
| **Translations** | `locales/<language>.json` next to `plugin.json` |

Actions also receive the device, page and widget that triggered them.

Some things are available to the **official C# plugins only** because they need code running inside the server: icon packs, dynamic
dropdown options, buttons in a settings form, press and release pairing and protected secrets. A JavaScript plugin cannot use them.

## What a plugin cannot do

- Add a new widget type or draw its own widget. A plugin adds actions, variables, forms, status items and icons; the editor draws everything.
- Run on anything but Windows (the server is Windows-only).
- Use `require`, `fetch`, files, .NET or more than 20 timers. See [JavaScript host API](/reference/js-host-api).

## The functions you need, in the order you need them

1. **Describe the plugin:** [`plugin.json`](/reference/manifest) with `id`, `name`, `version`, `minMacroGrid`, `entry`, `kind`.
2. **Start:** the script runs top to bottom once per load. Register everything here.
3. **Do work:** an action's `run(context, settings)` runs when a widget event fires. `context` says which device, page and widget triggered it.
4. **Publish data:** `host.variables.set(name, value)`, at any time (for example from a timer). Names and action types must start with `<plugin id>.`.
5. **Let the user configure it:** field declarations for actions and settings pages ([Settings pages](/guides/settings-pages)).
6. **Stop cleanly:** nothing to do; timers are cancelled and variables removed when the plugin is unloaded ([lifecycle](/basics/#lifecycle)).

The full list is in the [JavaScript host API](/reference/js-host-api).

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

1. [Getting started](/getting-started/), then [Tutorial 1: JavaScript](/tutorials/js-hello-world).
2. [Tutorial 2: live data](/tutorials/live-data) for variables.
3. [Settings pages](/guides/settings-pages) for forms, dropdowns, lists, file pickers and secrets.
4. [Debugging and logs](/guides/debugging), then [Publishing your plugin](/guides/publishing).
5. For reading, the official plugins: [OBS](/guides/obs-plugin) (variables, dynamic options, connection state) and [PLC Icons](/guides/icon-packs).
