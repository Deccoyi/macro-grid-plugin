# Changelogs

Every plugin keeps two changelogs next to its `plugin.json`: a short public one and a detailed technical one.

| Plugin | Short (public) | Detailed (developer) |
|---|---|---|
| OBS | [CHANGELOG.md](https://github.com/Deccoyi/macro-grid-plugin/blob/main/OBS/CHANGELOG.md) | [CHANGELOG-developer.md](https://github.com/Deccoyi/macro-grid-plugin/blob/main/OBS/CHANGELOG-developer.md) |
| PLC Icons | [CHANGELOG.md](https://github.com/Deccoyi/macro-grid-plugin/blob/main/PLCIcons/CHANGELOG.md) | [CHANGELOG-developer.md](https://github.com/Deccoyi/macro-grid-plugin/blob/main/PLCIcons/CHANGELOG-developer.md) |

The plugin SDK and the server have their own changelogs in the
[server repository](https://github.com/Deccoyi/macro-grid/tree/main/docs): `CHANGELOG.md` (short) and `CHANGELOG-developer.md`
(detailed). Read them before you move `sdkVersion`, because a `0.x` SDK minor version can break plugins.

Releases of each plugin are on the [Releases page](https://github.com/Deccoyi/macro-grid-plugin/releases) of this repository, tagged
`plugin-<name>-v<version>`.
