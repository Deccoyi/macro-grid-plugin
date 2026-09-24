# Compatibility and versioning

Macro Grid has three independently versioned parts, and every plugin has its own version too.

| What | Where the version lives | Now |
|---|---|---|
| Server | `ClientHub.ServerVersion` in the server repository | `0.2.0` |
| Plugin SDK (`MacroGrid.Plugin.Abstractions`) | `PluginSdk.Version` | `0.3.1` |
| Each plugin | `version` in its own `plugin.json` | per plugin |

## What the server checks

Every plugin declares in `plugin.json` which SDK it was built against and which server it needs:

- **`sdkVersion`** is an npm-style caret range checked against `PluginSdk.Version`. While the SDK is `0.x`, `^0.3.0` matches
  `0.3.x` only; from `1.0.0` on, `^1.0.0` matches any `1.x.y`.
- **`minServerVersion`** is the oldest server that has what the plugin uses.

A plugin that satisfies neither is listed as **Incompatible** in the editor and is not loaded. Set both honestly: `sdkVersion`
to the SDK you built and tested against, `minServerVersion` to the oldest server that has what you use.

## What counts as a breaking change

- **SDK:** a public interface a plugin implements or receives (`IPlugin`, `IPluginHost`, `IActionHandler`, `IVariableProvider`,
  `IVariableStore`, `IDeviceController`, `ActionContext`, ...) changes incompatibly. Every plugin built for that SDK must be
  rebuilt. While the SDK is `0.x`, a minor bump may do this.
- **Plugin:** it changes its action types, settings or variable names in a way that makes a user's saved profile stop working.

## Versioning your plugin

- Use [semantic versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`) in `plugin.json`, independent of the server and of every
  other plugin. A server release never changes it.
- Bump the MAJOR version only for a breaking change: action types or settings that change meaning, so that a user's saved
  profile would silently stop working. Never rename an action `type` or a variable name casually.
- A plugin does not depend on another plugin's version. Plugins talk to each other only at run time, through variables.

## The SDK package

C# plugins compile against the NuGet package `MacroGrid.Plugin.Abstractions`. Use the version that matches `sdkVersion`, and
keep the SDK dll out of your output (`ExcludeAssets="runtime"`): the server and every plugin share the server's copy. See
[Tutorial 2](/tutorials/csharp-hello-world#step-1-create-the-project).
