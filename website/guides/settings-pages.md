# Settings pages

A plugin can add a settings form to the Plugins window. The editor draws the form from a list of field declarations; the plugin
never draws UI itself. The same fields (`SettingField` in C#, plain objects in JavaScript) also describe the form of an
[action](/tutorials/csharp-hello-world#step-3-the-action-and-its-form).

A plugin with a settings page gets a **gear button** in the Plugins window, and its status bar item opens the page.

## JavaScript

`host.settings.page(fields)` adds a page. The values are stored in `settings.json` in the plugin folder and read with
`host.settings.get()`. Register the page at the top level of the script.

<<< @/../examples/hello-js/index.js#settings

## C#

Implement `IPluginSettingsPage` (`Fields`, `Load()`, `Save(values)`) and register it with `host.RegisterSettingsPage`. `Load` returns
the current values as a `JsonObject`, `Save` receives the values the user entered. They are your bridge to disk, usually a
`settings.json` in `host.DataDirectory`.

<<< @/../examples/hello-csharp/src/HelloSettingsPage.cs#settings{cs}

## Field kinds and options

`SettingFieldKind` is `Text`, `Password`, `Number`, `Slider`, `Bool`, `Select` or `Segmented`. Useful `SettingField` options:

| Option | Meaning |
|---|---|
| `Description`, `Placeholder`, `Default` | Help text, a hint and the initial value. |
| `Min`, `Max`, `Step` | Limits for `Number` and `Slider`. |
| `Options` | A fixed `SettingOption[]` for `Select` and `Segmented`. |
| `OptionsSource` | The id of a dynamic list served by `IOptionsSource`. |
| `DependsOn` | Keys whose current form values are passed to `GetOptionsAsync`; a change refetches the list. |
| `AllowVariables` | Shows the `{var}` insert button on a text field. You receive the raw template. |
| `VisibleWhen` | Only show the field when another field equals a value, for example `"mode=pause"`. |

## Dynamic dropdowns

A page (or an action) that also implements `IOptionsSource` can serve options for a field whose `OptionsSource` names it:

```csharp
Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken);
```

Return an `OptionsResult` with `SettingOption(value, label)` items, or set `Error` to show a message and an empty list. The OBS
plugin serves its scene, audio input and scene item lists this way, from a cache of what OBS reported (see
[the OBS plugin](/guides/obs-plugin)).

## Secrets

`SettingFieldKind.Password` hides the input, but the value you save is your own business: the OBS plugin stores its password in plain
text in its `settings.json`. Say so in your README.
