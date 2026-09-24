# License notice: OBS Control

This plugin is licensed under the MIT License, see [LICENSE](LICENSE). Copyright (c) 2026 Deccoyi. It is provided "as is", without warranty.

Third-party code: none is included or shipped. The plugin uses only what ships with .NET (including `System.Net.WebSockets`) and the
Macro Station plugin SDK (MIT, referenced at build time and not copied into the plugin). It speaks the obs-websocket v5 protocol to a
running OBS Studio; it contains no OBS code and no obs-websocket client library. The test project uses xunit and Microsoft test
packages only for testing; they are not part of the plugin.

The full list of components and license texts is in the repository: `THIRD_PARTY_NOTICES.md` and the `licenses/` folder.
