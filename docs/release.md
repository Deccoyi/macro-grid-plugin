# Releasing a plugin

Every plugin is released on its own, from a tag. Releases are cut from `main`, on the maintainer's machine: the plugin-signing key never leaves it.

## Tag naming

```
plugin-<name>-v<version>
```

| Plugin | `<name>` | Example tag |
|---|---|---|
| `OBS/` | `obs` | `plugin-obs-v0.2.0` |
| `PLCIcons/` | `plcicons` | `plugin-plcicons-v0.1.1` |
| `HelloJs/` | `hellojs` | `plugin-hellojs-v0.1.0` |
| `SoundBoard/` | `soundboard` | `plugin-soundboard-v0.1.0` |

`<version>` is the plugin's own semantic version and must equal `version` in that plugin's `plugin.json`; the release script takes the
version from `plugin.json` and refuses a tag that already exists. The server and the phone app use their own tag schemes (`server-v...`, `client-v...`) in their own repositories.

## Checklist

1. On `dev`, move the plugin's `[Unreleased]` entries in both changelogs to a dated version heading.
2. Set `version` in the plugin's `plugin.json` to the new version. Check `sdkVersion` and `minServerVersion` are still honest.
3. Make sure CI is green on `dev`, then merge `dev` into `main`.
4. Check out `main` (clean, equal to `origin/main`) and run the release script. Without `-Publish` it only builds and signs into a
   temp folder, so you can inspect the zip first:
   ```
   ./scripts/release-plugin.ps1 -Name obs
   ./scripts/release-plugin.ps1 -Name obs -Publish
   ```
   `-Name` is `obs`, `plc-icons`, `hellojs` or `soundboard`. The key defaults to `%USERPROFILE%\signing\plugin-signing\plugin-signing-private.pem`
   (`-KeyPath` overrides it). C# plugins need the SDK version they reference to exist on nuget.org first.
5. The script builds that plugin, zips the output together with `LICENSE`, `NOTICE.md` and `THIRD_PARTY_NOTICES.md`, hashes the zip
   (`<zip>.sha256`) and signs it with the plugin-signing key (`<zip>.sig`, `scripts/sign-package.cs`), then publishes the release with
   `gh` — not a draft, tag `plugin-<name>-v<version>`. It then commits the plugin's new version into `macrogrid-index.json` on `main`
   (`scripts/update-plugin-index.ps1`), the file the host and the Store read; see
   [Source index](../website/reference/source-index.md) for its format.
6. Every official release must be signed, so there is no unsigned path. Keep the key folder backed up and out of every repository.
   `.github/workflows` has no release workflow on purpose; `examples/third-party-release.yml` is a signing-free template for other repositories.
7. Publishing the release starts the **Documentation site** workflow (`pages.yml`), which rebuilds the Store with the new zip and
   changelog. Check that the run finished green (`gh run list --workflow pages.yml -L 1`) and the plugin's Store page shows the new
   version. A new plugin needs no extra step: the Store lists every top-level folder with a `plugin.json` (`website/store/catalog.json`
   only sets its category and icon). If `deploy` fails with "not allowed to deploy to github-pages due to environment protection
   rules", the `github-pages` environment must allow the tag pattern `plugin-*-v*` (Settings, Environments).

## Installing a released plugin

Unzip the archive into `%AppData%\MacroGrid\plugins\<id>\` (the folder that holds `plugin.json`), or use **Plugins > Manage Plugins >
Install from Folder** in the editor.
