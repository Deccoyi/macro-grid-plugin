# Manifest (plugin.json)

Every plugin folder has a `plugin.json` at its root. This is the OBS plugin's:

<<< @/../OBS/plugin.json

## Fields

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | Unique, stable id. Used in the folder name, in action types and variable names, and for approvals. The server refuses a second plugin with the same id. |
| `name` | yes | Display name in the Plugins window. |
| `version` | yes | The plugin's own semantic version, independent of the server's. |
| `macroGrid` | yes | The oldest Macro Grid the plugin runs on, as `MAJOR.MINOR.PATCH` such as `1.3.0`. The plugin runs on every Macro Grid from that version up to, but not including, the next MAJOR. Macro Grid and the plugin SDK share one version, so use the SDK version you build against, or an older one if you use nothing newer. A server that does not fit lists the plugin as *Incompatible* and does not load it. |
| `sdkVersion` | no | Legacy, from before Macro Grid 1.0.0. Read only when `macroGrid` is missing: `^0.4.x` then counts as `macroGrid: 1.0.0`, older ranges are incompatible. Keep it next to `macroGrid` only if the plugin must still load on servers older than 1.0.0. |
| `minServerVersion` | no | Legacy, the same as `sdkVersion`. Macro Grid 1.0.0 and newer ignore it. |
| `entry` | yes | C#: the entry DLL's file name. JavaScript: the script (usually `index.js`). |
| `kind` | yes | `"csharp"` or `"js"`. |
| `defaultLanguage` | no | The language the plugin's own texts are written in, such as `"en"` (the default). Translations come from `locales/<language>.json` next to `plugin.json`; a missing language or text falls back to the text as written. |
| `permissions` | no | JavaScript only: the permissions the script needs (see [Permissions](/reference/permissions)). Ignored for C# plugins. |
| `description` | no | A one-line summary shown in Discover and the Store. Additive; older hosts ignore it. |
| `author` | no | The plugin's author, shown next to `description`. Additive. |
| `homepage` | no | A URL to the plugin's page or source, shown as a link. Additive. |

## JSON schema

The manifest maps to the `PluginManifest` record in the SDK (property names are camelCase in the file). A schema you can use in an
editor:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["id", "name", "version", "macroGrid", "entry", "kind"],
  "properties": {
    "id": { "type": "string" },
    "name": { "type": "string" },
    "version": { "type": "string" },
    "macroGrid": { "type": "string", "pattern": "^\\d+\\.\\d+\\.\\d+$" },
    "sdkVersion": { "type": "string" },
    "minServerVersion": { "type": "string" },
    "entry": { "type": "string" },
    "kind": { "enum": ["csharp", "js"] },
    "defaultLanguage": { "type": "string" },
    "permissions": { "type": ["array", "null"], "items": { "type": "string" } },
    "description": { "type": "string" },
    "author": { "type": "string" },
    "homepage": { "type": "string" }
  },
  "additionalProperties": true
}
```

The server does not read this schema; it is provided for convenience and is derived from the SDK's `PluginManifest`.

## Examples

A JavaScript plugin:

<<< @/../examples/hello-js/plugin.json

A C# plugin:

<<< @/../examples/hello-csharp/plugin.json

The SDK version is `PluginSdk.Version` in `MacroGrid.Plugin.Abstractions`. See [Compatibility](/basics/compatibility) for what counts
as a breaking change.
