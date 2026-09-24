# Manifest (plugin.json)

Every plugin folder has a `plugin.json` at its root. This is the OBS plugin's:

<<< @/../OBS/plugin.json

## Fields

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | Unique, stable id. Used in the folder name, in action types and variable names, and for approvals. The server refuses a second plugin with the same id. |
| `name` | yes | Display name in the Plugins window. |
| `version` | yes | The plugin's own semantic version, independent of the server's. |
| `sdkVersion` | yes | The plugin SDK range the plugin was built against, a caret range such as `^0.3.0`. While the SDK is `0.x`, `^0.3.0` matches only `0.3.x`. If the server's SDK does not satisfy it the plugin is listed as *Incompatible* and not loaded. |
| `minServerVersion` | yes | The oldest server version the plugin needs. An older server lists the plugin as *Incompatible*. |
| `entry` | yes | C#: the entry DLL's file name. JavaScript: the script (usually `index.js`). |
| `kind` | yes | `"csharp"` or `"js"`. |
| `permissions` | no | JavaScript only: the permissions the script needs (see [Permissions](/reference/permissions)). Ignored for C# plugins. |

## JSON schema

The manifest maps to the `PluginManifest` record in the SDK (property names are camelCase in the file). A schema you can use in an
editor:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["id", "name", "version", "sdkVersion", "minServerVersion", "entry", "kind"],
  "properties": {
    "id": { "type": "string" },
    "name": { "type": "string" },
    "version": { "type": "string" },
    "sdkVersion": { "type": "string" },
    "minServerVersion": { "type": "string" },
    "entry": { "type": "string" },
    "kind": { "enum": ["csharp", "js"] },
    "permissions": { "type": ["array", "null"], "items": { "type": "string" } }
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
