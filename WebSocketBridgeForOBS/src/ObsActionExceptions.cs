namespace MacroGrid.Plugin.Obs;

// What an action throws, so the action base can report a coded outcome without matching on message text.
// They derive from InvalidOperationException and keep their messages (the locale files match them word for word).

/// <summary>OBS is not connected right now.</summary>
public sealed class ObsNotConnectedException() : InvalidOperationException("Not connected to OBS.");

/// <summary>A scene, source or item the action was set up with no longer exists in OBS.</summary>
public sealed class ObsTargetMissingException(string message) : InvalidOperationException(message);

/// <summary>A setting the action needs is empty.</summary>
public sealed class ObsNotConfiguredException(string message) : InvalidOperationException(message);
