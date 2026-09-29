# Permissions

Permissions apply to **JavaScript plugins**, which are the only kind third parties can write. (C# plugins are official and signed; the server
does not load any other, and they do not use permissions.)

Declare what the script needs in the `permissions` array of `plugin.json`. The user sees the list in the Plugins window and must
approve it before the script runs (status *Needs approval*). The approval is for that exact set: an update that asks for more waits
for approval again. Removing a plugin forgets its approval.

| Permission | Lets the script |
|---|---|
| `variables` | read any variable and publish its own |
| `actions` | register actions |
| `input` | press key combinations and type text on the PC, while a button press is being handled (see below) |
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

## The `input` permission

`input` lets a plugin press keys and type on the PC as if it were at the keyboard. The person's approval is the real decision, so the
Plugins window says: "Can press keys and type on your PC as if it were at your keyboard, when you press one of its buttons. Allow only for
plugins you trust." On top of the approval, the server enforces these limits for JavaScript plugins (the built-in hotkey and type-text
actions, which the person configures, are unchanged):

- **Only while handling a real press.** `host.input.*` works only inside an action that started with a touch on a device, and only until that
  action and the promise it returns have finished, or 5 seconds have passed, whichever comes first. From a timer, at start-up or from a
  web request that was not started during a press it throws `Keyboard input is only allowed while handling a button press.`
- **Why 5 seconds.** An action often waits for one web request before it types, so the window has to outlast a request; the request itself
  times out after 5 seconds. It is also short on purpose: a press cannot be saved up and reused later, so a plugin cannot type when nobody
  is pressing a button.
- **Small amounts.** At most 200 typed characters and 10 key combinations per press; one `host.input.type` call takes at most 200 characters.
  More throws.
- **Refused key combinations.** Anything with the Windows key (Start, Run, the power-user menu, settings) and the ones that open Task
  Manager or the security screen. The person can still use them through the built-in hotkey action.
- **Refused target windows.** Nothing is sent while a command prompt, shell or terminal, a script host, the registry editor, a system
  management console, Task Manager, the Start menu, a system dialog such as Run, or a Macro Grid window is in front (a plugin must never click
  through its own approval), when the window in front cannot be identified, or when Macro Grid runs as administrator.
- **A blocklist of harmful text.** Text typed during one press is normalized (lowercase, spaces collapsed, escape characters and quotes removed)
  and checked for things like starting a shell or script host, downloading and running something, encoded commands, changing the registry,
  services, scheduled tasks, users or the firewall, and deleting or formatting drives. On a match the call is refused, the plugin is switched off
  and a `Security:` line naming the rule (never the text) goes to the log. This is the weak rule: it can be worked around (spelling tricks,
  typing into another program) and can refuse legitimate text. It catches crude and copied attacks; the press rule and the window rule are what
  actually stop hidden abuse.
- **Visible use.** Every press that used the keyboard is counted, and the plugin's row in the Plugins window shows "Used the keyboard N times today".

**What this does not solve:** an approved plugin with `input` can still type up to 200 characters into an ordinary program when you press its
button. Allow `input` only for plugins you trust.
