# Stream Texts — plan

Status: **phase 1 (TXT files) and the overlay server of phase 2 are implemented** in `StreamTexts/` (C#). The SDK change event is still open: it needs a new optional SDK interface (MINOR bump of server and SDK) and a `minMacroGrid` raise, so it is a separate host release. Until then the plugin polls (cheap, and it writes only on change). The overlay runs inside the plugin on its own loopback `HttpListener`, so it needed no host change.

## Goal

Show Macro Grid variables (`user.x`, `system.cpu`, `system.ram`, other plugins' variables) as live text in a stream, like the "text labels" feature of streaming apps. No new widget: the plugin only exposes values to the text sources of streaming apps.

## Decisions

- **Name:** Stream Texts. The name must not contain a third-party brand name (the OBS trademark rule applies).
- **Kind:** official, signed **C#** plugin. Chosen for the lowest resource use: no Jint interpreter, no extra engine memory or thread per plugin, native string and file handling.
- **Output:** plain **TXT files** (phase 1). An HTTP overlay is optional and later (phase 2).

## What the host already allows

- **Reading any variable:** a C# plugin can read any variable by name (`system.*`, `user.*`, other plugins') with `IVariableStore.Get`. The store is handed to the plugin through `IVariableProvider.RunAsync` (`TrackingVariableStore`).
- **No change events, no listing:** the SDK has no change subscription and no way to list variables, so phase 1 polls. That is cheap for a short list of entries.
- **System metrics already exist:** `SystemMetricsProvider` publishes `system.cpu`, `system.ram`, `system.ram.used`, `system.ram.total`, `system.time` and `system.uptime` once per second.
- **No plugin HTTP routes:** plugins cannot add routes to the host's Kestrel server (9820/9821).

## Why TXT first

| | TXT files | Local HTTP (browser source) |
|---|---|---|
| Load on the streaming app | Very low: a native text source reads the file | Every browser source runs a Chromium (CEF) renderer: tens of MB of RAM plus CPU |
| Load on Macro Grid | A write only when a value changes | An extra listener port plus open SSE connections |
| Support | Universal: all major streaming apps have "read from file" | Any app with a browser source |
| Styling | Font, colour and outline set in the streaming app | Full CSS and animation |
| Delay | About 1 s (the streaming app re-reads the file) | Instant |

The host must stay light because people run it next to their streaming app and games. TXT is therefore the default mode.

## Design (phase 1: TXT)

- **Location:** a new `StreamTexts/` folder in this repo. Follow HelloWeather and SoundBoard for structure, and the usual versioning and signing rules (`official-csharp-plugins.md`).
- **Entry point:** an `IPlugin` that registers one `IVariableProvider`. Its `RunAsync`:
  - loops every 500 ms (configurable, minimum 250 ms);
  - renders each entry's template;
  - writes the file only when the rendered text differs from the last write.
- **Atomic write:** write `<name>.txt.tmp`, then `File.Replace` (or `File.Move` when the target does not exist yet), so the streaming app never reads a half-written file.
- **Encoding:** UTF-8 **without BOM**, because some text sources show the BOM as a stray character.
- **Settings:** a list of entries. Each entry has:
  - `fileName`, for example `cpu`. The plugin adds `.txt`. Allowed characters: letters, digits, `-`, `_` and space, at most 64.
  - `template`, for example `CPU: {system.cpu}% | RAM: {system.ram.used}/{system.ram.total} GB`. One file can combine several variables.
  - an optional format per placeholder (`{system.cpu:0}`, `{system.ram.used:0.0}`);
  - an optional `emptyText`, shown when a variable does not exist or has no value.
- **Output folder:** defaults to `Documents\Macro Grid\Stream Texts\`, and the user can change it. The settings page has an "Open folder" button and shows the full path of each file, ready to paste into the streaming app.
- **Cleanup:** when an entry is removed, its file is deleted. Files that were not created by the plugin are never touched.
- **Optional:** the plugin publishes `streamtexts.lastWrite` (a timestamp) for automations.

## Phase 2 (optional, later)

- **SDK change event:** add `IVariableStore.Changed` (or `Subscribe(names)`) so the plugin writes on change and stops polling. Recommended for performance. It needs an SDK version bump.
- **Overlay server:** an opt-in toggle that serves `http://127.0.0.1:<port>/t/<name>`. The page is minimal HTML with a transparent background, updated live over SSE, and takes a `?css=` parameter. It listens on localhost only.

## Alternative considered: JS plugin with a narrow `textfiles` permission

Not chosen, but kept as an option for third-party authors. JS plugins cannot write files or serve HTTP today. That is a deliberate sandbox boundary, not a technical limit. It could be widened safely with a narrow permission:

- **API:** `host.textfiles.write(name, text)` and `host.textfiles.remove(name)`.
- **Folder fixed by the host:** `Documents\Macro Grid\Stream Texts\<pluginId>\`. The name must match `^[\w\- ]{1,64}$`, and the host adds `.txt`. Path traversal and other file types are impossible.
- **Write-only:** no reading and no listing.
- **Quotas:** 64 files, 16 KB per file, 1 MB in total, plus a rate limit on writes.
- **Host-side writing:** the host does the atomic UTF-8 write.
- **User approval:** the permission dialog says "Can write text files to the Stream Texts folder".
- **Worst case:** the disk fills up to the quota. No code can run.
- **Cost:** changes to the JS runtime (`JsPermissions`, `JsPlugin.HostApi`, `JsPlugin.Bootstrap`), the permission dialog and `plugin-authoring.md`. That means a host release and a `minMacroGrid` bump.

For HTTP, a central host route fed by a JS API such as `host.overlay.publish(name, text)` would be safer than letting each plugin open its own port.

## Verification

1. Install the dev copy. Add entries for `system.cpu`, for `user.x`, and one combined template.
2. Check that the files appear, and that a file's timestamp changes only when its value changes.
3. In the streaming app, add a text source with "read from file" pointed at a file. Change `user.x` from a button and check that the text updates within about 1 s.
4. Check in Task Manager that host CPU and RAM barely change with 10+ entries.
