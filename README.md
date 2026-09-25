# Macro Grid plugins

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Status: alpha](https://img.shields.io/badge/status-alpha-orange.svg)
[![CI](https://github.com/Deccoyi/macro-grid-plugin/actions/workflows/ci.yml/badge.svg)](https://github.com/Deccoyi/macro-grid-plugin/actions/workflows/ci.yml)

Plugins for [Macro Grid](https://github.com/Deccoyi/macro-grid), the Windows server that turns a phone or tablet on your
local network into a customizable macro deck. This repository holds the official plugins and the guide for writing your own.
It is a separate repository from the server and from the phone app ([macro-grid-client](https://github.com/Deccoyi/macro-grid-client)),
and every plugin here is versioned on its own. Documentation site: <https://deccoyi.github.io/macro-grid-plugin/>. **Plugin store (downloads): <https://deccoyi.github.io/macro-grid-plugin/store/>.**

> ## AI-generated software: you use it entirely at your own risk
>
> All code, design, documentation and artwork of this project were created by artificial intelligence (an AI assistant working at the
> maintainer's direction). Nothing has been reviewed line by line by a human, security-audited or certified for any purpose.
>
> **No warranty, no liability.** The software is provided "as is", without warranty of any kind, express or implied. To the fullest
> extent permitted by law, the authors and contributors accept no responsibility or liability of any kind for it, including for damage,
> data loss, misuse, security problems or any other consequence of installing or using it. All risk is yours: which software you
> install, which devices you pair, which plugins you run and which buttons you press. The installer and the app ask you to accept the
> [user agreement](https://github.com/Deccoyi/macro-grid/blob/main/installer/license-agreement.txt). See also [LICENSE](LICENSE) (MIT).

## What is here

| Plugin | Kind | What it does |
|---|---|---|
| [OBS/](OBS/) | C# | Controls OBS Studio over obs-websocket v5: scenes, streaming, recording, audio, scene items, text sources, plus about 45 live `obs.*` variables. |
| [PLCIcons/](PLCIcons/) | C# | A static icon pack of ladder-logic (PLC) symbols for the editor's icon picker. |
| [Sound/](Sound/) | C# | Plays local sound files from buttons: named clips with volume, loop, overlap and fade control, plus live `sound.*` variables. |
| [HelloJs/](HelloJs/) | JavaScript | A small example of a sandboxed script plugin: a counter variable, a settings page, one action. |

## Installing a plugin

Manually, a plugin is a folder placed in `%AppData%\MacroGrid\plugins\<id>\` (the folder that contains `plugin.json`). The easy way:

1. Download a plugin archive from the [Plugin store](https://deccoyi.github.io/macro-grid-plugin/store/) (or the Releases page) and unzip it, build it yourself (C# plugins), or use the folder as it is
   (JavaScript plugins).
2. In the Macro Grid editor open **Plugins → Manage Plugins…** and choose **Install from Folder…**. For a C# plugin pick its build
   output folder (for example `OBS\src\bin\Debug\net10.0\`); the build puts `plugin.json` next to the DLL.
3. The plugin is loaded right away, with no restart. A JavaScript plugin asks you to approve the permissions it needs first.

Each plugin's README lists its own requirements and settings. Plugins are installed to `%AppData%\MacroGrid\plugins\<id>\`.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). The C# plugins reference the plugin SDK
(`MacroGrid.Plugin.Abstractions`) by path, so clone this repository **next to** the server repository:

```
some-folder/
├── macro-grid/          https://github.com/Deccoyi/macro-grid
└── macro-grid-plugin/   this repository
```

```powershell
dotnet build OBS\src\MacroGrid.Plugin.Obs.csproj
dotnet test  OBS\tests\MacroGrid.Plugin.Obs.Tests\MacroGrid.Plugin.Obs.Tests.csproj
dotnet build PLCIcons\src\MacroGrid.Plugin.PlcIcons.csproj
dotnet build Sound\src\MacroGrid.Plugin.Sound.csproj -p:UseLocalSdk=true
dotnet test  Sound\tests\MacroGrid.Plugin.Sound.Tests\MacroGrid.Plugin.Sound.Tests.csproj -p:UseLocalSdk=true
```

## Writing your own

[docs/plugin-authoring.md](docs/plugin-authoring.md) is the guide: the manifest, the C# SDK (actions, variables, settings forms, status
items, icon packs), the lifecycle, and the JavaScript sandbox with its permissions. [CONTRIBUTING.md](CONTRIBUTING.md) describes the rules
every plugin in this repository follows. Releases are described in [docs/release.md](docs/release.md). Please read the
[Code of Conduct](CODE_OF_CONDUCT.md) and the [security policy](SECURITY.md) before opening issues.

## Security

A C# plugin runs inside the server process with full .NET access: **only install C# plugins whose source you trust.** JavaScript plugins
run in a sandbox with no .NET access and only the permissions you approve, with time and memory limits per call.

## License

[MIT](LICENSE), copyright (c) 2026 Deccoyi. Each plugin folder carries its own `LICENSE` and `NOTICE.md`, and the C# plugins copy both
into their build output.

### Third-party licenses

- [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md): index of every component (name, version, license, copyright, URL).
- [licenses/](licenses/): the original license text of each component.

Most plugins ship no third-party code or assets; the listed components are used only to build and test. The Sound plugin is the
exception — it ships [NAudio](https://github.com/naudio/NAudio) (MIT) for WASAPI playback and audio file decoding (see
[Sound/NOTICE.md](Sound/NOTICE.md)). The PLC icons are original AI-generated artwork under this repository's MIT license, not taken
from an icon library (see [PLCIcons/NOTICE.md](PLCIcons/NOTICE.md)).
