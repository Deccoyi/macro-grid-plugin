# OBS plugin: name and non-affiliation notice

Status: **done** (OBS 0.3.0).

## Decision

- The plugin's display name is **"WebSocket Bridge for OBS"**. "OBS" must not lead a plugin name; "for OBS" is acceptable.
- `id`, the DLL and the namespace stay `obs`, so installed plugins, settings and profiles keep working.
- Every user-facing place says the same thing. English text:

  > This plugin is an independent, third-party project. It is not affiliated with, endorsed by or sponsored by the OBS Project. OBS and OBS Studio are trademarks of their owners. Get OBS Studio at https://obsproject.com/.

  The Turkish text is in `WebSocketBridge/locales/tr.json` under the English sentence as its key.
- The Discover/store card uses a one-line version in the manifest `description`: "Independent project, not affiliated with the OBS Project."

## Where it appears

`WebSocketBridge/plugin.json` (name, description), the plugin's settings window (`ObsSettingsPage`, a `Notice` field), `WebSocketBridge/README.md`, `WebSocketBridge/NOTICE.md`, `WebSocketBridge/locales/tr.json`, `docs/plugin-authoring.md` (the sample manifest), both OBS changelogs.

## Not in this repository

`macrogrid-index.json` carries the plugin's name and is regenerated on release. The `macro-grid` repository's `docs/guides/versioning.md` and `tests/MacroGrid.Tests/PluginCatalogClientTests.cs` use "OBS Control" only as a sample manifest/catalog entry; they are handled in a separate change there.
