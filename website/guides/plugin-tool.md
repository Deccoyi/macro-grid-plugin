# Check, try and package your plugin

The plugin tool is a small command-line program for JavaScript plugin authors. It is a separate download on the Macro Grid release page (`MacroGrid-PluginTool-<version>-win-x64.zip`) and uses the same rules and the same script runtime as the app, so a plugin it accepts is one the app accepts.

| Command | What it does |
|---|---|
| `macrogrid-plugin new <id>` | Starts a plugin in a folder named after the id: a manifest, a small working script and a README. |
| `macrogrid-plugin validate <folder>` | Checks the folder without running anything. It prints one `error:` or `warning:` line for each finding and ends with `OK: <id> <version>` or `FAILED: <n> error(s)`. |
| `macrogrid-plugin run <folder> --action <type>` | Starts the plugin in a temporary data folder, runs the action once and lists the permissions, actions, settings, status items and variables. `--deny <permission>` starts it without that permission, `--seconds <n>` lets timers fire. |
| `macrogrid-plugin pack <folder>` | Checks the folder, then writes `<id>-<version>.zip` and a `.sha256` file. Saved plugin data (`settings.json`, `storage.json`) and hidden files are left out. It does not sign anything. |

Exit code 0 means done, 1 means the plugin has errors, 2 means the command line was not understood.

Use `run` on your own plugin. To try someone else's plugin, install it in the app, where it asks for approval of its permissions. A plugin started by `run` can never reach Macro Grid's own ports.

A package made by `pack` can be installed with **Install from Folder** in the Plugins window after you unzip it, or published as described in [Publishing your plugin](/guides/publishing).
