# What is Macro Grid

Macro Grid turns a phone or tablet on your local network into a customizable macro deck for your Windows PC, like a hardware
macro keypad you design yourself. You lay out buttons, toggles, sliders and knobs on a grid in an editor. The deck shows live
values from the PC (CPU, RAM, the time, an OBS stream duration, ...) and presses keys, types text, opens programs, changes the
volume and controls other software through plugins.

::: warning Alpha, and written by an AI assistant
Macro Grid is in public alpha. **All code, design and documentation, including this site, were written by an AI assistant
(Claude) at a user's direction.** They have not been reviewed line by line by a human, security-audited or certified for
production use. Everything is provided "as is", without warranty of any kind, and you use it at your own risk. APIs and
plugin manifests may still change; the plugin SDK is `0.x`, which means a minor version can break plugins.
:::

## Architecture

There are three parts, in three repositories, versioned independently.

| Part | Repository | What it is |
|---|---|---|
| Server and editor | [macro-grid](https://github.com/Deccoyi/macro-grid) | A Windows tray application. It stores profiles, talks to decks over a WebSocket (port 9820), runs actions on the PC and hosts the editor in its own window. It also contains the plugin SDK. |
| Client | [macro-grid-client](https://github.com/Deccoyi/macro-grid-client) | The Android app that draws the deck and reports touches. Any browser can also act as a deck at `http://<PC address>:9820/deck/`. |
| Plugins | [macro-grid-plugin](https://github.com/Deccoyi/macro-grid-plugin) | The official plugins and this documentation. |

```
┌───────────── Windows PC ───────────────────────────────┐
│  Macro Grid server (tray app)                          │
│   ├─ port 9820: /ws  WebSocket for phones and decks    │
│   │             /api editor API (this computer only)   │
│   ├─ the editor (a WebView2 window)                    │
│   ├─ core: profiles, actions, variables, plugins       │
│   └─ Windows: key input, audio, system metrics         │
└───────────────▲────────────────────────────────────────┘
                │ WebSocket, JSON, local network only
     phone / tablet app  ·  any browser at /deck/
```

**Plugins run inside the server.** They add four kinds of things:

- **Actions**: things a widget can do when it is pressed (switch an OBS scene, bump a counter).
- **Variables**: live values a widget can show in its text (`{obs.stream.duration}`) or use in rules that change its color or icon.
- **Settings pages, status bar items**: a form in the Plugins window and a colored entry in the editor's status bar.
- **Icon packs**: sets of icons for the editor's icon picker.

A plugin cannot draw its own widget or add a widget type yet.

## The two kinds of plugin

| | C# plugin | JavaScript plugin |
|---|---|---|
| Trust | Full trust, inside the server process | Sandboxed, only a small `host` object and approved permissions |
| Good for | Real integrations (a websocket client, a device driver) | Small scripts (poll a local HTTP API, publish a variable, add an action) |
| Needs a build | Yes | No |

Only install C# plugins whose source you trust: they can do anything the server can do.

## Where to go next

- New to Macro Grid? [Getting started](/getting-started/) installs the server and pairs a phone.
- Want to write a plugin? Start with [Plugin basics](/basics/), then [Tutorial 1](/tutorials/js-hello-world).
