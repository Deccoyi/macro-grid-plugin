# Changelog — SoundBoard

New features and fixes in the SoundBoard plugin. For technical details, see [CHANGELOG-developer.md](CHANGELOG-developer.md).

## Unreleased

## 0.1.1 - 2026-09-26
### Changed
- **Now playing:** The now-playing variable shows the sound that is playing and is empty when nothing plays. A new last-played variable keeps the name of the last sound that was started.

### Fixed
- **Sounds that ended stayed "playing":** A sound that played to its end kept showing as playing, so a toggle button stayed on and needed a second press. It now switches off when the sound ends.

## 0.1.0 - 2026-09-25
### New
- First version: play local sound files from buttons. Pick a file, give it a name, a volume and a loop switch, and bind it to a
  widget — full play, hold-to-play or toggle. Overlap several sounds or have a new one cut the others off. Fade in and out. A master
  volume slider and its own button. See which sound is playing and how much is left on a widget's text.
