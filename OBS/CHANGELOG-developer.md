# Changelog (developer) — OBS Control

This file tracks the version of this plugin only (independent of the main program — see the "Independent versions" section of `../CONTRIBUTING.md`). The short, public changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]
### Added
- License files: `LICENSE` and `NOTICE.md` are now copied into the build output next to `plugin.json` (documentation and packaging only, no behavior change, no version bump).
- **Integration test suite** (`OBS/tests/MacroStation.Plugin.Obs.Tests/`, xUnit): tests envisioned by `docs/done/obs-plugin-0.2-plan.md`, run against an in-process fake obs-websocket v5 server (`FakeObsServer`, `HttpListener`-based) instead of a real OBS. They cover: handshake with and without a password; a 4009 (wrong password) close stops retrying until the settings change and retries immediately after `NotifySettingsChanged`; an unresponsive server is detected by timeout and reconnected; a sudden disconnect reconnects with backoff; an `ExitStarted` event closes the connection immediately without waiting for the TCP timeout; and when an input is removed (`InputRemoved`), its variables (`obs.input.*.muted/volumeDb`) are deleted with `IVariableStore.Remove`. Run separately with `dotnet test OBS/tests/MacroStation.Plugin.Obs.Tests/` (the repo has no shared `.sln`; each project is built and tested on its own).

## [0.2.0] - 2026-09-23
### Added
- Connection settings now use a schema-driven form through the plugin's own `ObsSettingsPage : IPluginSettingsPage` (Plugin SDK 0.3.0); no OBS-specific code is left in the editor.
- `ObsState` cache: scenes, audio inputs, scene items including groups, profiles and transitions — dropdowns are filled from here without a live connection to OBS.
- The variable catalog grew from 8 to about 45 (stream/record statistics, studio mode, virtual camera/replay buffer, dynamic input/item variables; see `README.md`).
- New actions: `obs.setPreviewScene`, `obs.studioTransition`, `obs.toggleStudioMode`, `obs.setTransition`, `obs.setProfile`, `obs.pauseRecord`, `obs.virtualCam`, `obs.replayBuffer`, `obs.saveReplay`, `obs.adjustVolume`, `obs.setItemVisibility`, `obs.setText`. All existing actions now use a schema-driven form with `IActionDescriptor` + `IOptionsSource`.
- An action pointed at a deleted target (a removed scene/audio input) now fails with an explicit error instead of silently doing nothing.
- Status queries go in a single `RequestBatch` frame (every 1 s while streaming/recording, otherwise every 5 s); scenes, inputs, profiles and so on are updated from events only, with no polling.
- Live connection status in the editor's window-wide status bar.
- If the server is on this machine, after 3 failed connection attempts the `obs64`/`obs32`/`obs` process is looked up. If there is no process, attempts stop and it is checked silently every 5 s, and the status bar shows "OBS · not running". When OBS starts, it reconnects immediately (without waiting for backoff). If an `ExitStarted` event arrives, the process check runs without waiting for 3 failures. If the server is on another machine, normal backoff continues. The goal: do not waste resources on pointless socket attempts while OBS is closed.

### Fixed
- `ObsEventSubscription.Inputs` had the wrong bit value (it was actually Transitions) — events such as `InputMuteStateChanged` never arrived.
- The handshake and every request now have a timeout (5 s); a frozen OBS or a half-open TCP connection no longer looks "connected" forever.
- A request registered on an already closed connection now fails immediately (it used to wait forever).
- A wrong password (close code 4009/4010/4012) no longer retries endlessly or floods the log — it waits until the settings change and the status bar shows "wrong password".
- Other causes of disconnection are retried with a backoff that grows from 2 s to 30 s (with jitter).
- The same state is no longer logged repeatedly, only when it changes.
- A single buffer is reused instead of allocating a new one for every message.
- Unexpected JSON types no longer bring down the receive loop (safe-read helpers).
- When OBS is closing (`ExitStarted`), the connection is closed immediately without waiting for the TCP timeout.
- When the connection drops, every `obs.*` variable is reset/removed, not just the three booleans.

## [0.1.0] - 2026-09-23
### Added
- First release: obs-websocket v5 connection (Hello/Identify handshake, SHA256-based authentication, automatic reconnect).
- Variables: `obs.connected`, `obs.streaming`, `obs.stream.duration`, `obs.recording`, `obs.record.duration`, `obs.scene.current`, `obs.stats.fps`, `obs.stats.cpu`.
- Actions: switch scene, start/stop/toggle streaming and recording, mute/unmute an input and set its level.
- Settings are kept in `%AppData%/MacroStation/plugins/obs/settings.json` (there was no settings screen in the editor yet).
