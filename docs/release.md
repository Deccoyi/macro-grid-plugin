# Releasing a plugin

Every plugin is released on its own, from a tag. Releases are cut from `main`.

## Tag naming

```
plugin-<name>-v<version>
```

| Plugin | `<name>` | Example tag |
|---|---|---|
| `OBS/` | `obs` | `plugin-obs-v0.2.0` |
| `PLCIcons/` | `plcicons` | `plugin-plcicons-v0.1.1` |
| `HelloJs/` | `hellojs` | `plugin-hellojs-v0.1.0` |

`<version>` is the plugin's own semantic version and must equal `version` in that plugin's `plugin.json`; the workflow refuses a tag
that does not match. The server and the phone app use their own tag schemes (`server-v...`, `client-v...`) in their own repositories.

## Checklist

1. On `dev`, move the plugin's `[Unreleased]` entries in both changelogs to a dated version heading.
2. Set `version` in the plugin's `plugin.json` to the new version. Check `sdkVersion` and `minServerVersion` are still honest.
3. Make sure CI is green on `dev`, then merge `dev` into `main`.
4. Tag the merge commit on `main` and push the tag:
   ```
   git tag plugin-obs-v0.2.0
   git push origin plugin-obs-v0.2.0
   ```
5. The `Release plugin` workflow (`.github/workflows/release.yml`) builds that plugin, zips the output together with `LICENSE`,
   `NOTICE.md` and `THIRD_PARTY_NOTICES.md`, hashes the zip (`<zip>.sha256`) and signs it with the repository's plugin-signing
   key (`<zip>.sig`), then publishes the release — not a draft. It then commits the plugin's new version into
   `macrogrid-index.json` on `main`, the file the host and the Store read; see
   [Source index](../website/reference/source-index.md) for its format.
6. The workflow refuses to run if the `PLUGIN_SIGNING_PRIVATE_KEY` secret is missing; every official release must be signed.

## Installing a released plugin

Unzip the archive into `%AppData%\MacroGrid\plugins\<id>\` (the folder that holds `plugin.json`), or use **Plugins > Manage Plugins >
Install from Folder** in the editor.
