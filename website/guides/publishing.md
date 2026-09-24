# Publishing your plugin

Plugins are distributed as plain folders: there is no plugin store yet. Publishing means giving people a folder (usually a zip) they
can install from the editor.

## Checklist

1. **Manifest.** `id` is unique and stable, `version` follows [semantic versioning](/basics/compatibility#versioning-your-plugin),
   `sdkVersion` is the SDK you built and tested against, `minServerVersion` the oldest server that has what you use.
2. **Permissions** (JavaScript). Ask only for what you use; see [Permissions](/reference/permissions). An update that asks for more
   waits for the user's approval again.
3. **A README** that says what the plugin does, its requirements, its settings and where it stores data. Say so if it stores a
   secret in plain text.
4. **A licence.** Ship a `LICENSE` file, and a `NOTICE.md` if you include third-party code or assets. State where every asset (an
   icon, a font) comes from and that you may redistribute it.
5. **A changelog.** Keep a short public one and a detailed technical one, as the plugins in this repository do.
6. **Clean output** (C#). The folder holds your DLL, your own dependencies and `plugin.json`, and **not**
   `MacroGrid.Plugin.Abstractions.dll`.
7. **Test it from a clean state**: remove the plugin, install it from the folder you ship, and follow your own README.

## What to ship

- **JavaScript plugin:** the folder with `plugin.json`, the script and your `LICENSE`.
- **C# plugin:** the build output folder (`bin\Release\net10.0\`), which already contains `plugin.json`, plus your `LICENSE`.

Zip the folder contents. Users either unzip into `%AppData%\MacroGrid\plugins\<id>\` or use **Plugins, Manage Plugins, Install from
Folder...** on the unzipped folder.

## Releases in this repository

If you contribute a plugin to the official repository, each plugin is released on its own from a tag on `main`:

```
plugin-<name>-v<version>
```

for example `plugin-obs-v0.2.0`. The version must equal `version` in the plugin's `plugin.json`. A release workflow builds that
plugin, zips the output with its licence files and creates a draft pre-release for review. The rules for contributing are in
[Repository rules](/guides/repo-rules).

## Security notes for users

Tell your users what your plugin can do. A C# plugin has full trust, so users should only install it if they trust its source. A
JavaScript plugin shows its permission list before it runs.
