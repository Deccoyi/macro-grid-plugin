# Permissions

Permissions apply to **JavaScript plugins**. A script starts with no power at all: it can only compute, keep timers, show a settings
page, write to the log and update the status bar. Anything that touches the outside (other variables, the keyboard, the network,
the action list) needs a permission that the user has approved.

## The four permissions

| Permission | Lets the script | Calls it unlocks |
|---|---|---|
| `variables` | read **any** variable on the server and publish, change or remove its **own** | `host.variables.set / get / remove / describe` |
| `actions` | add its own actions to the editor's action picker | `host.registerAction` |
| `input` | press key combinations and type text on the PC, as if the user did it | `host.input.hotkey / type` |
| `http:<host>:<port>` | send HTTP requests to exactly that host and port | `host.http.get / post / getAsync / postAsync` |

There are no other permission names. `http:` is the only one that takes a value, and you write one entry per target.

### `variables`

- Reading is not limited to your own values: `host.variables.get('system.cpu')`, `get('obs.streaming')` or another plugin's
  variable all work. That is how a plugin can react to what is happening elsewhere.
- Writing is limited to names that start with `<plugin id>.`. A plugin can never overwrite `system.*` or another plugin's values.
- Without this permission a plugin cannot publish live data at all, so almost every data plugin asks for it.

### `actions`

- Adds entries to the action picker. The user binds them to a widget event; the script's `run` function then executes.
- Action types must start with `<plugin id>.` (`hellojs.bump`), so a saved profile can always tell whose action it is.
- A plugin without it can still be useful as a pure data source (`variables` only).

### `input`

- `host.input.hotkey('ctrl+shift+m')` sends a key combination, `host.input.type('text')` types text (up to 2000 characters per call).
- This is the most powerful local permission: whatever window has focus receives the keys. Ask for it only if the plugin's whole point
  is to send input, and tell users in your README when it will do so.
- The accepted key names are listed under [`host.input`](/reference/js-host-api#host-input).

### `http:<host>:<port>`

The value is the exact target, for example:

```json
"permissions": ["http:localhost:4455", "http:192.168.1.20:8080", "http:api.example.com:443"]
```

Rules, all enforced by the server:

| Rule | Detail |
|---|---|
| Exact match | The request URL's host and port must equal an approved entry. There are no wildcards, no subdomain matching and no path rules. |
| The port is always written | `http://localhost/x` uses port 80, so it needs `http:localhost:80`. `https://` without a port uses 443, so it needs `http:<host>:443`. The permission name starts with `http:` even for `https://` URLs. |
| Case | Permission strings are case-insensitive. |
| Allowed format | `http:` + letters, digits, `.` or `-` + `:` + 1 to 5 digits. Anything else is an unknown permission and the plugin shows as *Error*. IPv6 literals are not accepted. |
| Schemes | Only `http://` and `https://` URLs. |
| Methods | `GET` and `POST`. A POST body is always sent as JSON. |
| Redirects | Not followed. A redirect could send the request to a host nobody approved. Request the final address yourself. |
| Timeout | 5 seconds per request. |
| Response size | At most 1 MB of text. A bigger response fails. |
| Concurrency | Async calls: at most 4 requests in flight per plugin. |

## What the user sees

Before the plugin runs (and before it is installed from a folder), the Plugins window lists the permissions in plain language:

| Permission | Shown as |
|---|---|
| `variables` | Read variables and publish its own |
| `actions` | Add its own actions |
| `input` | Press keys and type text on this PC |
| `http:<host>:<port>` | Send web requests to *target* **on this computer**, **on your local network**, or **on the internet (it can send data out of your network)** |

The HTTP line is labelled by where the target is: `localhost`, `127.x.x.x` and `::1` are *this computer*; `10.x`, `172.16-31.x`, `192.168.x`,
`169.254.x`, single-word names (`nas`) and names ending in `.local`, `.lan`, `.home.arpa` or `.internal` are the *local network*;
everything else is *the internet*. Users are more careful about internet targets, so ask for the narrowest one you can.

## Approval

1. The plugin's status is *Needs approval* until the user approves. Nothing of the script runs before that.
2. The approval is for **that exact set**. An update that adds a permission (or a new `http:` target) waits for approval again.
3. Removing a plugin forgets its approval. Installing it again asks again.
4. Approvals, installs and removals are written to the log files.

## What needs no permission

| Call | Notes |
|---|---|
| `host.log` | Written to the server log, 500 characters per line. |
| `host.settings.page / get` | A settings form stored in `settings.json` in the plugin folder. |
| `host.status` | Up to 10 status bar items per plugin. |
| `host.every / after / cancel` | At most 20 timers, 100 ms minimum. |
| `host.permissions` | The list of granted permissions, so a script can adapt. |

## Errors

- **Unknown permission string** in `plugin.json`: the plugin is an *Error* and does not start.
- **A call without its permission** throws an ordinary JavaScript `Error` that the script can `try/catch`. Messages look like
  `This plugin has not been granted the 'input' permission.` or `This plugin has not been granted 'http:localhost:4455'.`
- **Bad names** (`Variable names must start with 'myplugin.'`) throw in the same way, whatever permissions are granted.
- Five failures in a row switch the plugin off; see [JavaScript host API](/reference/js-host-api#limits-and-failures).

## Choosing permissions

| The plugin | Ask for |
|---|---|
| shows a computed or fetched value | `variables` (+ `http:...` if it fetches) |
| adds a button action that does something in its own code | `actions` |
| reacts to system values (say, turns a status red when `system.cpu` is high) | `variables` |
| sends a shortcut when something happens | `input` (+ `actions` if a button triggers it) |
| talks to a local app with an HTTP API | `http:localhost:<port>` |

Ask only for what you use. A plugin that asks for nothing shows "asks for no special permissions".

```json
"permissions": ["variables", "actions", "http:localhost:4455"]
```

::: tip
`host.permissions` is a frozen array of what the user granted. A plugin can check it and, for example, hide an optional feature instead of failing.
:::

C# plugins have no permission gate and are limited to the official repository, so this page does not cover them.
