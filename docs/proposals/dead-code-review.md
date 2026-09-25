# Dead code review

Candidates that were found but not removed (public surface or unclear intent). None is referenced inside the repository.

- `ObsConnectionState.Error`: never assigned; only reached through the `_` fallback in `SetState`. Public enum, so kept.
- Static `Settings(...)` helpers on `ObsSetSceneAction`, `ObsSetMuteAction`, `ObsToggleMuteAction`, `ObsSetVolumeAction`, `ObsSetItemVisibilityAction` and `ObsModeAction` (pause record, virtual camera, replay buffer): no callers. They document the settings shape, so kept.
- `ObsClient.CloseStatus`: set on close, no reader inside the plugin. Kept as a diagnostic.
- Fields of `ObsSceneItem`/`ObsInputInfo` not read outside `ObsState` were not audited one by one.
