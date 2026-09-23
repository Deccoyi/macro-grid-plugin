# PLC icons plugin

A static icon pack for Macro Station: 29 ladder-logic (PLC) symbols (coil, timers, comparison and arithmetic blocks, branches) that appear
in the editor's icon picker as their own category. It has no connection, no settings and no actions. Kind: C# plugin. Id: `plc-icons`.
The category is named "PLC İkonları" in the picker.

## Installation

1. Build: `dotnet build PLCIcons\src\MacroStation.Plugin.PlcIcons.csproj` (add `-c Release` for a release build). This needs the
   `macro-station` repository next to this one, see the [top-level README](../README.md#building).
2. In the Macro Station editor open **Plugins → Manage Plugins… → Install from Folder…** and pick `PLCIcons\src\bin\Debug\net10.0\` (or
   `Release\net10.0\`). The build copies `plugin.json` next to the DLL. The pack is available immediately.

## Icons

`add`, `calculate`, `close-branch`, `coil`, `convert`, `divide`, `empty-block`, `equal`, `f-trig`, `greater`, `greater-equal`, `lesser`,
`lesser-equal`, `move`, `multiply`, `n`, `nc`, `no`, `not-equal`, `open-branch`, `p`, `r-trig`, `reset-coil`, `set-coil`, `subtract`,
`timer-convert`, `tof-timer`, `ton-timer`, `tp-timer`.

The SVG files are in `src/icons/` and are embedded in the DLL. Each one draws with `stroke="currentColor"`, so the editor can color it:
when an icon is picked the editor adds the widget's color to the root `<svg>` tag.

## Adding an icon

Put a new `.svg` in `src/icons/` and add its file name (without the extension) to the `Names` array in `src/PlcIconsPlugin.cs`. The
project embeds everything matching `icons\*.svg`. Keep the icon monochrome and use `currentColor` for every stroke and fill.

## Changelog

[CHANGELOG.md](CHANGELOG.md) (short) and [CHANGELOG-developer.md](CHANGELOG-developer.md) (detailed).
