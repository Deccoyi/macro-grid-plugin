# Sound plugin

Plays local sound files from Macro Grid buttons: pick a file, give it a name and a volume, and bind it to a widget. Kind: C# plugin.
Id: `sound`.

## Requirements

- Windows, with at least one WASAPI render (playback) device. Nothing else — no external app or server to connect to.

## Installation

1. Build: `dotnet build Sound\src\MacroGrid.Plugin.Sound.csproj -p:UseLocalSdk=true` (add `-c Release` for a release build). This needs
   the `macro-grid` repository next to this one, see the [top-level README](../README.md#building). `-p:UseLocalSdk=true` is required
   until the Plugin SDK version this plugin targets (see `plugin.json`'s `sdkVersion`) is published on NuGet.
2. In the Macro Grid editor open **Plugins → Manage Plugins… → Install from Folder…** and pick `Sound\src\bin\Debug\net10.0-windows\`
   (or `Release\net10.0-windows\`). The build copies `plugin.json` next to the DLL. The plugin is loaded immediately.

## Settings

In the Plugins window click the gear button next to the Sound entry.

- **Output device:** the WASAPI playback device to use, or the system default. If the saved device disappears (unplugged, disabled)
  the plugin falls back to the default and shows a warning in the status bar.
- **Sounds:** one row per sound — **File** (a native picker; the file is never copied, its absolute path is stored, so moving it means
  picking it again), **Name** (shown in the action picker and available as `{sound.<id>.name}`), **Volume**, **Loop**, and a **Preview**
  button that plays (or, if already previewing, stops) the file at that row's current volume without saving first. A row a file cannot
  be found for shows a warning under it.
- **Master volume**, applied on top of every sound's own volume.
- **When the same sound plays again:** *Overlap* starts another copy alongside the ones already playing; *Cut* stops everything else
  first.
- **Default stop style** (*Immediate* or *Fade*) and **Fade in/out**, in milliseconds — the fallback a `sound.play`/`sound.stop`
  binding uses when its own stop style is left at "Default".

Settings are stored in `%AppData%\MacroGrid\plugins\sound\settings.json`. Saving applies live: volume and loop changes reach sounds
already playing, and a changed output device reopens on the next sound.

## Variables

All names start with `sound.`; use them in widget text as `{sound.s1.name}`.

- `sound.nowPlaying` — name of the most recently started sound.
- `sound.masterVolume` — master volume (%).
- Per sound (`<id>` is the row's own short id, e.g. `s1`, shown nowhere in the UI but stable across renames and reordering):
  `sound.<id>.name`, `sound.<id>.playing` (true/false — pair it with a dynamic color rule to light up a button while it plays),
  `sound.<id>.remaining` and `sound.<id>.duration` (with loop on, `remaining` is the rest of the current pass). A deleted sound's
  variables are removed.

## Actions

All are in the action picker under the "Sound" category.

- **`sound.play`:** *Sound* to play, *Play mode* (*Full* plays it through; *Hold* plays while the button is held and stops on release;
  *Toggle* starts it, or stops it if it is already playing) and *Stop style* (used when Hold releases or Toggle stops).
- **`sound.stop`:** stops one sound or every sound, with its own stop style.
- **`sound.setMasterVolume`:** *Set* to a value, *Adjust* by a step, or *Slider/knob* to follow a slider or knob's live dragged value.

A `sound.play`/`sound.stop` aimed at a sound that was since deleted, or whose file has gone missing, fails with an explicit error —
shown on the phone and in the editor's status bar — instead of doing nothing.

## Status bar

Two entries: how many sound files are currently missing, and the output device's state (ready, using the fallback device, or no
device available). Clicking either opens the settings.
