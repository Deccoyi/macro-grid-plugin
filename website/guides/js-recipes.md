# What you can build

A JavaScript plugin has eight tools: **variables**, **actions**, a **settings page**, **status items**, **keyboard input**, **HTTP**,
**timers** and **translations**. Each one is small; the plugins come from combining them. This page shows the combinations, each with
the permissions it needs and a working sketch. The details of every call are in the
[JavaScript host API](/reference/js-host-api), the form options in [Field kinds](/reference/js-field-kinds).

| Idea | Tools | Permissions |
|---|---|---|
| Show a value from a local app or service on a button | HTTP, timers, variables | `variables`, `http:` |
| A button that calls a local HTTP API | actions, HTTP, settings | `actions`, `http:` |
| React to another value (a warning when the CPU is busy) | variables, timers, status | `variables` |
| A slider that controls something | actions (`context.value`), HTTP | `actions`, `http:` |
| A button that sends a shortcut with your own logic | actions, keyboard | `actions`, `input` |
| A counter, timer or clock you design yourself | timers, variables | `variables` |
| A connection light in the status bar | HTTP, status | `http:` |
| A plugin with an editable list of targets | settings `List`, actions | `actions` (+ what it calls) |

## 1. Show a value from a local service

Poll an HTTP API and publish the result. Widgets show it as `{home.temp}`.

```json
{ "id": "home", "name": "Home", "version": "1.0.0", "minMacroGrid": "1.3.0", "kind": "js", "entry": "index.js",
  "permissions": ["variables", "http:localhost:8123"] }
```

```js
host.variables.describe([
  { name: 'home.temp', description: 'Living room temperature', example: '21.5', category: 'Home', type: 'number', unit: '°C' },
])

host.every(10000, async () => {
  try {
    const r = await host.http.getAsync('http://localhost:8123/api/temp')
    host.variables.set('home.temp', JSON.parse(r.body).value)
    host.status('home', 'Online', 'Ok')
  } catch (e) {
    host.status('home', 'Offline', 'Error')
  }
})
```

Type `Temp {home.temp|0.0}°` in a widget's text. The `async` version keeps other timers and actions running while it waits.

## 2. A button that calls an API, with a form

The user chooses the light and the state when binding the action; the address lives on the settings page.

```json
"permissions": ["actions", "http:localhost:8123"]
```

```js
host.settings.page([{ key: 'base', label: 'Server', kind: 'Text', default: 'http://localhost:8123' }])

host.registerAction({
  type: 'home.light', name: 'Set light', category: 'Home',
  fields: [
    { key: 'light', label: 'Light', kind: 'Text', placeholder: 'kitchen' },
    { key: 'state', label: 'State', kind: 'Segmented', default: 'on',
      options: [{ value: 'on', label: 'On' }, { value: 'off', label: 'Off' }] },
  ],
  async run(context, settings) {
    const base = host.settings.get().base
    await host.http.postAsync(base + '/api/light', { name: settings.light, on: settings.state === 'on' })
  },
})
```

Remember the exact-target rule: if the user changes the address to another host or port, the plugin needs that `http:` permission too.
Keep the target fixed or document what to declare.

## 3. React to other values

`variables` lets you *read* everything. Turn the status bar red when the CPU is busy:

```js
host.every(2000, () => {
  const cpu = host.variables.get('system.cpu')
  if (cpu === null) return
  host.status('cpu', 'CPU ' + Math.round(cpu) + '%', cpu > 90 ? 'Error' : cpu > 70 ? 'Warning' : 'Ok')
  host.variables.set('watch.hot', cpu > 90)      // a widget can now colour itself with a dynamic rule
})
```

Reading works for other plugins' variables too, so one plugin can combine several sources (`obs.streaming` and `system.cpu`) into one
derived variable.

## 4. A slider that controls something

A slider or knob triggers the action on *Value changed* and passes the value in `context.value`.

```js
host.registerAction({
  type: 'home.dim', name: 'Dim light', category: 'Home',
  run(context) {
    if (context.value === null) return          // bound to a button, not a slider
    host.http.post('http://localhost:8123/api/dim', { level: Math.round(context.value) })
  },
})
```

## 5. A shortcut with your own logic

Only the plugin knows whether to send the shortcut. Toggle between two keys by remembering state:

```json
"permissions": ["actions", "input"]
```

```js
let muted = false
host.registerAction({
  type: 'keys.talk', name: 'Push to talk', category: 'Keyboard',
  run() {
    muted = !muted
    host.input.hotkey(muted ? 'ctrl+shift+m' : 'ctrl+shift+u')
    host.status('talk', muted ? 'Muted' : 'Live', muted ? 'Warning' : 'Ok')
  },
})
```

`host.input.type` types a whole text; combine it with a form field (`allowVariables: true`) so the text can contain live values.

## 6. A timer you design yourself

No permission beyond `variables`:

```js
let left = 0, id = null
host.registerAction({
  type: 'tools.countdown', name: 'Start countdown', category: 'Tools',
  fields: [{ key: 'seconds', label: 'Seconds', kind: 'Number', default: 60, min: 1, max: 3600 }],
  run(context, settings) {
    if (id !== null) host.cancel(id)
    left = settings.seconds
    id = host.every(1000, () => {
      host.variables.set('tools.left', left)
      if (--left < 0) { host.cancel(id); id = null }
    })
  },
})
```

## 7. A list of targets

A `List` field lets users add as many rows as they like, and the action can offer them by name:

```js
host.settings.page([{ key: 'hosts', label: 'Hosts', kind: 'List', itemFields: [
  { key: 'name', label: 'Name', kind: 'Text' }, { key: 'url', label: 'URL', kind: 'Text' } ] }])

host.every(15000, async () => {
  for (const h of host.settings.get().hosts ?? []) {
    try { await host.http.getAsync(h.url); host.variables.set('ping.' + h.name, true) }
    catch { host.variables.set('ping.' + h.name, false) }
  }
})
```

Only approved `host:port` targets work, so this pattern fits when the user's targets are known to the plugin author (a plugin
that pings any address would need the user to approve each one).

## Design tips

- **One job per plugin.** A small plugin is easier to approve than a big one that asks for everything.
- **Stable names.** A renamed action `type` or variable breaks saved buttons; that is a MAJOR version bump of your plugin.
- **Cheap timers.** Poll no faster than you need. Prefer `getAsync` so a slow service does not stall the plugin.
- **Handle failure.** `try/catch` around HTTP, set a status level of `Error` and keep the timer running. Five uncaught failures in a
  row switch the plugin off.
- **Say what you send.** For `input` and internet targets, tell users in your README what the plugin will do.

Next: [Debugging](/guides/debugging), then [Publishing](/guides/publishing).
