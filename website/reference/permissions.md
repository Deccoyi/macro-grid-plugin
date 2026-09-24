# Permissions

Permissions apply to **JavaScript plugins only**. A C# plugin has full trust and its `permissions` field is ignored.

Declare what the script needs in the `permissions` array of `plugin.json`. The user sees the list in the Plugins window and must
approve it before the script runs (status *Needs approval*). The approval is for that exact set: an update that asks for more waits
for approval again. Removing a plugin forgets its approval.

| Permission | Lets the script |
|---|---|
| `variables` | read any variable and publish its own |
| `actions` | register actions |
| `input` | press key combinations and type text on the PC |
| `http:<host>:<port>` | send HTTP requests to exactly that host and port (one entry per target, for example `http:localhost:4455`) |

Timers, settings pages, status items and logging need no permission.

- An unknown permission string makes the plugin an *Error*.
- A call without its permission throws an ordinary JavaScript `Error` that the script can catch.
- HTTP is exact: only approved `host:port` pairs, redirects are not followed (a redirect could leave the approved host), requests time
  out after 5 seconds and responses are capped at 1 MB.
- Variable names and action types must start with `<plugin id>.` whatever the permissions.

Ask only for what you use. Example manifest fragment from the JavaScript example:

```json
"permissions": ["variables", "actions"]
```

For C# plugins the equivalent is trust: the plugin runs inside the server process and can do anything the server can. See
[Plugin basics](/basics/).
