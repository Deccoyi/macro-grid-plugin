# Voice Changer Control

Controls the Voicemod app on this PC: switch the voice changer on and off, mute the microphone, load a voice, pick a random voice and play sounds, from a button.

> **Unofficial.** This plugin works with the Voicemod app on this PC. It is an independent plugin and is not made, endorsed or affiliated with Voicemod. Voicemod is a trademark of its owner. It needs your own Voicemod Control API key.

## Requirements

- Windows, with the Voicemod app installed and running.
- Your own Voicemod Control API key. Voicemod gives it to each person who asks; Macro Grid cannot include one. How to get a key: <https://deccoyi.github.io/macro-grid/tutorials/voicemod>
- Macro Grid 1.6.0 or newer (the key is a protected setting from that version on).

## Setup

1. Install the plugin and allow its permissions (it talks to the Voicemod app on this computer only).
2. Open the plugin's settings, paste your key into **API key** and save.
3. Start Voicemod. The plugin status shows when it is connected.

## Actions

Toggle voice changer, Toggle microphone mute, Toggle hear myself, Toggle background effect, Load voice (by id), Random voice, Play sound (by file name), Stop all sounds.

## Variables

`voicechanger.enabled`, `voicechanger.micmuted`, `voicechanger.voice` (the current voice id) and `voicechanger.voices` (all voices as `id=name` pairs, to find the id for **Load voice**).

## Privacy

The key is stored protected for the current Windows user, is sent only to the Voicemod app on this PC and is never written to a log. The plugin makes no connection to the internet.

License: MIT, see [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md). This plugin was created by AI tools and is provided "as is", without warranty of any kind; the authors accept no responsibility or liability for it, and you use it at your own risk.
