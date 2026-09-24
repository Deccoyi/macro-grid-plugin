# Getting started

This page takes you from nothing to a working button on your phone. The full user documentation is in the
[server repository](https://github.com/Deccoyi/macro-grid); this is the short path you need before writing plugins.

## Requirements

- Windows 10 or 11 for the server.
- The [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (part of current Windows).
- A phone or tablet on the **same local network** with the Android app, or any browser.

## 1. Install the server

There are no published releases yet while the project is in alpha. Until the first release you build the server from source
(you need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node.js](https://nodejs.org/) 20 or newer):

```powershell
cd editor;    npm install; npm run build; cd ..
cd webclient; npm install; npm run build; cd ..
Copy-Item editor\dist\*    src\MacroGrid.Host\wwwroot\editor -Recurse -Force
New-Item -ItemType Directory -Force src\MacroGrid.Host\wwwroot\deck | Out-Null
Copy-Item webclient\dist\* src\MacroGrid.Host\wwwroot\deck -Recurse -Force
dotnet run --project src/MacroGrid.Host
```

Once a release exists, the server ships as a Windows installer (`MacroGrid-Setup-<version>.exe`) that installs to
`Program Files\Macro Grid`, opens TCP port 9820 for private networks and leaves your data in `%AppData%\MacroGrid` on uninstall.
Check the [Releases page](https://github.com/Deccoyi/macro-grid/releases) of the server repository.

The server appears as a **tray icon**; its menu opens the editor.

::: warning Trusted networks only
Traffic is not encrypted and the server listens on port 9820 on all interfaces. Use it on a home or office network you trust,
never forward the port to the internet, and pair only devices you trust: a paired device can press keys, type text and start
programs on your PC.
:::

## 2. Pair your phone

1. In the editor open the **Pairing** window. It shows a six-digit **PIN** and a **QR code**, valid for five minutes.
2. On the phone open the app and scan the QR code, or enter the PC's address and the PIN.
3. The device is paired and shows the profile. From then on it uses a token instead of the PIN. You can revoke devices in the editor.

The QR code encodes `macrogrid://pair?host=<ip>&port=9820&pin=<pin>`. A browser can act as a deck too: open
`http://<PC address>:9820/deck/` and pair with the PIN.

## 3. Your first profile and button

1. In the editor create a **profile** (a profile has one or more pages; a page is a grid).
2. Drag a **button** widget onto the grid and give it a text, for example `CPU {system.cpu|0}%`.
3. Bind an action to its **press** event, for example the built-in `core.hotkey` action with `ctrl+shift+s`.
4. Save. Connected devices update immediately, and pressing the button on the phone runs the action on the PC.

Several actions bound to the same event run one after the other, which makes a macro.

## 4. Plugins

Open **Plugins, Manage Plugins** in the editor to install plugins. Plugins add new actions to the action picker and new
variables to the `{...}` picker in text fields. The plugins in this repository (OBS control, PLC icons, a JavaScript hello
world) are described in the [Guides](/guides/obs-plugin), and you can [write your own](/tutorials/js-hello-world).
