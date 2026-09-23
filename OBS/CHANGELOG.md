# Changelog — OBS Control

New features and fixes in the OBS plugin. For technical details, see [CHANGELOG-developer.md](CHANGELOG-developer.md).

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
