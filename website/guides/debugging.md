# Debugging and logs

## The server log

The server writes one log file per day to `%AppData%\MacroGrid\logs\server-YYYY-MM-DD.log`. Open it in any text editor.

- `host.log(message)` (JavaScript) and `IPluginHost.Log(message)` (C#) write a line to that log, prefixed with your plugin id, for
  example `[hellocsharp] Hello, world! ...`. Search the log for `[<your id>]`.
- Use it for rare events (connection changes, errors), not for polling: there is no per-plugin log level.
- The server also logs plugin loading and unloading, and a warning when an unloaded plugin's assembly is still referenced.

## The Plugins window

Each plugin shows a status, and a message is shown under its name:

| Status | Usual cause |
|---|---|
| **Incompatible** | `macroGrid` asks for a newer Macro Grid than this one, or for another MAJOR; the message under the name says which. See [Compatibility](/basics/compatibility). |
| **Needs approval** | A JavaScript plugin's permissions are not approved yet. |
| **Error** | `plugin.json` could not be parsed; the entry file is missing; the `id` or an action type is already used; a permission string is unknown; `Initialize` or the script's first run threw; or the plugin was switched off after failing 5 times in a row. |

**Reload** tries again after you fix the problem.

## Runtime failures

- **An action throws.** The server catches it, logs it and shows the message on the phone that pressed the widget and in the
  editor's status bar. Throw clear messages. Actions of one phone run one after another, so a slow action delays that phone's next
  action, not other phones.
- **A variable provider throws.** The server logs it and restarts the provider after 5 seconds. A provider that returns normally is
  not restarted.
- **A JavaScript call goes over its budget** (2 seconds, 32 MB, 2 million statements, recursion depth 100): it fails with an error;
  the plugin keeps running. HTTP requests time out after 5 seconds.
- **A JavaScript plugin fails 5 times in a row:** it is switched off, status *Error* with the last message. **Reload** restarts it.
- **A permission is missing.** The call throws an ordinary JavaScript `Error` that the script can catch.

## Common problems

- **The action or variable does not show up.** Variables and action types of JavaScript plugins must start with `<plugin id>.`. For a
  C# plugin check that the status is *Loaded* and that the action type is unique.
- **`is IActionHandler` fails or types are not found.** Your output contains its own copy of `MacroGrid.Plugin.Abstractions.dll`. Use
  `ExcludeAssets="runtime"`.
- **JavaScript registrations are ignored.** Register actions, the settings page and variable descriptions at the top level of the
  script, not from a callback.
- **Files are missing.** `Assembly.Location` is empty inside a C# plugin; use `IPluginHost.DataDirectory`.
- **A rebuilt plugin does not change.** Install from the build output folder again (the same `id` replaces it) or click **Reload**.
- **Memory warning on unload.** Something you started (a thread, a socket, a timer) still runs after the server cancelled your token.
  Stop it in `Dispose` / `DisposeAsync`.
