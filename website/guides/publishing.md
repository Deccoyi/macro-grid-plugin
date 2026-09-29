# Publishing your plugin

Plugins are distributed as plain folders. Publishing means giving people a folder (usually a zip) they
can install from the editor.

## Checklist

1. **Manifest.** `id` is unique and stable, `version` follows [semantic versioning](/basics/compatibility#versioning-your-plugin),
   `minMacroGrid` is the oldest Macro Grid that has what you use (the SDK version you build against, or older).
2. **Permissions** (JavaScript). Ask only for what you use; see [Permissions](/reference/permissions). An update that asks for more
   waits for the user's approval again.
3. **A README** that says what the plugin does, its requirements, its settings and where it stores data. Say so if it stores a
   secret in plain text.
4. **A licence.** Ship a `LICENSE` file, and a `NOTICE.md` if you include third-party code or assets. State where every asset (an
   icon, a font) comes from and that you may redistribute it.
5. **A changelog.** Keep a short public one and a detailed technical one, as the plugins in this repository do.
6. **Test it from a clean state**: remove the plugin, install it from the folder you ship, and follow your own README.

## What to ship

The folder with `plugin.json`, the script and your `LICENSE`. Your plugin must be a JavaScript plugin: the server does not load a C# plugin by another author.

Zip the folder contents. Users either unzip into `%AppData%\MacroGrid\plugins\<id>\` or use **Plugins, Manage Plugins, Install from
Folder...** on the unzipped folder.

## Releases in this repository

The official plugins in this repository are built, signed and released by the maintainer from a tag on `main`, `plugin-<name>-v<version>`
(for example `plugin-obs-v0.2.0`), with a script on the maintainer's PC: the signing key never leaves it. The release zip carries a
signature over its own files, which the server checks every time a C# plugin loads. Pull requests from outside the project are generally
not accepted, because official plugins run with full trust; open an issue first. Your own plugin belongs in your own repository, as a
JavaScript plugin, and people install it from there (see below).

## Running your own source

The host can install a JavaScript plugin from any public GitHub repository the user adds, not only this one — with a clear third-party warning,
since only this repository's releases are signed with the official key. A repository that ships a C# plugin is refused: the server installs a C#
plugin only from the official source. Two shapes are supported:

- **A multi-plugin repository**, added as a source in the Discover tab: keep a `macrogrid-index.json` at your repository's root
  on `main`, listing your own plugins and pointing only at your own repository's releases. Copy this repository's
  `examples/third-party-release.yml` and `scripts/update-plugin-index.ps1` as a starting point and drop the signing step (you have
  no official key, and a signature you added yourself would not be trusted anyway). List `"kind": "js"` plugins only.
- **A single-plugin repository**, installed by pasting its URL: keep `plugin.json` at the root on `main`, always matching the
  latest release, tagged `v<version>` with a `<id>-<version>.zip` and a `<id>-<version>.zip.sha256` asset.

Either way, `macrogrid-index.json` at your root instead of `plugin.json` is what tells the host "this is a multi-plugin
repository" — see [Source index](/reference/source-index) for the exact schema.

## The Store

The [Store](/store/) lists the official plugins of this repository and builds itself from the repository and its GitHub releases. This is how
a plugin gets listed there (only the maintainer adds plugins to this repository; see [Repository rules](/guides/repo-rules)):

1. **Add the folder** at the repository root (for example `MyPlugin/`) with `plugin.json`, a `README.md` (its first section becomes
   "What it does" and its first paragraph the card text), a `CHANGELOG.md` and the licence files, as described above.
2. **Optionally add one line to `website/store/catalog.json`**: `{ "id": "my-plugin", "dir": "MyPlugin", "category": "Integrations", "icon": "code" }`.
   The Store lists every top-level folder that has a `plugin.json` even without this line (category "Other", no icon); the line only
   sets how the card looks. `id` must equal the `id` in `plugin.json`. `icon` is the name of an SVG in `website/public/store/icons/`;
   without one the card shows a letter. Set `"featured": true` to sort it first.
3. **Tag a release** `plugin-<id>-vX.Y.Z` (for example `plugin-my-plugin-v0.1.0`) as described in [Releases](#releases-in-this-repository).
   Once the release is published, the Store shows its zip as the download button. The site is rebuilt whenever a release is published,
   edited or deleted.

Draft releases are never shown. Until a plugin has a published release, its card links to the GitHub Releases page.

## Security notes for users

Tell your users what your plugin can do. A JavaScript plugin runs in a sandbox and shows its permission list before it runs; Macro Grid does
not review plugins from other authors, so say what yours does with each permission, especially `input` and `http`.
