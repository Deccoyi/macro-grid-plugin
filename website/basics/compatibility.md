<script setup>
import { data as v } from '../.vitepress/versions.data'
</script>

# Compatibility and versioning

**The Macro Grid editor and the plugin SDK share one version number** (the editor includes the server that runs on your PC). The phone app and every plugin have their own.

| What | Where the version lives | Now |
|---|---|---|
| Macro Grid editor and plugin SDK (`MacroGrid.Plugin.Abstractions`) | `<Version>` in the server repository's `Directory.Build.props` | `{{ v.macroGrid ?? 'see the latest release' }}` |
| Each plugin | `version` in its own `plugin.json` | per plugin |
| Phone app | its own `package.json` | see its releases |

## What the editor checks

Every plugin declares in `plugin.json` the oldest Macro Grid editor version it runs on:

```json
{ "minMacroGrid": "1.3.0" }
```

The plugin runs on every Macro Grid editor from **1.3.0 up to, but not including, 2.0.0**. Always write three parts (`1.3.0`, not `1.3`).

**`minMacroGrid` is a minimum, not an exact match.** A plugin declaring `"minMacroGrid": "1.2.1"` does **not** run on Macro Grid editor `1.1.1` (older
than what it asks for) — only on `1.2.1` and every later version of the same MAJOR.

`minMacroGrid` was called `macroGrid` up to Macro Grid editor 1.2.x. That name still works — read only when `minMacroGrid` is absent — and
stays readable for at least one MAJOR after the rename. A plugin that must still run on 1.2.x, which reads only the old name, writes both
fields with the same value.

| Plugin says | Editor 1.2.4 | 1.3.0 | 1.9.9 | 2.0.0 |
|---|---|---|---|---|
| `1.0.0` | runs | runs | runs | rebuild |
| `1.3.0` | needs 1.3.0 | runs | runs | rebuild |

A plugin that does not fit is listed as **Incompatible** in the editor, with the reason ("Needs Macro Grid editor 1.3.0 or newer, this is 1.2.4",
or "must be rebuilt" for another MAJOR), and is not loaded. Discover and the Store only offer a version that fits.

Set `minMacroGrid` to the SDK version you build against, or older if you use nothing that came later: a lower value lets the plugin run on more
editor versions.

### Older manifests

Before Macro Grid 1.0.0 a manifest had `sdkVersion` and `minServerVersion`. They are still read when neither `minMacroGrid` nor `macroGrid` is
present: `sdkVersion` `^0.4.x` counts as `minMacroGrid: 1.0.0` (nothing a 0.4 plugin uses changed), anything older must be rebuilt. If your
plugin also has to load on servers older than 1.0.0, keep the two old fields next to `minMacroGrid`; Macro Grid 1.0.0 and newer ignore them.

## What counts as a breaking change

Within one MAJOR the SDK only grows: members are added, never removed or changed, so a plugin built for 1.0.0 keeps running on 1.x.

- **Macro Grid / SDK (a new MAJOR):** something a plugin uses (the `host` object of JavaScript plugins, the manifest, the SDK interfaces the
  official C# plugins implement) changes incompatibly. Plugins must be changed, and the official C# ones rebuilt.
- **Plugin (its own MAJOR):** it changes its action types, settings or variable names in a way that makes a user's saved profile stop working.

## Versioning your plugin

- Use [semantic versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`) in `plugin.json`, independent of Macro Grid and of every
  other plugin. A Macro Grid release never changes it.
- Bump the MAJOR version only for a breaking change: action types or settings that change meaning, so that a user's saved
  profile would silently stop working. Never rename an action `type` or a variable name casually.
- A plugin does not depend on another plugin's version. Plugins talk to each other only at run time, through variables.

## The SDK

The official C# plugins compile against the plugin SDK, whose version is the Macro Grid version it belongs to. A JavaScript plugin needs no SDK
and no build: it only declares `minMacroGrid` in its manifest.
