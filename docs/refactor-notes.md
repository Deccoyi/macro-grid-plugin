# Refactor notes (branch refactor/cleanup)

Behavior is unchanged. Manifest ids, entry DLLs, namespaces, action type ids, variable names, settings keys,
`#region` markers and the `hello-js` copies are untouched.

- Added `.editorconfig` and `docs/engineering-guidelines.md`.
- Removed `ObsState.AllInputNames` and the never-read scene collection name list in `ObsState` (the request is still sent, the current name is still read).
- Removed unused usings (IDE0005) in OBS, its tests and PLCIcons.
- Split `OBS/src/ObsActions.cs` into `OBS/src/Actions/` (`SceneActions`, `OutputActions`, `AudioActions`, `SourceActions`, `ObsActionHelpers`, `ObsActionBase`). Registration order in `ObsPlugin.cs` is unchanged.
- Split `ObsConnection` into partial files: connect loop, polling, events, variables, status.
- New shared base classes: `ObsActionBase`, `ObsOptionsAction`, `ObsRequestAction`, `ObsModeAction`, `ObsSceneAction`, `ObsNamedChoiceAction`.
- Plugin csproj boilerplate moved to `build/Plugin.props`, imported explicitly by OBS and PLCIcons only. `examples/hello-csharp` stays standalone.
- New test `ObsActionCatalogTests` pins the registered actions (order, ids, labels, icons, fields) against a golden file.

## Public surface

- New public abstract types in the OBS plugin DLL (listed above). Nothing was removed or renamed; `TypeId` constants and the static `Settings(...)` helpers remain available on the same classes.
- The OBS plugin DLL is not a shared API; the host only uses `ObsPlugin` and the interfaces the actions implement.
