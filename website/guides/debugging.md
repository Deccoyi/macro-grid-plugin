# Debugging and logs

## The server log

The server writes one log file per day to `%AppData%\MacroGrid\logs\server-YYYY-MM-DD.log`. Open it in any text editor.

- `host.log(message)` writes a line to that log, prefixed with your plugin id, for
  example `[hellojs] Hello, world! ...`. Search the log for `[<your id>]`.
- Use it for rare events (connection changes, errors), not for polling: there is no per-plugin log level.
- The server also logs plugin loading and unloading, and a warning when an unloaded plugin's assembly is still referenced.

## The Plugins window

Each plugin shows a status, and a message is shown under its name:

| Status | Usual cause |
|---|---|
| **Incompatible** | `minMacroGrid` asks for a newer Macro Grid than this one, or for another MAJOR; the message under the name says which. See [Compatibility](/basics/compatibility). |
| **Needs approval** | A JavaScript plugin's permissions are not approved yet. |
| **Not allowed** | A C# plugin that is not officially signed, or whose files no longer match its signature. Third-party plugins must be JavaScript; a C# plugin is never loaded unless the official key signed it. |
| **Error** | `plugin.json` could not be parsed; the entry file is missing; the `id` or an action type is already used; a permission string is unknown; `Initialize` or the script's first run threw; or the plugin was switched off after failing 5 times in a row. |

**Reload** tries again after you fix the problem. A plugin that is *Not allowed* can only be removed.

## Runtime failures

- **An action throws.** The server catches it, logs it and shows the message on the phone that pressed the widget and in the
  editor's status bar. Throw clear messages. Actions of one phone run one after another, so a slow action delays that phone's next
  action, not other phones.
- **A JavaScript call goes over its budget** (2 seconds, 32 MB, 2 million statements, recursion depth 100): it fails with an error;
  the plugin keeps running. HTTP requests time out after 5 seconds.
- **A JavaScript plugin fails 5 times in a row:** it is switched off, status *Error* with the last message. **Reload** restarts it.
- **A permission is missing.** The call throws an ordinary JavaScript `Error` that the script can catch.

## Common problems

- **The action or variable does not show up.** Variables and action types must start with `<plugin id>.`. Check that the status is *Loaded* and that the action type is unique.
- **`host.input` throws.** Keyboard input works only while a button press is being handled, for at most 5 seconds after it, and it is refused
  when a terminal, a system tool or a Macro Grid window is in front. See [Permissions](/reference/permissions#the-input-permission).
- **JavaScript registrations are ignored.** Register actions, the settings page and variable descriptions at the top level of the
  script, not from a callback.
- **A changed script does not take effect.** Install the folder again (the same `id` replaces it) or click **Reload**.
