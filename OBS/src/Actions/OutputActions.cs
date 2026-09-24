namespace MacroGrid.Plugin.Obs;

// Stream, recording, virtual camera and replay buffer.

public sealed class ObsStartStreamAction(ObsConnection obs) : ObsRequestAction(obs, "StartStream")
{
    public const string TypeId = "obs.startStream";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Start streaming";
    public override string? Icon => "radio";
}

public sealed class ObsStopStreamAction(ObsConnection obs) : ObsRequestAction(obs, "StopStream")
{
    public const string TypeId = "obs.stopStream";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Stop streaming";
    public override string? Icon => "square";
}

public sealed class ObsToggleStreamAction(ObsConnection obs) : ObsRequestAction(obs, "ToggleStream")
{
    public const string TypeId = "obs.toggleStream";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Toggle streaming";
    public override string? Icon => "radio";
}

public sealed class ObsStartRecordAction(ObsConnection obs) : ObsRequestAction(obs, "StartRecord")
{
    public const string TypeId = "obs.startRecord";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Start recording";
    public override string? Icon => "circle";
}

public sealed class ObsStopRecordAction(ObsConnection obs) : ObsRequestAction(obs, "StopRecord")
{
    public const string TypeId = "obs.stopRecord";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Stop recording";
    public override string? Icon => "square";
}

public sealed class ObsToggleRecordAction(ObsConnection obs) : ObsRequestAction(obs, "ToggleRecord")
{
    public const string TypeId = "obs.toggleRecord";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Toggle recording";
    public override string? Icon => "circle";
}

/// <summary>Pauses/resumes/toggles OBS recording. Settings: { "mode": "pause"|"resume"|"toggle" }</summary>
public sealed class ObsPauseRecordAction(ObsConnection obs)
    : ObsModeAction(obs, ("pause", "Pause", "PauseRecord"), ("resume", "Resume", "ResumeRecord"), "ToggleRecordPause")
{
    public const string TypeId = "obs.pauseRecord";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Pause recording";
    public override string? Icon => "pause";
}

/// <summary>Starts/stops/toggles the OBS virtual camera. Settings: { "mode": "start"|"stop"|"toggle" }</summary>
public sealed class ObsVirtualCamAction(ObsConnection obs)
    : ObsModeAction(obs, ("start", "Start", "StartVirtualCam"), ("stop", "Stop", "StopVirtualCam"), "ToggleVirtualCam")
{
    public const string TypeId = "obs.virtualCam";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Virtual camera";
    public override string? Icon => "webcam";
}

/// <summary>Starts/stops/toggles the OBS replay buffer. Settings: { "mode": "start"|"stop"|"toggle" }</summary>
public sealed class ObsReplayBufferAction(ObsConnection obs)
    : ObsModeAction(obs, ("start", "Start", "StartReplayBuffer"), ("stop", "Stop", "StopReplayBuffer"), "ToggleReplayBuffer")
{
    public const string TypeId = "obs.replayBuffer";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Replay buffer";
    public override string? Icon => "rewind";
}

public sealed class ObsSaveReplayAction(ObsConnection obs) : ObsRequestAction(obs, "SaveReplayBuffer")
{
    public const string TypeId = "obs.saveReplay";
    public override string Type => TypeId;
    public override string DisplayName => "OBS: Save replay";
    public override string? Description => "Saves the last few seconds of the replay buffer to disk";
    public override string? Icon => "save";
}
