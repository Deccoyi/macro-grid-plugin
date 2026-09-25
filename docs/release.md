# Releasing a plugin

The one release guide for every part of Macro Grid (tags, the order of a release, all signing keys) is [docs/guides/release.md in the server repository](https://github.com/Deccoyi/macro-grid/blob/main/docs/guides/release.md); read it first. This page only has what is specific to a plugin release.

Every plugin is released on its own, from a tag. Releases are cut from `main`, on the maintainer's machine: the plugin-signing key never leaves it, and no workflow in this repository signs or publishes a plugin.

## Tag naming

```
plugin-<name>-v<version>
```

| Plugin | `<name>` (also the release script's `-Name`) | Example tag |
|---|---|---|
| `OBS/` | `obs` | `plugin-obs-v0.2.2` |
| `PLCIcons/` | `plc-icons` | `plugin-plc-icons-v0.1.3` |
| `SoundBoard/` | `soundboard` | `plugin-soundboard-v0.1.1` |
| `HelloJs/` | `hellojs` | `plugin-hellojs-v0.1.1` |

`<version>` is the plugin's own semantic version and must equal `version` in that plugin's `plugin.json`; the release script takes the
version from `plugin.json` and refuses a tag that already exists. The server, the SDK and the phone app use their own tag schemes (`server-v...`, `client-v...`) in their own repositories.

## Checklist

1. On `dev`, move the plugin's `[Unreleased]` entries in both changelogs to a dated version heading.
2. Set `version` in the plugin's `plugin.json` to the new version. Check `macroGrid` there: the oldest Macro Grid the plugin runs on (`MAJOR.MINOR.PATCH`). Keep it as low as what the plugin really uses; raise it (together with `MacroGridSdkVersion` in `Directory.Build.props`) only when the plugin starts to use something added in a newer MINOR of the SDK. The build fails when it has another MAJOR than the SDK or is newer than it. The Macro Grid version it asks for must already be released, and for a C# plugin its SDK package must be on nuget.org.
3. Make sure CI is green on `dev`, then merge `dev` into `main`.
4. Check out `main` (clean, equal to `origin/main`) and run the release script. Without `-Publish` it only builds and signs into a
   temp folder, so you can inspect the zip first:
   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/release-plugin.ps1 -Name obs
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/release-plugin.ps1 -Name obs -Publish
   ```
   `-Name` is `obs`, `plc-icons`, `hellojs` or `soundboard`. The key defaults to `%USERPROFILE%\signing\plugin-signing\plugin-signing-private.pem`
   (`-KeyPath` overrides it); it is never in a repository and is never printed.
5. The script builds that plugin, zips the output together with `LICENSE`, `NOTICE.md` and `THIRD_PARTY_NOTICES.md`, hashes the zip
   (`<zip>.sha256`) and signs it with the plugin-signing key (`<zip>.sig`, `scripts/sign-package.cs`), then publishes the release with
   `gh` (not a draft, tag `plugin-<name>-v<version>`). It then commits the plugin's new version into `macrogrid-index.json` on `main`
   (`scripts/update-plugin-index.ps1`, which writes `macroGrid`, plus `sdkVersion` and `minServerVersion` when `plugin.json` still has them), the file the host and the Store read; see
   [Source index](../website/reference/source-index.md) for its format. The index stays `formatVersion` 1 because a server before 1.0.0 refuses any other number.
6. Every official release must be signed, so there is no unsigned path. Keep the key folder backed up and out of every repository (the guide above says why and what happens if it is lost).
   `.github/workflows` has no release workflow on purpose; `examples/third-party-release.yml` is a signing-free template for other repositories.

## Installing a released plugin

Unzip the archive into `%AppData%\MacroGrid\plugins\<id>\` (the folder that holds `plugin.json`), or use **Plugins > Manage Plugins >
Install from Folder** in the editor.
