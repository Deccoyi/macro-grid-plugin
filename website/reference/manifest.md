# Manifest (plugin.json)

Every plugin folder has a `plugin.json` at its root. This is the JavaScript example's:

<<< @/../examples/hello-js/plugin.json

## Fields

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | Unique, stable id. Used in the folder name, in action types and variable names, and for approvals. 1 to 64 characters: letters, digits, `.`, `-` and `_`, starting with a letter or digit and not ending with a dot (reserved Windows device names are refused). The server refuses a second plugin with the same id. |
| `name` | yes | Display name in the Plugins window. |
| `version` | yes | The plugin's own semantic version, independent of the server's. |
| `minMacroGrid` | yes | The oldest Macro Grid the plugin runs on, as `MAJOR.MINOR.PATCH` such as `1.3.0`. **It is a minimum, not an exact match:** the plugin runs on every Macro Grid from that version up to, but not including, the next MAJOR — an older server (say `1.2.1` written but the server is `1.1.1`) does *not* run it. Macro Grid and the plugin SDK share one version, so use the SDK version you build against, or an older one if you use nothing newer. A server that does not fit lists the plugin as *Incompatible* and does not load it. Was called `macroGrid` up to Macro Grid 1.2.x; that name still works, read only when `minMacroGrid` is absent, and stays readable for at least one MAJOR after the rename. |
| `macroGrid` | no | Legacy: the earlier name of `minMacroGrid`, same meaning and format. Read only when `minMacroGrid` is missing. A plugin that must still run on Macro Grid up to 1.2.x (which reads only this name) writes both, with the same value. |
| `sdkVersion` | no | Legacy, from before Macro Grid 1.0.0. Read only when neither `minMacroGrid` nor `macroGrid` is present: `^0.4.x` then counts as `minMacroGrid: 1.0.0`, older ranges are incompatible. Keep it next to `minMacroGrid` only if the plugin must still load on servers older than 1.0.0. |
| `minServerVersion` | no | Legacy, the same as `sdkVersion`. Macro Grid 1.0.0 and newer ignore it. |
| `entry` | yes | JavaScript: the script (usually `index.js`). For an official C# plugin: the entry DLL's file name. |
| `kind` | yes | `"js"` for every plugin except the official ones. `"csharp"` is used only by the official plugins, and the server loads such a plugin only when it carries the official signature (`signature.json` and `signature.sig`); a plugin that claims `"csharp"` without it is shown as *Not allowed*. |
| `defaultLanguage` | no | The language the plugin's own texts are written in, such as `"en"` (the default). Translations come from `locales/<language>.json` next to `plugin.json`; a missing language or text falls back to the text as written. |
| `permissions` | no | JavaScript only: the permissions the script needs (see [Permissions](/reference/permissions)). |
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
  "required": ["id", "name", "version", "minMacroGrid", "entry", "kind"],
  "properties": {
    "id": { "type": "string" },
    "name": { "type": "string" },
    "version": { "type": "string" },
    "minMacroGrid": { "type": "string", "pattern": "^\\d+\\.\\d+\\.\\d+$" },
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

## Example

A JavaScript plugin:

<<< @/../examples/hello-js/plugin.json

See [Compatibility](/basics/compatibility) for what counts as a breaking change.
