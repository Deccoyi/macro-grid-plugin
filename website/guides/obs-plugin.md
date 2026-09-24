# A real-world plugin: OBS

The [OBS plugin](https://github.com/Deccoyi/macro-grid-plugin/tree/main/OBS) (id `obs`) controls
[OBS Studio](https://obsproject.com/) over obs-websocket v5, which is built into OBS 28 and newer. It is the largest plugin in the
repository and uses almost everything the SDK offers, so it is the best example to read after the tutorials.

## What it provides

- **22 actions** in the *OBS* category: scenes, studio mode, transitions, streaming, recording, replay buffer, virtual camera,
  audio mute and volume, scene item visibility and text sources.
- **About 45 live variables** with the `obs.` prefix, for example `obs.streaming`, `obs.stream.duration`, `obs.scene.current`,
  `obs.stats.fps`, plus `obs.input.<slug>.muted` and `obs.item.<scene>.<source>.visible` for things that come and go.
- **A settings page** (Enabled, Server, Port, Password) and **a status bar item** with the connection state.

It requires OBS 28 or newer with the WebSocket server turned on (**Tools, WebSocket Server Settings** in OBS). The README of the
plugin lists every variable and action.

## The entry point

<<< @/../OBS/src/ObsPlugin.cs

One object, `ObsConnection`, owns the connection. It is registered as the variable provider, handed to the settings page and to
every action.

| File | Role |
|---|---|
| `ObsPlugin.cs` | `IPlugin`: creates the connection and registers everything (above). |
| `ObsConnection.cs` | `IVariableProvider` and `IVariableCatalogSource`. Runs for the plugin's lifetime: connects, keeps the variables up to date, updates the status item and reconnects with capped exponential backoff. |
| `ObsClient.cs`, `ObsAuth.cs`, `ObsJson.cs` | A small obs-websocket v5 client: handshake, authentication, requests and events. |
| `ObsState.cs` | A cache of what OBS reported (scenes, inputs, scene items). |
| `ObsActions.cs` | The actions. Each implements `IActionHandler`, `IActionDescriptor` and, for dropdowns, `IOptionsSource`. |
| `ObsSettings.cs` | `ObsSettings` (persisted as `settings.json` in `DataDirectory`) and `ObsSettingsPage : IPluginSettingsPage`. |

## Patterns worth copying

- **A long-running provider.** `ObsConnection.RunAsync` runs until its token is cancelled. It creates its status item with
  `host.CreateStatusItem("connection")`, publishes variables through the `IVariableStore` it is given, and never lets a failed
  connection end the method: it retries with a capped exponential backoff, and a settings change wakes it up early.
- **Dynamic variables are removed.** When an OBS input or scene item is deleted or renamed, the plugin removes its variable with
  `store.Remove(name)`, so it stops showing up in the picker.
- **Dropdowns from a cache.** Actions implement `IOptionsSource`; the scene, input and item lists come from the cache of what OBS
  reported, so they do not wait for a live round trip.
- **Clear failures.** An action that targets something that no longer exists (a scene that was deleted in OBS) fails with a clear
  message. The server shows it on the phone and in the editor's status bar, so a stale button is never silent.
- **Settings apply immediately.** The settings page's `Save` writes `settings.json` and signals the connection, so a corrected host,
  port or password reconnects within moments.
- **Tests without OBS.** `OBS/tests` drives the plugin against a fake obs-websocket server (`FakeObsServer.cs`). Run `dotnet test` on
  `OBS/tests/MacroGrid.Plugin.Obs.Tests`.

The plugin's user-visible strings (action names, form labels, status text) are currently in Turkish; the code, comments and docs
are in English, which is the rule for every plugin in the repository.

## Using it in a layout

1. Install the plugin ([Plugin basics](/basics/#install-a-plugin)), open its settings from the gear button and turn **Enabled** on
   (the plugin starts disabled).
2. Add a button with the text `{obs.scene.current}` and bind the set-scene action to it.
3. Add a label `Live {obs.stream.duration}` and bind the stream actions to a toggle.
