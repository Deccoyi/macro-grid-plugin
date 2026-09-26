# Changelog — OBS Control

New features and fixes in the OBS plugin. For technical details, see [CHANGELOG-developer.md](CHANGELOG-developer.md).

## Unreleased

## 0.2.2 - 2026-09-25
### Changed
- **Needs Macro Grid 0.3.2 or newer:** this version works with the new plugin system, so an older Macro Grid will not load it.
- **Plugin catalog:** the plugin now has a description and author for the Discover tab.

## 0.2.1 - 2026-09-24
### New
- **Languages:** Action names, form labels, values and status texts are shown in Turkish or English, following the language of the app.

## 0.2.0 - 2026-09-23
### New
- **Settings in the editor:** OBS settings use the same ready-made form as other plugins.
- **Many more actions:** studio mode, transitions, profiles, virtual camera, replay buffer, volume and text.
- **Many more values:** stream and record statistics and more, ready to show on your buttons.
- **Live status:** The bottom bar shows whether OBS is connected, closed or needs a password.
- **Smarter reconnect:** When OBS is closed, the plugin waits quietly and reconnects as soon as OBS opens.

### Fixed
- A button linked to a deleted scene or input now shows an error instead of doing nothing.
- A wrong password no longer makes the plugin retry forever.

## 0.1.0 - 2026-09-23
- First release: connect to OBS, switch scenes, start and stop streaming and recording, and control input volume.
