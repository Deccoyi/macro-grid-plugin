# Macro Grid plugins

Plugins for [Macro Grid](https://github.com/Deccoyi/macro-grid), the Windows server that turns a phone or tablet on your
local network into a customizable macro deck. This repository holds the official plugins and the guide for writing your own.
It is a separate repository from the server and from the phone app ([macro-grid-client](https://github.com/Deccoyi/macro-grid-client)),
and every plugin here is versioned on its own.

> **This project was written entirely by an AI assistant (Claude) at a user's direction.** It has not been independently audited,
> security-reviewed or certified for production use. It is provided "as is", without warranty of any kind, and you use it at your own
> risk; see [LICENSE](LICENSE).

## What is here

| Plugin | Kind | What it does |
|---|---|---|
| [OBS/](OBS/) | C# | Controls OBS Studio over obs-websocket v5: scenes, streaming, recording, audio, scene items, text sources, plus about 45 live `obs.*` variables. |
| [PLCIcons/](PLCIcons/) | C# | A static icon pack of ladder-logic (PLC) symbols for the editor's icon picker. |
| [HelloJs/](HelloJs/) | JavaScript | A small example of a sandboxed script plugin: a counter variable, a settings page, one action. |

## Installing a plugin

1. Build it (C# plugins) or use the folder as it is (JavaScript plugins).
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
```

## Writing your own

[docs/plugin-authoring.md](docs/plugin-authoring.md) is the guide: the manifest, the C# SDK (actions, variables, settings forms, status
items, icon packs), the lifecycle, and the JavaScript sandbox with its permissions. [CONTRIBUTING.md](CONTRIBUTING.md) describes the rules
every plugin in this repository follows.

## Security

A C# plugin runs inside the server process with full .NET access: **only install C# plugins whose source you trust.** JavaScript plugins
run in a sandbox with no .NET access and only the permissions you approve, with time and memory limits per call.

## License

[MIT](LICENSE), copyright (c) 2026 Deccoyi. Each plugin folder carries its own `LICENSE` and `NOTICE.md`, and the C# plugins copy both
into their build output.

### Third-party licenses

- [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md): index of every component (name, version, license, copyright, URL).
- [licenses/](licenses/): the original license text of each component.

No plugin ships third-party code or assets; the listed components are used only to build and test. The PLC icons are original
AI-generated artwork under this repository's MIT license, not taken from an icon library (see [PLCIcons/NOTICE.md](PLCIcons/NOTICE.md)).
