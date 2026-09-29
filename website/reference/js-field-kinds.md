# Field kinds

A **field** describes one input of a form. The editor draws the form; your plugin never draws UI. The same field objects are used in
two places:

- `host.settings.page(fields)`: the plugin's settings window (the gear button in the Plugins window).
- `fields` of `host.registerAction({...})`: the form the user fills in when binding the action to a widget event.

```js
{ key: 'volume', label: 'Volume', kind: 'Slider', min: 0, max: 100, step: 5, default: 50 }
```

`key` is the name under which the value arrives (`settings.volume` in an action, `host.settings.get().volume` in a settings page).
`label` is the text next to the input. `kind` is one of the names below.

## The kinds

| Kind | The user sees | Value in your script | Uses these options |
|---|---|---|---|
| `Text` | a single-line text box | string | `placeholder`, `default`, `allowVariables` |
| `Password` | a hidden text box | string (stored as plain text in JavaScript plugins) | `placeholder` |
| `Number` | a number box | number | `min`, `max`, `step`, `default` |
| `Slider` | a slider with the value shown | number | `min`, `max`, `step`, `default` |
| `Bool` | a checkbox or switch | `true` / `false` | `default` |
| `Select` | a dropdown | the chosen option's `value` (string) | `options`, `default` |
| `Segmented` | a row of buttons, one active | the chosen option's `value` (string) | `options`, `default` |
| `File` | a path box with a **Browse** button (native file picker) | the path (string) | `fileFilter` |
| `List` | repeated rows, the user adds and removes them | an array of objects | `itemFields` |
| `Notice` | read-only warning text | none, never saved | `label`, `description` |
| `Button` | a button | none | not usable from JavaScript (see below) |

## Options

| Option | Applies to | Meaning |
|---|---|---|
| `description` | all | Help text under the field. |
| `placeholder` | text kinds | A hint shown while the box is empty. |
| `default` | value kinds | The initial value. Also what `settings` and `host.settings.get()` hold until the user changes it. |
| `min`, `max`, `step` | `Number`, `Slider` | Limits and increment. |
| `options` | `Select`, `Segmented` | A fixed list of `{ value, label }`. Optional `group` indents the row as *Group › Item*, optional `icon` shows an icon. |
| `allowVariables` | `Text` | Shows the `{var}` insert button. You receive the raw text with the `{...}` still in it. Read the value yourself with `host.variables.get`. |
| `visibleWhen` | all | Show the field only when another field has a value, written `"key=value"`, for example `"mode=pause"`. Inside a `List` row it is checked against that row's own values. |
| `fileFilter` | `File` | Required. A Windows file filter such as `"Audio files (*.wav;*.mp3)\|*.wav;*.mp3"`. |
| `itemFields` | `List` | Required. The fields of one row, using any kind above. Row keys you did not declare are kept across a save. |
| `dependsOn`, `optionsSource`, `command` | dynamic dropdowns and buttons | These need a C# handler and do nothing in a JavaScript plugin. |

## Examples

**A settings page with several kinds**

```js
host.settings.page([
  { kind: 'Notice', key: 'n', label: 'Talks to the hub on your network.' },
  { key: 'host', label: 'Hub address', kind: 'Text', placeholder: '192.168.1.20' },
  { key: 'mode', label: 'Mode', kind: 'Segmented', default: 'auto',
    options: [{ value: 'auto', label: 'Auto' }, { value: 'manual', label: 'Manual' }] },
  { key: 'level', label: 'Level', kind: 'Slider', min: 0, max: 100, step: 5, default: 50,
    visibleWhen: 'mode=manual' },
  { key: 'token', label: 'Token', kind: 'Password' },
  { key: 'log', label: 'Write to file', kind: 'File', fileFilter: 'Text files (*.txt)|*.txt' },
])
```

**A list**

```js
host.settings.page([
  { key: 'lights', label: 'Lights', kind: 'List', itemFields: [
    { key: 'name', label: 'Name', kind: 'Text' },
    { key: 'port', label: 'Port', kind: 'Number', min: 1, max: 65535, default: 8080 },
  ] },
])
const lights = host.settings.get().lights   // [{ name: 'Desk', port: 8080 }, ...]
```

**An action form that can use variables**

```js
host.registerAction({
  type: 'myplugin.say', name: 'Say', run(context, settings) { host.log(settings.text) },
  fields: [{ key: 'text', label: 'Text', kind: 'Text', allowVariables: true }],
})
```

## Where the values live

- **Settings page:** stored in `settings.json` in the plugin folder. Only declared keys are kept. `host.settings.get()` merges
  saved values over the defaults. Users can also open the file, so validate what you read.
- **Action form:** stored inside the profile, next to the button that uses the action, and passed to `run` as `settings`.

Form labels can be translated through `locales/<language>.json`, see
[JavaScript host API](/reference/js-host-api#translations). The general behavior of settings pages is in
[Settings pages](/guides/settings-pages).
