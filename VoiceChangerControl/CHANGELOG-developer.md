# Changelog (developer) — Voice Changer Control

This file follows the [Keep a Changelog](https://keepachangelog.com/) format. Only what a plugin author, integrator or maintainer
needs and git history cannot carry — everything else is in commit messages and pull request descriptions. The short, public
changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]
### Added
- First version. JavaScript plugin, id `voicechanger`. Action types `voicechanger.togglevoicechanger`, `voicechanger.togglemute`, `voicechanger.togglehearmyself`, `voicechanger.togglebackground`, `voicechanger.stopsounds`, `voicechanger.loadvoice` (field `voiceID`), `voicechanger.randomvoice` (field `mode`) and `voicechanger.playsound` (field `fileName`). Variables `voicechanger.enabled`, `voicechanger.micmuted`, `voicechanger.voice` and `voicechanger.voices`. Setting `apiKey` (`Password` kind).
- Permissions: `variables`, `actions` and `ws:localhost:<port>` for the eleven ports the Voicemod app can listen on.
- `minMacroGrid` is `1.6.0` (provisional): the first server release that stores JavaScript `Password` settings protected. Do not release before that server version is out.
