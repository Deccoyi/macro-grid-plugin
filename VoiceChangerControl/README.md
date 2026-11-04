# Voice Changer Control

Controls the Voicemod app on this PC: switch the voice changer on and off, mute the microphone, load a voice, pick a random voice and play sounds, from a button.

> **Unofficial plugin. Not affiliated with, endorsed by or sponsored by Voicemod. Voicemod is a trademark of its owner.** Türkçe: Resmi olmayan eklenti. Voicemod ile bağlantılı değildir, Voicemod tarafından onaylanmamış veya desteklenmemiştir. Voicemod, sahibinin ticari markasıdır.
>
> This plugin works with the Voicemod app on this PC. It needs your own Voicemod Control API key.

## Requirements

- Windows, with the Voicemod app installed and running.
- Your own Voicemod Control API key. Voicemod gives it to each person who asks; Macro Grid cannot include one. How to get a key: <https://deccoyi.github.io/macro-grid/tutorials/voicemod>
- Macro Grid 1.6.0 or newer (the key is a protected setting and the voice and sound pickers come with that version).

## Setup

1. Install the plugin and allow its permissions (it talks to the Voicemod app on this computer only).
2. Open the plugin's settings, paste your key into **API key** and save.
3. Start Voicemod. The plugin status shows when it is connected.

## Actions

Toggle voice changer, Toggle microphone mute, Toggle hear myself, Toggle background effect, Load voice, Random voice, Play sound, Stop all sounds, Refresh lists.

**Play sound** first asks for the **Source**: *Voicemod* (the boards the app ships) or *Soundboards* (the boards you made yourself, with all their sounds); then a **Panel** dropdown lists the panels (boards) of that source with the number of their sounds (empty means all panels); then the search box lists only that panel of that source. A button saved by an older version has no source or panel (or the removed source *Community*) and searches all of them. The search folds case, accents and Turkish letters (ı/i, ç/c, ğ/g, ö/o, ş/s, ü/u), and every typed word must be in the sound's name or its panel name. **Load voice** and **Play sound** show a search box over the voices and sounds of the Voicemod app. Favorites come first, then (voices) the last ones you used, then everything A-Z; sounds are grouped by soundboard (your own and disabled soundboards too; a disabled sound is marked "(disabled)"). A sound is played by its id through the app's `playMeme` action (the Voicemod docs name no other play action). The voice and sound lists are read once after the plugin has connected and registered (**Refresh lists**, a reconnect, or the refresh button of a newer editor reads them again; a search that finds an empty list also reads them, at most every 5 minutes) and are kept in memory only. If a voice or sound was removed from the app, the action says so.

**Pictures:** each voice and sound in the search box shows its Voicemod picture, and the editor can use it as the button icon. The host asks the plugin for a picture only when it is used (a button or a saved button needs it); browsing the list asks for nothing; the plugin reads it from the Voicemod app one at a time (at most 4 a second, a queue of 50) and hands it to the host, which keeps it on disk. Pictures that are not PNG, JPEG, GIF or WebP, or are over 256 KB or 1024 x 1024 pixels, are skipped.

## Variables

All variables are written only when their value changes. A true/false variable shows Açık/Kapalı by default; a text can choose its own words, for example `{voicechanger.enabled|ON/OFF}` (the host's own format, nothing to map).

| Variable | Type | Meaning |
|---|---|---|
| `voicechanger.enabled` | true/false | the voice changer is on |
| `voicechanger.micmuted` | true/false | the microphone is muted in the app |
| `voicechanger.hearmyself` | true/false | you hear your own voice |
| `voicechanger.background` | true/false | the background effect is on |
| `voicechanger.memesmuted` | true/false | sounds are muted for you (mute for me) |
| `voicechanger.voice` | text | id of the current voice |
| `voicechanger.voicename` | text | name of the current voice (empty until the voice list is read) |
| `voicechanger.license` | text | `free` or `pro`, when the app reports it |
| `voicechanger.state` | text | the connection state, as in the plugin status |
| `voicechanger.voicecount` | number | voices in the app |
| `voicechanger.soundcount` | number | sounds in the app |
| `voicechanger.lastsound` | text | name of the sound played last by this plugin |
| `voicechanger.lasterror` | text | the last problem or failed action; empty when connected and fine |

## Privacy

The key is stored protected for the current Windows user, is sent only to the Voicemod app on this PC and is never written to a log. The plugin makes no connection to the internet.

## Host limits (for developers)

A JavaScript plugin runs under fixed limits of the Macro Grid server (`JsPluginLimits` and the host API; server 1.6.0). What each does here:

| Limit | Value | When it is hit | This plugin |
|---|---|---|---|
| Script call time | 2 s per call | the call fails | calls are tiny |
| Memory, statements, recursion | 32 MB, 2 million, 100 | the call fails | far below |
| Failures in a row | 5 | the plugin is switched off | socket callbacks never throw |
| Timers | 20, shortest 100 ms | `every`/`after` throws | 2 timers, 1 s tick |
| Open web sockets | 2 | `connect` throws, P103 line | 1, closed before the next is opened |
| Web socket connects | 10 a minute | `connect` throws, P103 line | at most 8, at least 7 s apart |
| Incoming message | 64 KB, or `limits.wsMessageKB` in `plugin.json` for a `localhost` socket after the person approved it | that socket is closed, P103 line | this plugin asks for **512 KB**, measured on a real Voicemod: the voice list is 183 KB (257 voices), the sound list 329 KB (860 sounds in 37 soundboards), a voice picture reply 103 KB, a meme picture reply 163 KB; the lists are read one at a time, `getMemes` is never read (it is the same sounds as the soundboards, flattened); a closed socket turns that list off with a message |
| Incoming messages | 50 a second | the read waits, nothing is lost | events are cheap; unchanged variable values are not written again |
| Button pictures | the host asks for 20 ids per call, 500 ms apart; a picture is at most 256 KB decoded and 1024 x 1024 pixels, raster only | refused with a P151 line | one `getBitmap` at a time, at most 4 a second, a queue of 50 (oldest dropped), invalid pictures skipped, switched off after two connection drops during a picture request |
| Outgoing messages | 20 a second, 32 waiting, 64 KB each | `send` throws, P103 line | at most 10 a second, a button pressed faster is refused with a message |
| Variable name | own id as prefix, at most 120 characters | the call throws | `voicechanger.*` |
| Variable value | no host limit | none | cut at 8000 characters |
| Status text | 80 characters | cut | every text fits; an unchanged status is not sent again |
| Error List | 10 calls a second, 20 lines per plugin, 300 characters | dropped | 3 keyed lines |
| Settings file | 64 KB | save is refused | one key |
| Permissions | `ws:localhost:<port>` for each of the 11 ports, plus `variables` and `actions` | the call throws | all declared; plain `ws://` is allowed to localhost |
| Macro Grid's own ports | never reachable | the connect is refused | none of the 11 ports is one |

Not used: HTTP, storage, notices, input, widgets.

License: MIT, see [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md). This plugin was created by AI tools and is provided "as is", without warranty of any kind; the authors accept no responsibility or liability for it, and you use it at your own risk.
