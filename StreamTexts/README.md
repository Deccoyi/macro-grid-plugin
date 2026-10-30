# Stream Texts plugin

Writes live text files from Macro Grid variables, so the text source of your streaming app can show them: a CPU/RAM readout, a
counter you keep in a user variable, the name of the song that is playing. Kind: C# plugin. Id: `streamtexts`.

It adds no widget and no action. A streaming app has a text source that "reads from a file"; point it at one of the files this
plugin writes and the text follows the variable.

## Requirements

- Windows. Nothing else. The files need no port; the optional browser source uses one local port.

## Installation

1. Build: `dotnet build StreamTexts\src\MacroGrid.Plugin.StreamTexts.csproj`. This needs the `macro-grid` repository next to this one
   (the plugin SDK is built from it), see the [top-level README](../README.md#building).
2. In the Macro Grid editor open **Plugins → Manage Plugins… → Install from Folder…** and pick `StreamTexts\src\bin\Debug\net10.0\`.

## Settings

In the Plugins window click the gear button next to the Stream Texts entry.

- **Folder:** where the files are written. Empty means `Documents\Macro Grid\Stream Texts`. It must be a full path. **Open folder**
  opens it in Explorer.
- **Check every (ms):** how often the values are looked at (250 to 10000, default 500). A file is only written when its text
  changed, so a low value costs next to nothing; a streaming app re-reads a file about once a second anyway.
- **Text files:** one row per file.
  - **File name:** letters, digits, space, `-` and `_`, up to 64 characters. `.txt` is added for you. **Show file path** shows
    the full path to paste into the streaming app.
  - **Text:** what is written into the file. Use the same tokens as a widget's text: `{system.cpu}`, `{system.cpu|0}` (a format),
    `{user.score||0}` (what to write while the variable has no value). `{{` and `}}` give a literal brace, and `\n` is a new line.
    A file can mix any number of variables: `CPU {system.cpu|0}% | RAM {system.ram.used|0.0}/{system.ram.total|0} GB`.

- **Write text files:** on by default. Turn it off to use only the browser source; the files this plugin made are then deleted.
- **Browser source server:** off by default. When on, each text is also served at `http://127.0.0.1:<port>/t/<name>` (default port
  9830), a transparent page for a browser source that follows the text live. Add `?css=` with your own styles, for example
  `?css=%23t{font-size:48px;color:gold}` (the text sits in an element with the id `t`). `/t/<name>.txt` gives the plain text, and
  `http://127.0.0.1:<port>/` lists the names. Safety: it listens on this PC only (`127.0.0.1`), answers GET only, serves only the
  names you set up, refuses any other host name, and allows at most 16 open pages. A browser source costs the streaming app more
  memory than a text file, which is why the files stay the default.

Numbers and dates are always formatted the same way on every PC (a dot as the decimal mark).

## How the files are written

- UTF-8 without a byte order mark, so no stray character shows up in front of the text.
- Each write goes to a `.tmp` file first and is then moved over the real one, so the streaming app never reads half a file. If the
  app has the file open at that very moment, the write is simply tried again on the next check.
- A file you delete is written again within about 10 seconds.
- When you remove or rename a row, or change the folder, the plugin deletes the files it made before and only those.
- When the plugin is turned off, the files stay with their last text.

## Limits

Up to 64 files and 1000 characters per text. These keep a typo from filling the disk or slowing the check loop.
