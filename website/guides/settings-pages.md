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

`SettingFieldKind` is `Text`, `Password`, `Number`, `Slider`, `Bool`, `Select`, `Segmented`, `File`, `List`, `Button` or `Notice`. `File` is a path box with
a Browse button, `List` is a set of repeated rows, `Button` runs a command in your plugin, and `Notice` is read-only warning text (`Button` and `Notice` are
never saved as values). Useful `SettingField` options:

| Option | Meaning |
|---|---|
| `Description`, `Placeholder`, `Default` | Help text, a hint and the initial value. |
| `Min`, `Max`, `Step` | Limits for `Number` and `Slider`. |
| `Options` | A fixed `SettingOption[]` for `Select` and `Segmented`. |
| `OptionsSource` | The id of a dynamic list served by `IOptionsSource`. |
| `DependsOn` | Keys whose current form values are passed to `GetOptionsAsync`; a change refetches the list. |
| `AllowVariables` | Shows the `{var}` insert button on a text field. You receive the raw template. |
| `VisibleWhen` | Only show the field when another field equals a value, for example `"mode=pause"`. Inside a `List` row it is checked against that row's own values. |
| `FileFilter` | Required for `File`: a WinForms file filter such as `"Audio files (*.wav;*.mp3)\|*.wav;*.mp3"`, passed to the native file picker as is. |
| `ItemFields` | Required for `List`: the fields of one row. The value is a JSON array of objects; keys outside the schema are kept across a save. |
| `Command` | Required for `Button`: the command id passed to `ISettingsCommandHandler.RunCommandAsync(command, values, token)` on the same class as your settings page. The returned text is shown as a short message. |

## Dynamic dropdowns

A page (or an action) that also implements `IOptionsSource` can serve options for a field whose `OptionsSource` names it:

```csharp
Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken);
```

Return an `OptionsResult` with `SettingOption(value, label)` items, or set `Error` to show a message and an empty list. The OBS
plugin serves its scene, audio input and scene item lists this way, from a cache of what OBS reported (see
[the OBS plugin](/guides/obs-plugin)).

## Secrets

`SettingFieldKind.Password` hides the input, but the value you save is your own business. In C#, run it through `host.Secrets.Protect(...)` before
writing it to your settings file and `Unprotect` when reading it back (it is protected for the current Windows user, so a copied data folder does not
carry a usable secret). The OBS plugin does this for its password. Without it the value is plain text in `settings.json`, so say so in your README.
