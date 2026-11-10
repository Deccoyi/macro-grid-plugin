using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Status: connection state, the status text and the status item.
public sealed partial class ObsConnection
{
    private void SetState(IVariableStore store, ObsConnectionState state, TimeSpan? retryIn = null)
    {
        _connState = state;
        store.Set("obs.connected", state == ObsConnectionState.Connected);
        var (text, level) = state switch
        {
            ObsConnectionState.Disabled => ("OBS · off", StatusLevel.Idle),
            ObsConnectionState.Connecting => ("OBS · connecting…", StatusLevel.Busy),
            ObsConnectionState.Connected => ("OBS · connected", StatusLevel.Ok),
            ObsConnectionState.Reconnecting => ($"OBS · retrying in {retryIn?.TotalSeconds:0}s", StatusLevel.Warning),
            ObsConnectionState.WaitingForObs => ("OBS · not running", StatusLevel.Idle),
            ObsConnectionState.AuthFailed => ("OBS · wrong password", StatusLevel.Error),
            ObsConnectionState.PasswordUnreadable => ("OBS · saved password could not be read on this computer, enter it again in the plugin settings", StatusLevel.Error),
            _ => ("OBS · error", StatusLevel.Error),
        };
        store.Set("obs.status", text);
        _statusItem?.Update(text, level, "video");
    }

    private void UpdateConnectedStatusText(bool wasStreaming, bool wasRecording, IVariableStore store, double fps, TimeSpan? streamDuration, TimeSpan? recordDuration)
    {
        if (_connState != ObsConnectionState.Connected) return;
        var streaming = (bool?)store.Get("obs.streaming") ?? false;
        var duration = streaming ? streamDuration : recordDuration;
        var text = streaming || wasStreaming || (bool?)store.Get("obs.recording") == true || wasRecording
            ? $"OBS · {fps:0} fps · ● {duration?.ToString(@"hh\:mm\:ss") ?? "00:00:00"}"
            : $"OBS · {fps:0} fps";
        if (host.Demand.IsObserved("obs.status")) store.Set("obs.status", text);
        _statusItem?.Update(text, StatusLevel.Ok, "video");
    }
}
