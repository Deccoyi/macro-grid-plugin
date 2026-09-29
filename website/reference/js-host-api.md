# JavaScript host API

A plugin with `"kind": "js"` is one script (`entry`, usually `index.js`) that runs in a sandbox inside the server. Everything goes
through the global, frozen `host` object. There is no `require`, no `import`, no `fetch`, no `setTimeout`, no file access and no
access to .NET. The script runs in strict mode with modern JavaScript, including `Promise` and `async` functions.

This page lists every function, every option and every limit. For the permission side see [Permissions](/reference/permissions); for
ideas built from these pieces see [What you can build](/guides/js-recipes).

## At a glance

| Area | Functions | Permission |
|---|---|---|
| [Logging](#host-log) | `host.log` | none |
| [Variables](#host-variables) | `host.variables.set / get / remove / describe` | `variables` |
| [Actions](#host-registeraction) | `host.registerAction` | `actions` |
| [Settings page](#host-settings) | `host.settings.page / get` | none |
| [Status bar](#host-status) | `host.status` | none |
| [Keyboard](#host-input) | `host.input.hotkey / type` | `input`, and only during a button press |
| [HTTP](#host-http) | `host.http.get / post / getAsync / postAsync` | `http:<host>:<port>` |
| [Timers](#host-every-host-after-host-cancel) | `host.every / after / cancel` | none |
| [Granted permissions](#host-permissions) | `host.permissions` | none |
| Translations | `locales/<language>.json` next to `plugin.json` | none |

Two rules apply everywhere:

- **Names are yours.** Variable names and action types must start with `<plugin id>.`.
- **Register at the top level.** Actions, the settings page and variable descriptions are collected while the script first runs.
  Registrations made later, from a timer or an action, are ignored. Variables can be set at any time.

## host.log

```js
host.log('connected to ' + url)
```

Writes a line to the server log with the plugin id in front (see [Debugging](/guides/debugging)). The argument is turned into a
string and cut at 500 characters.

## host.variables

Needs `variables`. A variable is a live value that widgets show as `{name}` and that dynamic rules and conditions can use.

```js
host.variables.set('myplugin.temp', 21.5)   // number, string, boolean or null
host.variables.get('system.cpu')            // any variable, yours or not
host.variables.remove('myplugin.temp')
host.variables.describe([
  { name: 'myplugin.temp', description: 'Room temperature', example: '21.5', category: 'Home',
    type: 'number', unit: '°C' },
  { name: 'myplugin.mode', description: 'Current mode', example: 'eco', category: 'Home',
    type: 'text', values: ['eco', 'comfort', 'away'] },
])
```

| Call | What it does |
|---|---|
| `set(name, value)` | Publishes or updates one of your variables. `undefined` becomes `null`; any other type (an object, an array) is turned into a string. |
| `get(name)` | Reads the current value of any variable, including `system.*` and other plugins'. A variable that does not exist gives `null`. |
| `remove(name)` | Removes one of your variables. |
| `describe(list)` | Lists variables in the editor's variable picker so people do not have to guess names. Call it once, at the top level. |

**Names** must start with `<plugin id>.`, be at most 120 characters and use only letters, digits, `.`, `_` and `-`.

**`describe` entries:**

| Field | Meaning |
|---|---|
| `name` | The variable name, with the plugin id prefix. |
| `description` | One line shown in the picker. |
| `example` | An example value, shown next to it. |
| `category` | The group it appears under in the picker. Invent your own category name freely. |
| `type` | `'text'` (default), `'number'`, `'boolean'`, `'duration'` or `'dateTime'`. The editor uses it to show the value and to offer the right input in a condition. |
| `unit` | For a number: the unit shown next to the value input, for example `'%'`, `'GB'`, `'kbps'`. |
| `values` | For fixed-choice text: the allowed values, offered as a list in conditions. |

**Value types.** Numbers, strings and booleans are stored as they are. In widget text a number takes a format (`{myplugin.temp|0.0}`),
a boolean prints as *Açık/Kapalı* unless the widget gives its own words (`{myplugin.on|ON/OFF}`), see
[Variables and text](https://deccoyi.github.io/macro-grid/guide/variables). Descriptions, categories and units go through the
plugin's translation file.

Built-in variables you can read include `system.time`, `system.cpu`, `system.ram`, `system.ram.used`, `system.ram.total`,
`system.uptime`, `system.audio.master` and `system.audio.muted`, plus whatever other plugins publish (for example `obs.*`).

## host.registerAction

Needs `actions`.

```js
host.registerAction({
  type: 'myplugin.setLight',      // required, must start with the plugin id
  name: 'Set light',              // picker name (default: the type)
  category: 'Home',               // picker group (default: 'Plugins')
  description: 'Turns a light on or off',
  icon: 'lightbulb',              // optional icon name, shown in the picker
  fields: [ /* the action's own form, see Field kinds */ ],
  run(context, settings) { /* ... */ },   // required
})
```

| Property | Meaning |
|---|---|
| `type` | Unique id, saved in profiles. Renaming it breaks saved buttons, so keep it stable. |
| `name`, `category`, `description`, `icon` | What the editor's action picker shows. All go through the plugin's translation file. |
| `fields` | The form the user fills in when binding the action. Same shape as a settings page. See [Field kinds](/reference/js-field-kinds). |
| `run(context, settings)` | Called when the bound widget event fires. May be `async`. |

**`context`**

| Property | Meaning |
|---|---|
| `deviceId` | The phone or browser deck that triggered it. |
| `pageId` | The page the widget is on. |
| `widgetId` | The widget that triggered it. |
| `value` | For a slider or knob (*Value changed*): the value the user dragged to. `null` for every other event. |

**`settings`** is an object with one entry per field `key` of the action's form (values the user did not change hold the field's `default`).

Which events an action can be bound to is up to the user: press, release, long press, double tap, toggle on/off, value changed.
One action serves them all; read `context.value` if you want to react to a slider.

The action counts as finished when `run` returns. Work you `await` inside an `async run` carries on after that.

## host.settings

```js
host.settings.page([
  { key: 'url',  label: 'Server URL', kind: 'Text', default: 'http://localhost:4455' },
  { key: 'poll', label: 'Poll every (s)', kind: 'Number', default: 5, min: 1, max: 60 },
])
const { url, poll } = host.settings.get()
```

- `page(fields)` adds one settings form to the Plugins window (a gear button). Call it at the top level. A second call replaces the first.
- `get()` returns the current values as an object: saved values over the `default`s. It returns `{}` when there is no page. Call it
  again when you need fresh values; users can change them while the plugin runs.
- Values are stored in `settings.json` in the plugin folder. Only keys you declared are kept, and the saved file may be at most 64 KB.
- A `Password` field is stored as plain text in JavaScript plugins; say so in your README.

See [Field kinds](/reference/js-field-kinds) for every kind and option.

## host.status

```js
host.status('conn', 'Connected', 'Ok')
```

Creates or updates an entry in the editor's window-wide status bar. `id` is yours (per plugin). `text` is cut at 80 characters.
`level` is `'Idle'` (default and fallback for anything unknown), `'Ok'`, `'Busy'`, `'Warning'` or `'Error'`; it sets the colour.
At most 10 items per plugin. If the plugin has a settings page, clicking its item opens it. The text goes through the translation
file, and a text with a value in it can be translated as a template (key `"Retrying in {0}s"`).

## host.input

Needs `input`, and works **only while a button press is being handled**: inside an `action`'s `run` (and the promise it returns) for at
most 5 seconds. From a timer or at start-up it throws `Keyboard input is only allowed while handling a button press.` The full list of
limits is in [Permissions](/reference/permissions#the-input-permission).

```js
host.input.hotkey('ctrl+shift+m')
host.input.type('hello')             // at most 200 characters per call, 200 per press
```

- At most 10 key combinations and 200 typed characters per press.
- Combinations with the Windows key (`win`) and `ctrl+escape`, `ctrl+alt+delete` are refused.
- Nothing is sent while a terminal, script host, system tool, system dialog or a Macro Grid window is in front, or when Macro Grid runs as administrator.
- Text that looks like a harmful command throws `This text is not allowed.` and switches the plugin off.

**Combination syntax:** keys joined with `+`, at most one non-modifier key, case-insensitive. A literal `+` key is written `plus`.
A bad combination throws an `Error` with the reason (`Unknown key: 'foo'.`, `More than one key: ...`).

| Group | Names |
|---|---|
| Modifiers | `ctrl` (`control`), `shift`, `alt` (`option`). `win` (`windows`, `meta`, `cmd`) parses but is refused for plugins. |
| Letters and digits | `a` to `z`, `0` to `9`, `num0` to `num9` |
| Function keys | `f1` to `f24` |
| Navigation | `up`, `down`, `left`, `right`, `home`, `end`, `pageup` (`pgup`), `pagedown` (`pgdn`), `insert` (`ins`), `delete` (`del`) |
| Editing | `enter` (`return`), `escape` (`esc`), `tab`, `space`, `backspace` (`bksp`) |
| Locks and misc | `printscreen` (`prtsc`), `pause`, `capslock`, `numlock`, `scrolllock`, `menu` |
| Punctuation | `plus`, `minus`, `comma`, `period`, `semicolon`, `slash`, `backslash`, `quote`, `backquote`, `bracketleft`, `bracketright`, `equal` |
| Keypad | `numadd`, `numsubtract`, `nummultiply`, `numdivide`, `numdecimal` |
| Media | `volumeup`, `volumedown`, `volumemute`, `mediaplaypause`, `medianext`, `mediaprev`, `mediastop` |

The keys go to whichever window has focus, which is why the press rule exists: the user just touched a button on purpose.

## host.http

Needs `http:<host>:<port>` for the exact target of each URL.

```js
const r = host.http.get('http://localhost:4455/status', { headers: { Authorization: 'Bearer x' } })
// r = { status: 200, body: '...text...' }
const data = JSON.parse(r.body)

host.http.post('http://localhost:4455/scene', { name: 'Intro' })   // body is sent as JSON

const r2 = await host.http.getAsync(url)          // promise, does not block
await host.http.postAsync(url, { on: true }, { headers: {} })
```

| Call | Returns |
|---|---|
| `get(url, { headers })` | `{ status, body }`, blocking |
| `post(url, body, { headers })` | `{ status, body }`, blocking. `body` is passed through `JSON.stringify` and sent with `Content-Type: application/json`, so a string is sent quoted. |
| `getAsync(url, { headers })` | a promise for `{ status, body }` |
| `postAsync(url, body, { headers })` | a promise for `{ status, body }` |

- `status` is the HTTP status code; a 404 or 500 is **not** an exception. `body` is text; parse it yourself.
- A refused, failed or timed-out request throws an `Error` (blocking) or rejects the promise (async).
- **Blocking vs async:** `get`/`post` hold the plugin's thread until the answer arrives, so its timers and other actions wait. Use the
  async calls inside `async` actions and timers to keep everything else running. At most 4 async requests in flight per plugin.
- The rules (exact host and port, no redirects, 5 s timeout, 1 MB, GET and POST only) are in [Permissions](/reference/permissions#http-host-port).

## host.every, host.after, host.cancel

```js
const id = host.every(5000, () => { /* repeats */ })
host.after(1000, () => { /* once */ })
host.cancel(id)
```

No permission. The shortest interval is 100 ms, at most 20 timers per plugin. Timers of a busy plugin do not pile up: a tick that arrives while
the script is still busy is dropped. The callback runs on the same single thread as everything else in the plugin. Callbacks can
be `async`.

## host.permissions

A frozen array of the permission strings the user granted, for example `['variables', 'http:localhost:4455']`.

## Translations

Write every text in one language (`defaultLanguage` in `plugin.json`, `en` by default) and add `locales/<language>.json` next to
`plugin.json`: a single object mapping your text to its translation.

```json
{ "Set light": "Işığı ayarla", "Home": "Ev", "Retrying in {0}s": "{0} sn içinde yeniden denenecek" }
```

Translated: action names, descriptions and categories, variable descriptions and categories, settings form labels, and status
texts and tooltips. A missing file or entry falls back to the text as written.

## Limits and failures

| Limit | Value |
|---|---|
| Start-up of the script | 10 seconds |
| Each call into the script (start-up run, an action, a timer tick) | 2 seconds, 32 MB, 2 million statements, recursion depth 100. A call that goes over fails with an error; the plugin keeps running. |
| Timers | 20 per plugin, 100 ms minimum |
| Status items | 10 per plugin |
| HTTP | 5 s per request, 1 MB response, 4 async requests in flight |
| Log line | 500 characters |
| `host.input` | 200 typed characters and 10 key combinations per press, 5 second press window |
| `settings.json` | 64 KB |

- **One thing at a time.** The script runs on its own thread, one call at a time, so a slow plugin never blocks the server or
  another plugin.
- **Five failures in a row switch the plugin off.** Its status shows *Error* with the last message. **Reload** starts it again. One
  successful call resets the count.
- If the script throws while it first runs, the plugin shows *Error* with the message.

## Not possible yet

- A plugin cannot draw its own widget (a `plugin-html` widget is planned) or add an icon pack or a new widget type.
- No files, no sockets, no WebSocket, no `fetch`, no `setTimeout` (use `host.after`), no modules.
- No button inside a settings form (a `Button` field needs code in the server, so it exists only in official C# plugins).
- No dynamic dropdown lists: `Select` options are static (dynamic lists are official-C# only).
- No way to pick which page or profile a phone shows from JavaScript.
