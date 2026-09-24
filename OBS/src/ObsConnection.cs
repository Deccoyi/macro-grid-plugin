using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

public enum ObsConnectionState { Disabled, Connecting, Connected, Reconnecting, WaitingForObs, AuthFailed, Error }

/// <summary>
/// Owns the (re)connecting OBS WebSocket connection for the whole plugin lifetime: an
/// <see cref="IVariableProvider"/> that never returns while the process is alive, publishing "obs.*"
/// variables and reconnecting with capped exponential backoff whenever OBS isn't running or the
/// connection drops. Action handlers (the OBS actions) share this same instance's
/// <see cref="RequestAsync"/>/<see cref="Cache"/> to actually do things — there's exactly one connection
/// to OBS. Settings are re-read once on (re)connect and whenever <see cref="NotifySettingsChanged"/> is
/// called by the settings page, not on every loop tick.
/// </summary>
public sealed partial class ObsConnection(IPluginHost host) : IVariableProvider, IVariableCatalogSource
{
    private const string Category = "OBS";
    private static readonly Regex SlugInvalid = new("[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly TimeSpan InitialBackoff = TimeSpan.FromSeconds(2);

    /// <summary>After this many failed attempts in a row, a local OBS that isn't even running stops being
    /// retried — the app is also used without OBS, and hammering a closed port forever is pointless.</summary>
    private const int FailuresBeforeProcessCheck = 3;
    private static readonly TimeSpan ProcessPollInterval = TimeSpan.FromSeconds(5);
    private static readonly string[] ObsProcessNames = ["obs64", "obs32", "obs"];

    private readonly Lock _clientLock = new();
    private readonly ObsState _cache = new();
    private readonly SemaphoreSlim _settingsSignal = new(0, int.MaxValue);
    private ObsClient? _client;
    private CancellationTokenSource? _sessionCts;
    private IPluginStatusItem? _statusItem;
    private ObsConnectionState _connState = ObsConnectionState.Disabled;
    private string? _lastLoggedError;
    private volatile bool _obsExitStarted;

    public ObsState Cache => _cache;

    public static string Slug(string name) => SlugInvalid.Replace(name.ToLowerInvariant(), "_").Trim('_');

    /// <summary>Used by the OBS actions. Throws <see cref="InvalidOperationException"/> if not
    /// currently connected — actions don't queue or wait for a connection, a button press while OBS is
    /// closed should fail fast and visibly (ActionDispatcher logs it, it never silently no-ops).</summary>
    public Task<JsonObject> RequestAsync(string requestType, JsonObject? requestData, CancellationToken cancellationToken)
    {
        var client = CurrentClient ?? throw new InvalidOperationException("Not connected to OBS.");
        return client.RequestAsync(requestType, requestData, cancellationToken);
    }

    private ObsClient? CurrentClient { get { lock (_clientLock) return _client; } }

    /// <summary>Called by <see cref="ObsSettingsPage.Save"/> — cancels the current session (if any) and
    /// resets backoff so a corrected password/host takes effect within moments, not on the next scheduled
    /// retry; also what breaks the AuthFailed hold once the user fixes the password.</summary>
    public void NotifySettingsChanged()
    {
        _settingsSignal.Release();
        try { _sessionCts?.Cancel(); }
        catch (ObjectDisposedException) { } // session ended between the read and the Cancel — nothing to cancel.
    }
}
