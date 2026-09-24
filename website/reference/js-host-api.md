# JavaScript host API

A plugin with `"kind": "js"` is one script (`entry`, usually `index.js`) that runs in a sandbox inside the server. Everything goes
through the global, read-only `host` object. There is no `require`, no `fetch`, no file access and no access to .NET.

```js
host.log(message)

host.variables.set(name, value)      // number, string, boolean or null
host.variables.get(name)
host.variables.remove(name)
host.variables.describe([{ name, description, example, category }])   // list them in the editor's variable picker

host.registerAction({ type, name, category, description, icon, fields, run(context, settings) {} })
// context: { deviceId, pageId, widgetId, value }; settings: the values of the action's fields
// fields: the same shape as the C# SettingField, e.g. { key, label, kind: 'Text' | 'Number' | 'Bool' | 'Select' | ..., default, min, max, options }

host.settings.page(fields)           // adds a settings page (stored in settings.json in the plugin folder)
host.settings.get()                  // the current values as an object
host.status(id, text, level)         // status bar item; level: 'Idle' | 'Ok' | 'Busy' | 'Warning' | 'Error'

host.input.hotkey('ctrl+shift+m')    // needs 'input'
host.input.type('hello')             // needs 'input'

host.http.get(url, { headers })          // needs http:<host>:<port>; returns { status, body } (body is text), synchronous
host.http.post(url, body, { headers })   // the body is sent as JSON

const id = host.every(ms, fn)        // repeat; the shortest interval is 100 ms
host.after(ms, fn)                   // once
host.cancel(id)
host.permissions                     // what was granted
```

A complete script using `variables`, `settings`, `registerAction`, `status` and `every` is
[Tutorial 1](/tutorials/js-hello-world).

## Which permission does what

| Call | Needs |
|---|---|
| `host.variables.*` | `variables` |
| `host.registerAction` | `actions` |
| `host.input.*` | `input` |
| `host.http.*` | `http:<host>:<port>` for that exact target |
| `host.log`, `host.settings.*`, `host.status`, timers | nothing |

See [Permissions](/reference/permissions).

## Rules the server enforces

- **Names are yours.** Variable names and action types must start with `<plugin id>.`, so a plugin can never overwrite `system.cpu` or
  another plugin's values.
- **Register at the top level.** Actions, the settings page and variable descriptions must be registered while the script first runs;
  registrations made later from a callback are ignored. Variables can be set at any time.
- **Time and memory are limited per call** (start-up, each action, each timer tick): 2 seconds, 32 MB, 2 million statements and a
  recursion depth of 100. A call that goes over fails with an error; the plugin keeps running. HTTP requests time out after 5 seconds,
  responses are capped at 1 MB and redirects are not followed.
- **One thing at a time.** The script runs on its own thread, one call at a time, so a slow plugin never blocks the server or another
  plugin. `host.http` blocks the plugin's own other callbacks while it waits. At most 20 timers per plugin; ticks that pile up while
  the script is busy are dropped.
- **A plugin that fails 5 times in a row is switched off** (status *Error* with the last message). Reload starts it again.

There is no `async`/`await` host API yet and no way for a plugin to draw its own widget (a `plugin-html` widget is planned).

## Field declarations

`fields` (for an action or a settings page) use the same shape as the C# `SettingField`; see
[Settings pages](/guides/settings-pages#field-kinds-and-options). `kind` is one of `Text`, `Password`, `Number`, `Slider`, `Bool`,
`Select`, `Segmented`.
