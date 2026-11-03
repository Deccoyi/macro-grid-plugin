# Changelog (developer) — Voice Changer Control

This file follows the [Keep a Changelog](https://keepachangelog.com/) format. Only what a plugin author, integrator or maintainer
needs and git history cannot carry — everything else is in commit messages and pull request descriptions. The short, public
changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]
### Added
- First version. JavaScript plugin, id `voicechanger`. Action types `voicechanger.togglevoicechanger`, `voicechanger.togglemute`, `voicechanger.togglehearmyself`, `voicechanger.togglebackground`, `voicechanger.stopsounds`, `voicechanger.loadvoice` (field `voiceID`), `voicechanger.randomvoice` (field `mode`) and `voicechanger.playsound` (field `fileName`). Action `voicechanger.refreshvoices` ("Refresh lists"). Variables `voicechanger.enabled`, `voicechanger.micmuted` and `voicechanger.voice`. Setting `apiKey` (`Password` kind).
- `voicechanger.loadvoice` (`voiceID`) and `voicechanger.playsound` (`fileName`) are searchable `Select` fields with the options sources `voices` and `sounds` (`host.options.source`); the stored value is the voice id or the sound id. Needs the server options-source API (1.6.0).
- Pictures: option icons are `plugin-image:voicechanger/voice:<voiceID>` and `plugin-image:voicechanger/sound:<bitmapChecksum>` (host.images.onRequest/put). Needs the server picture API (1.6.0).
- `plugin.json` asks for `limits.wsMessageKB` 256 (provisional: set from the measured reply sizes of a real Voicemod, at most 1024). The person approves it.
- Permissions: `variables`, `actions` and `ws:localhost:<port>` for the eleven ports the Voicemod app can listen on.
- `minMacroGrid` is `1.6.0` (provisional): the first server release that stores JavaScript `Password` settings protected. Do not release before that server version is out.
