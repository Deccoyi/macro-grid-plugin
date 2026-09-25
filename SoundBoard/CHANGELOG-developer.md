# Changelog (developer) — SoundBoard

This file follows the [Keep a Changelog](https://keepachangelog.com/) format. Only what a plugin author, integrator or maintainer
needs and git history cannot carry — everything else is in commit messages and pull request descriptions. The short, public
changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]

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
