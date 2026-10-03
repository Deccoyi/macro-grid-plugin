# Changelog (developer) — SoundBoard

This file follows the [Keep a Changelog](https://keepachangelog.com/) format. Only what a plugin author, integrator or maintainer
needs and git history cannot carry — everything else is in commit messages and pull request descriptions. The short, public
changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]
### Changed
- Actions implement `IActionOutcomeHandler` (a coded outcome). Needs an editor that has the interface, so `minMacroGrid` must be raised before this is released. A play action with no sound chosen now reports "not configured"; a missing file names the sound instead of the file path.

## [0.1.2] - 2026-09-29
### Changed
- `plugin.json` declares `"minMacroGrid": "1.0.0"` (the field's new name; built against SDK 1.0.0). The old `sdkVersion` / `minServerVersion` stay so servers before 1.0.0 still load it.
- Package format: the release zip now also carries `signature.json` (`id`, `version`, `kind` and the SHA-256 of every file) and `signature.sig` in its root, written by `scripts/release-plugin.ps1`. Macro Grid versions that enforce official-only C# plugins check them every time the plugin loads and refuse a plugin whose files do not match; older versions ignore the two files. No manifest, setting, action or variable changed.

## [0.1.1] - 2026-09-26
### Changed
- `soundboard.nowPlaying` now holds the name of the sound playing at that moment (the latest started voice that has not finished) and is `""` when idle. The old meaning, the last started sound's name, moved to the new `soundboard.lastPlayed`. Bindings that showed the last sound after it ended need `soundboard.lastPlayed`.

### Fixed
- A non-looping sound never became "finished": the mixer drops an input on its first short read and does not read it again, but `SoundVoice` waited for a read of 0. The voice, `soundboard.<id>.playing` and toggle mode stayed stuck on. A short read now marks the voice finished; `LoopingSampleProvider` fills the whole buffer so a looping voice is not dropped at the end of the file.

## [0.1.0] - 2026-09-25

### Added
- First version. Built against Plugin SDK `^0.4.0` (uses `SettingFieldKind.File`/`List`/`Button`/`Notice`, `IReleaseAwareAction` and
  `ISettingsCommandHandler`, all new in that SDK version — a server on an older SDK cannot load this plugin). `minServerVersion` is `0.3.2`, the first server that ships SDK 0.4.0.
- Settings (`settings.json`): `outputDevice`, `sounds` (each row: `id`, `file`, `name`, `volume`, `loop`), `masterVolume`,
  `overlapMode` (`overlap`/`cut`), `stopStyle` (`immediate`/`fade`), `fadeInMs`, `fadeOutMs`. A sound row's `id` is assigned once and
  kept for its lifetime; it names that sound's `soundboard.<id>.*` variables.
- Actions: `soundboard.play` (`sound`, `playMode`: `full`/`hold`/`toggle`, `stopStyle`: `default`/`immediate`/`fade`), `soundboard.stop`
  (`target`: `all`/`one`, `sound`, `stopStyle`), `soundboard.setMasterVolume` (`mode`: `set`/`adjust`/`slider`, `value`, `step`).
- Variables: `soundboard.nowPlaying`, `soundboard.masterVolume`, and per sound `soundboard.<id>.name`, `.playing`, `.remaining`, `.duration`.
- Ships [NAudio](https://github.com/naudio/NAudio) (MIT) in its build output for WASAPI playback and audio file decoding — see the
  repository's `THIRD_PARTY_NOTICES.md`.
