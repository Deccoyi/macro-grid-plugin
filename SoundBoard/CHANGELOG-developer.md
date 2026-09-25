# Changelog (developer) — SoundBoard

This file follows the [Keep a Changelog](https://keepachangelog.com/) format. Only what a plugin author, integrator or maintainer
needs and git history cannot carry — everything else is in commit messages and pull request descriptions. The short, public
changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]

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
