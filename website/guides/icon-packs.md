# Icon packs: PLC Icons

::: warning Icon packs come only with official plugins
An icon pack is code that runs inside the server, so it can only be a C# plugin, and the server loads C# plugins only when they are
official and signed. JavaScript plugins cannot add icon packs. This page shows how the official PLC Icons plugin is built, for reading.
:::

An icon pack is the simplest useful C# plugin: no connection, no settings, no actions. The **PLC Icons** plugin
([`PLCIcons/`](https://github.com/Deccoyi/macro-grid-plugin/tree/main/PLCIcons), id `plc-icons`) contributes 29 ladder-logic (PLC)
symbols to the editor's icon picker as their own category. This page walks through it.

## What a pack is

Implement `IIconPackSource` and register it in `Initialize` with `host.RegisterIconPack`:

| Member | Meaning |
|---|---|
| `Id` | Unique id of the pack. |
| `DisplayName` | The category name in the icon picker. |
| `IconNames` | The names of the icons, in the order you want them listed. |
| `GetIconSvg(name)` | The SVG text of one icon, or `null` if there is none. |

## The whole plugin

<<< @/../PLCIcons/src/PlcIconsPlugin.cs

- `PlcIconsPlugin` is the `IPlugin` entry point: one line, `host.RegisterIconPack(new PlcIconPack())`.
- The names are sorted so the picker's ordering does not depend on the file system.
- The SVGs are read from resources embedded in the assembly, so there are no loose files at run time.

The display name is written in English (the plugin's default language); the Turkish name "PLC İkonları" comes from `locales/tr.json`.

## The project

The icons live in `src/icons/` and are embedded with one line in the `.csproj`:

```xml
<EmbeddedResource Include="icons\*.svg" />
```

The resource name is `<RootNamespace>.icons.<file name>` (`MacroGrid.Plugin.PlcIcons.icons.coil.svg`), which is what
`GetManifestResourceStream` asks for above. `plugin.json` is copied to the output like in any C# plugin:

<<< @/../PLCIcons/plugin.json

## An icon

Each icon is a monochrome SVG that draws with `stroke="currentColor"`. The editor colors the icon by setting `color` on the root
`<svg>` element, so never hard-code colors. This is `coil.svg`:

<<< @/../PLCIcons/src/icons/coil.svg{xml}

## Adding an icon

1. Put a new `.svg` in `src/icons/`. Keep it monochrome and use `currentColor` for every stroke and fill.
2. Add its file name (without the extension) to the `Names` array.
3. Build and reinstall (or reload) the plugin.
