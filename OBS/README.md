# OBS plugin

Controls [OBS Studio](https://obsproject.com/) from Macro Grid over obs-websocket v5, which is built into OBS 28 and newer. It adds
22 actions (scenes, streaming, recording, audio, scene items, text sources) and about 45 live `obs.*` variables you can show on
widgets. Kind: C# plugin. Id: `obs`.

## Requirements

- OBS Studio 28 or newer.
- In OBS, turn the WebSocket server on: **Tools → WebSocket Server Settings**. If you set a password there, enter the same one in the
  plugin's settings.

## Installation

1. Build: `dotnet build OBS\src\MacroGrid.Plugin.Obs.csproj` (add `-c Release` for a release build). This needs the `macro-grid`
   repository next to this one, see the [top-level README](../README.md#building).
2. In the Macro Grid editor open **Plugins → Manage Plugins… → Install from Folder…** and pick `OBS\src\bin\Debug\net10.0\` (or
   `Release\net10.0\`). The build copies `plugin.json` next to the DLL. The plugin is loaded immediately.

## Settings

In the Plugins window click the gear button next to the OBS entry (or click the OBS item in the status bar). The fields are **Enabled**,
**Server**, **Port** and **Password**. Saving applies immediately: the plugin reconnects within moments, no restart needed. The settings are
stored in `%AppData%\MacroGrid\plugins\obs\settings.json` (the password is stored in plain text). On first start the file is created
with the plugin **disabled**:

```json
{ "enabled": false, "host": "127.0.0.1", "port": 4455, "password": "" }
```

Turn **Enabled** on to connect.

## Status bar

The editor's status bar shows the connection state ("connected", "connecting", "wrong password", "OBS is not running", the time until the
next retry, ...); while connected it adds the frame rate and, when streaming or recording, the elapsed time. Clicking it opens the settings.
(The status text is translated to Turkish through `locales/tr.json`.)

## Variables

All names start with `obs.`; use them in widget text as `{obs.stream.duration}`.

- **Connection:** `obs.connected`, `obs.status`, `obs.ws.in`, `obs.ws.out`
- **Scenes and state:** `obs.scene.current`, `obs.scene.preview`, `obs.studioMode`, `obs.transition.current`, `obs.profile.current`,
  `obs.sceneCollection.current`
- **Stream:** `obs.streaming`, `obs.stream.reconnecting`, `obs.stream.duration`, `obs.stream.timecode`, `obs.stream.congestion`,
  `obs.stream.bytes`, `obs.stream.kbps`, `obs.stream.frames.dropped`, `obs.stream.frames.total`, `obs.stream.frames.droppedPercent`
- **Recording:** `obs.recording`, `obs.record.paused`, `obs.record.duration`, `obs.record.timecode`, `obs.record.bytes`, `obs.record.kbps`
- **Outputs:** `obs.virtualcam`, `obs.replayBuffer`
- **Statistics:** `obs.stats.fps`, `obs.stats.cpu`, `obs.stats.memory`, `obs.stats.disk`, `obs.stats.renderTime`,
  `obs.stats.render.skipped` / `.total` / `.skippedPercent`, `obs.stats.output.skipped` / `.total` / `.skippedPercent`
- **Per audio input and per scene item:** `obs.input.<slug>.muted`, `obs.input.<slug>.volumeDb`, and
  `obs.item.<scene>.<source>.visible`. A slug is the name in lower case with every run of characters other than `a-z` and `0-9` replaced by
  `_`. When an input or scene item is deleted or renamed its variable is removed.

## Actions

All are in the action picker under the "OBS" category, with forms whose scene, audio input and scene item lists are filled from a cache of
what OBS reported (they do not wait for a live round trip).

- **Scenes:** `obs.setScene`, `obs.setPreviewScene`, `obs.studioTransition`, `obs.toggleStudioMode`, `obs.setTransition`, `obs.setProfile`
- **Stream and recording:** `obs.startStream`, `obs.stopStream`, `obs.toggleStream`, `obs.startRecord`, `obs.stopRecord`, `obs.toggleRecord`,
  `obs.pauseRecord` (pause, resume or toggle)
- **Outputs:** `obs.virtualCam`, `obs.replayBuffer` (start, stop or toggle), `obs.saveReplay`
- **Audio:** `obs.setMute` (mute or unmute), `obs.toggleMute`, `obs.setVolume` (0 to 100%; on a slider or knob it uses the live dragged
  value), `obs.adjustVolume` (a step in dB)
- **Scene items and text:** `obs.setItemVisibility` (including groups; show, hide or toggle), `obs.setText` (sets a text source; the text
  may contain `{variables}`, which the server resolves before the action runs)

An action aimed at something that no longer exists (a deleted scene or audio input) fails with an explicit error instead of doing nothing;
the message is shown on the phone and in the editor's status bar.

## How the connection behaves

- The handshake and every request have timeouts. If the connection drops, the plugin retries with a growing, jittered wait from 2 up to
  30 seconds.
- A wrong password or a version mismatch (obs-websocket close codes 4009, 4010 and 4012) stops the retries until the settings change, and
  shows "wrong password" instead of retrying forever and filling the log.
- When OBS announces that it is closing, the connection is closed at once instead of waiting for a TCP timeout.
- Stream, recording and statistics are polled with a single batched request (every second while streaming or recording, every five seconds
  otherwise). Scenes, inputs, profiles and the rest are kept up to date from OBS events, never polled.
- If OBS is expected on this computer (the server is `127.0.0.1`, `localhost` or this machine's own address), then after three failed
  attempts the plugin stops trying to connect and only checks every five seconds whether an `obs64`, `obs32` or `obs` process exists, so it
  does not keep opening sockets while OBS is closed. It connects as soon as OBS starts. For a remote OBS the normal retry continues.

## Tests

`dotnet test OBS\tests\MacroGrid.Plugin.Obs.Tests\MacroGrid.Plugin.Obs.Tests.csproj` runs the connection layer against a fake
obs-websocket server (handshake, wrong password, timeouts, sudden disconnects, OBS shutting down, input removal).

## License

MIT, see [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md). The plugin includes no third-party code; the build output carries both files. This plugin was created by AI tools and is provided "as is", without warranty of any kind; the authors accept no responsibility or liability for it, and you use it at your own risk.
Full details: [../THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).

## Changelog

[CHANGELOG.md](CHANGELOG.md) (short) and [CHANGELOG-developer.md](CHANGELOG-developer.md) (detailed).
