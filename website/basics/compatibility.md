<script setup>
import { data as v } from '../.vitepress/versions.data'
</script>

# Compatibility and versioning

**Macro Grid (the server) and the plugin SDK share one version number.** The phone app and every plugin have their own.

| What | Where the version lives | Now |
|---|---|---|
| Macro Grid: server and plugin SDK (`MacroGrid.Plugin.Abstractions`) | `<Version>` in the server repository's `Directory.Build.props` | `{{ v.macroGrid ?? 'see the latest release' }}` |
| Each plugin | `version` in its own `plugin.json` | per plugin |
| Phone app | its own `package.json` | see its releases |

## What the server checks

Every plugin declares in `plugin.json` the oldest Macro Grid it runs on:

```json
{ "macroGrid": "1.3.0" }
```

The plugin runs on every Macro Grid from **1.3.0 up to, but not including, 2.0.0**. Always write three parts (`1.3.0`, not `1.3`).

| Plugin says | Macro Grid 1.2.4 | 1.3.0 | 1.9.9 | 2.0.0 |
|---|---|---|---|---|
| `1.0.0` | runs | runs | runs | rebuild |
| `1.3.0` | needs 1.3.0 | runs | runs | rebuild |

A plugin that does not fit is listed as **Incompatible** in the editor, with the reason ("Needs Macro Grid 1.3.0 or newer, this is 1.2.4",
or "must be rebuilt" for another MAJOR), and is not loaded. Discover and the Store only offer a version that fits.

Set `macroGrid` to the SDK version you build against, or older if you use nothing that came later: a lower value lets the plugin run on more
servers. For a C# plugin the build checks that it has the same MAJOR as the SDK package and is not newer than it.

### Older manifests

Before Macro Grid 1.0.0 a manifest had `sdkVersion` and `minServerVersion`. They are still read when `macroGrid` is missing: `sdkVersion` `^0.4.x`
counts as `macroGrid: 1.0.0` (nothing a 0.4 plugin uses changed), anything older must be rebuilt. If your plugin also has to load on servers older
than 1.0.0, keep the two old fields next to `macroGrid`; Macro Grid 1.0.0 and newer ignore them.

## What counts as a breaking change

Within one MAJOR the SDK only grows: members are added, never removed or changed, so a plugin built for 1.0.0 keeps running on 1.x.

- **Macro Grid / SDK (a new MAJOR):** a public interface a plugin implements or receives (`IPlugin`, `IPluginHost`, `IActionHandler`,
  `IVariableProvider`, `IVariableStore`, `IDeviceController`, `ActionContext`, ...) changes incompatibly. Every plugin must be rebuilt.
- **Plugin (its own MAJOR):** it changes its action types, settings or variable names in a way that makes a user's saved profile stop working.

## Versioning your plugin

- Use [semantic versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`) in `plugin.json`, independent of Macro Grid and of every
  other plugin. A Macro Grid release never changes it.
- Bump the MAJOR version only for a breaking change: action types or settings that change meaning, so that a user's saved
  profile would silently stop working. Never rename an action `type` or a variable name casually.
- A plugin does not depend on another plugin's version. Plugins talk to each other only at run time, through variables.

## The SDK package

C# plugins compile against the NuGet package `MacroGrid.Plugin.Abstractions`; its version is the Macro Grid version it belongs to. Use the
version you want to build against, and keep the SDK dll out of your output (`ExcludeAssets="runtime"`): the server and every plugin share
the server's copy. See [Tutorial 2](/tutorials/csharp-hello-world#step-1-create-the-project).
