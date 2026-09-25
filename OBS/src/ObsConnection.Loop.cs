using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Connect loop: settings, reconnect with backoff, waiting for the OBS process.
public sealed partial class ObsConnection
{
    public async Task RunAsync(IVariableStore store, CancellationToken cancellationToken)
    {
        _statusItem = host.CreateStatusItem("connection");
        SetState(store, ObsConnectionState.Disabled);
        var backoff = InitialBackoff;
        var failures = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Settings are re-read right below, so any save signal raised before this point is already covered.
            while (_settingsSignal.Wait(0)) { }
            var settings = ObsSettings.LoadOrCreate(host.DataDirectory);
            if (!settings.Enabled)
            {
                SetState(store, ObsConnectionState.Disabled);
                if (await WaitOrSettingsChangedAsync(TimeSpan.FromSeconds(5), cancellationToken) is null) break;
                continue;
            }

            if (failures >= FailuresBeforeProcessCheck && IsLocalHost(settings.Host) && !IsObsProcessRunning())
            {
                SetState(store, ObsConnectionState.WaitingForObs);
                LogOnce("OBS is not running; connection attempts are paused until it starts.");
                if (!await WaitForObsProcessAsync(cancellationToken)) break;
                backoff = InitialBackoff;
                failures = 0;
                continue;
            }

            SetState(store, ObsConnectionState.Connecting);
            ObsClient? client = null;
            using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _sessionCts = sessionCts;
            var settingsChanged = false;
            try
            {
                client = await ObsClient.ConnectAsync(settings.Host, settings.Port, settings.Password, sessionCts.Token);
                lock (_clientLock) _client = client;
                _lastLoggedError = null;
                host.Log($"Connected to OBS ({settings.Host}:{settings.Port})");
                backoff = InitialBackoff;
                failures = 0;

                client.EventReceived += (type, data) => HandleEvent(store, type, data);
                await _cache.RefreshAllAsync(client, sessionCts.Token);
                PublishSceneAndInputVariables(store);
                SetState(store, ObsConnectionState.Connected);

                await RunTickLoopAsync(client, store, sessionCts.Token);

                if (sessionCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    settingsChanged = true;
                else if (await client.Completion is { } failure)
                    host.Log($"OBS connection lost: {failure.Message}");
            }
            catch (ObsAuthException ex) when (IsHardAuthFailure(ex.CloseStatus))
            {
                host.Log($"OBS authentication failed, not retrying until the settings change: {ex.Message}");
                SetState(store, ObsConnectionState.AuthFailed);
                if (!await WaitForSettingsChangeAsync(cancellationToken)) break;
                continue; // skip the backoff delay below — retry immediately with the new settings.
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException) when (sessionCts.IsCancellationRequested)
            {
                settingsChanged = true;
            }
            catch (Exception ex)
            {
                failures++;
                LogOnce($"Could not connect to OBS: {ex.Message}");
            }
            finally
            {
                _sessionCts = null;
                lock (_clientLock) _client = null;
                if (client is not null) await client.DisposeAsync();
                ResetAllVariables(store); // reads Describe() for the dynamic names to remove, so this runs before _cache.Reset()
                _cache.Reset();
                SetState(store, cancellationToken.IsCancellationRequested ? ObsConnectionState.Disabled : ObsConnectionState.Reconnecting, backoff);
            }

            if (_obsExitStarted)
            {
                // OBS announced its own shutdown — check for the process on the next pass instead of after 3 failures.
                _obsExitStarted = false;
                failures = FailuresBeforeProcessCheck;
            }

            if (settingsChanged)
            {
                host.Log("OBS settings changed, reconnecting.");
                backoff = InitialBackoff;
                failures = 0;
                continue;
            }

            var woke = await WaitOrSettingsChangedAsync(backoff, cancellationToken);
            if (woke is null) break;
            if (woke == true)
            {
                backoff = InitialBackoff;
                failures = 0;
                continue;
            }
            backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 30) + Random.Shared.NextDouble());
        }
    }

    private void LogOnce(string message)
    {
        if (message == _lastLoggedError) return;
        _lastLoggedError = message;
        host.Log(message);
    }

    /// <summary>A process check only means something when OBS is expected on this machine; for a remote
    /// host there's nothing to look at, so it keeps the normal backoff.</summary>
    private static bool IsLocalHost(string host)
    {
        try
        {
            var addresses = IPAddress.TryParse(host, out var ip) ? [ip] : Dns.GetHostAddresses(host);
            var local = NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Select(u => u.Address)
                .ToHashSet();
            return addresses.Any(a => IPAddress.IsLoopback(a) || local.Contains(a));
        }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or ArgumentException or NetworkInformationException)
        {
            return false;
        }
    }

    private static bool IsObsProcessRunning()
    {
        foreach (var name in ObsProcessNames)
        {
            var processes = Process.GetProcessesByName(name);
            foreach (var p in processes) p.Dispose();
            if (processes.Length > 0) return true;
        }
        return false;
    }

    /// <summary>Polls for the OBS process instead of the port — cheap, silent, and returns as soon as OBS
    /// starts (or the user changes settings, e.g. points to another host).</summary>
    private async Task<bool> WaitForObsProcessAsync(CancellationToken cancellationToken)
    {
        while (!IsObsProcessRunning())
        {
            var woke = await WaitOrSettingsChangedAsync(ProcessPollInterval, cancellationToken);
            if (woke is null) return false;
            if (woke == true) return true;
        }
        return true;
    }

    /// <summary>null = cancelled, true = settings were saved, false = the delay simply elapsed.</summary>
    private async Task<bool?> WaitOrSettingsChangedAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try { return await _settingsSignal.WaitAsync(delay, cancellationToken); }
        catch (OperationCanceledException) { return null; }
    }

    /// <summary>4009/4010/4012 — the close codes for "wrong password"/"already
    /// identified"/"unsupported rpc version": none of these are fixed by retrying, only by the user
    /// changing settings, so retrying just floods the log.</summary>
    private static bool IsHardAuthFailure(WebSocketCloseStatus? closeStatus) =>
        closeStatus is not null && (int)closeStatus.Value is 4009 or 4010 or 4012;

    private async Task<bool> WaitForSettingsChangeAsync(CancellationToken cancellationToken)
    {
        try { await _settingsSignal.WaitAsync(cancellationToken); return true; }
        catch (OperationCanceledException) { return false; }
    }
}
