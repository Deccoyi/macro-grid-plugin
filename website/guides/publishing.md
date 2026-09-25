# Publishing your plugin

Plugins are distributed as plain folders. Publishing means giving people a folder (usually a zip) they
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
plugin, zips the output with its licence files, hashes and signs the zip, publishes the release (not a draft), and commits the
plugin's new version into `macrogrid-index.json` on `main` — see [Source index](/reference/source-index) for that file's format
and what the signature covers. The rules for contributing are in [Repository rules](/guides/repo-rules).

## Running your own source

The host can install from any public GitHub repository the user adds, not only this one — with a clear third-party warning,
since only this repository's releases are signed with the official key. Two shapes are supported:

- **A multi-plugin repository**, added as a source in the Discover tab: keep a `macrogrid-index.json` at your repository's root
  on `main`, listing your own plugins and pointing only at your own repository's releases. Copy this repository's
  `examples/third-party-release.yml` and `scripts/update-plugin-index.ps1` as a starting point and drop the signing step (you have
  no official key, and a signature you added yourself would not be trusted anyway).
- **A single-plugin repository**, installed by pasting its URL: keep `plugin.json` at the root on `main`, always matching the
  latest release, tagged `v<version>` with a `<id>-<version>.zip` and a `<id>-<version>.zip.sha256` asset.

Either way, `macrogrid-index.json` at your root instead of `plugin.json` is what tells the host "this is a multi-plugin
repository" — see [Source index](/reference/source-index) for the exact schema.

## Getting your plugin into the Store

The [Store](/store/) lists the plugins of this repository and builds itself from the repository and its GitHub releases, so listing a
plugin takes three steps (for a contribution, follow the [Repository rules](/guides/repo-rules) first):

1. **Add the folder** at the repository root (for example `MyPlugin/`) with `plugin.json`, a `README.md` (its first section becomes
   "What it does" and its first paragraph the card text), a `CHANGELOG.md` and the licence files, as described above.
2. **Add one line to `website/store/catalog.json`**: `{ "id": "my-plugin", "dir": "MyPlugin", "category": "Integrations", "icon": "code" }`.
   `id` must equal the `id` in `plugin.json`. `icon` is the name of an SVG in `website/public/store/icons/`; without one the card shows
   a letter. Set `"featured": true` to sort it first.
3. **Tag a release** `plugin-<id>-vX.Y.Z` (for example `plugin-my-plugin-v0.1.0`) as described in [Releases](#releases-in-this-repository).
   Once the release is published, the Store shows its zip as the download button. The site is rebuilt whenever a release is published,
   edited or deleted.

Draft releases are never shown. Until a plugin has a published release, its card links to the GitHub Releases page.

## Security notes for users

Tell your users what your plugin can do. A C# plugin has full trust, so users should only install it if they trust its source. A
JavaScript plugin shows its permission list before it runs.
