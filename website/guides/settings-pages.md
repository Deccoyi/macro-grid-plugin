# Settings pages

A plugin can add a settings form to the Plugins window. The editor draws the form from a list of field declarations; the plugin
never draws UI itself. The same fields (plain objects) also describe the form of an
[action](/tutorials/js-hello-world).

A plugin with a settings page gets a **gear button** in the Plugins window, and its status bar item opens the page.

## Declaring a page

`host.settings.page(fields)` adds a page. The values are stored in `settings.json` in the plugin folder, at most 64 KB (a larger save is refused with a message), and
read with `host.settings.get()`. Register the page at the top level of the script.

<<< @/../examples/hello-js/index.js#settings

## Field kinds and options

`kind` is `Text`, `Password`, `Number`, `Slider`, `Bool`, `Select`, `Segmented`, `File`, `List`, `Button` or `Notice`. `File` is a path box with
a Browse button, `List` is a set of repeated rows, `Button` runs a command in the plugin (official C# plugins only), and `Notice` is read-only warning text (`Button` and `Notice` are
never saved as values). Useful field options:

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

## Only in official C# plugins

Some things need code inside the server and are therefore not available to JavaScript plugins: a `Button` field that runs a command in the plugin,
dropdowns filled from a live source (`OptionsSource`), and protected secrets (a `Password` field is hidden while typing, but a JavaScript plugin
stores its value as plain text in `settings.json`, so say so in your README).
