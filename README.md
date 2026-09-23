# Macro Station plugins

Plugins for [Macro Station](https://github.com/Deccoyi/macro-station), the Windows server that turns a phone or tablet on your
local network into a customizable macro deck. This repository holds the official plugins and the guide for writing your own.
It is a separate repository from the server and from the phone app ([macro-station-client](https://github.com/Deccoyi/macro-station-client)),
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
2. In the Macro Station editor open **Plugins → Manage Plugins…** and choose **Install from Folder…**. For a C# plugin pick its build
   output folder (for example `OBS\src\bin\Debug\net10.0\`); the build puts `plugin.json` next to the DLL.
3. The plugin is loaded right away, with no restart. A JavaScript plugin asks you to approve the permissions it needs first.

Each plugin's README lists its own requirements and settings. Plugins are installed to `%AppData%\MacroStation\plugins\<id>\`.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). The C# plugins reference the plugin SDK
(`MacroStation.Plugin.Abstractions`) by path, so clone this repository **next to** the server repository:

```
some-folder/
├── macro-station/          https://github.com/Deccoyi/macro-station
└── macro-station-plugin/   this repository
```

```powershell
dotnet build OBS\src\MacroStation.Plugin.Obs.csproj
dotnet test  OBS\tests\MacroStation.Plugin.Obs.Tests\MacroStation.Plugin.Obs.Tests.csproj
dotnet build PLCIcons\src\MacroStation.Plugin.PlcIcons.csproj
```

## Writing your own

[docs/plugin-authoring.md](docs/plugin-authoring.md) is the guide: the manifest, the C# SDK (actions, variables, settings forms, status
items, icon packs), the lifecycle, and the JavaScript sandbox with its permissions. [CONTRIBUTING.md](CONTRIBUTING.md) describes the rules
every plugin in this repository follows.

## Security

A C# plugin runs inside the server process with full .NET access: **only install C# plugins whose source you trust.** JavaScript plugins
run in a sandbox with no .NET access and only the permissions you approve, with time and memory limits per call.

## License

[MIT](LICENSE). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for the components the plugins use.
