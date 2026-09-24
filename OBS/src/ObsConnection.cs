using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

public enum ObsConnectionState { Disabled, Connecting, Connected, Reconnecting, WaitingForObs, AuthFailed, Error }

/// <summary>
/// Owns the (re)connecting OBS WebSocket connection for the whole plugin lifetime: an
/// <see cref="IVariableProvider"/> that never returns while the process is alive, publishing "obs.*"
/// variables and reconnecting with capped exponential backoff whenever OBS isn't running or the
/// connection drops. Action handlers (<see cref="ObsActions"/>) share this same instance's
/// <see cref="RequestAsync"/>/<see cref="Cache"/> to actually do things — there's exactly one connection
/// to OBS. Settings are re-read once on (re)connect and whenever <see cref="NotifySettingsChanged"/> is
/// called by the settings page, not on every loop tick.
/// </summary>
public sealed class ObsConnection(IPluginHost host) : IVariableProvider, IVariableCatalogSource
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

    /// <summary>Used by <see cref="ObsActions"/>. Throws <see cref="InvalidOperationException"/> if not
    /// currently connected — actions don't queue or wait for a connection, a button press while OBS is
    /// closed should fail fast and visibly (ActionDispatcher logs it, it never silently no-ops).</summary>
    public Task<JsonObject> RequestAsync(string requestType, JsonObject? requestData, CancellationToken cancellationToken)
    {
        var client = CurrentClient ?? throw new InvalidOperationException("OBS'e bağlı değil.");
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

    public IEnumerable<VariableInfo> Describe()
    {
        yield return new("obs.connected", "OBS'e bağlı mı", "{obs.connected}", Category);
        yield return new("obs.status", "Bağlantı durumu metni", "{obs.status}", Category);
        yield return new("obs.ws.in", "Bu oturumda alınan mesaj sayısı", "{obs.ws.in}", Category);
        yield return new("obs.ws.out", "Bu oturumda gönderilen mesaj sayısı", "{obs.ws.out}", Category);

        yield return new("obs.scene.current", "Aktif program sahnesi", "{obs.scene.current}", Category);
        yield return new("obs.scene.preview", "Önizleme sahnesi (stüdyo modu)", "{obs.scene.preview}", Category);
        yield return new("obs.studioMode", "Stüdyo modu açık mı", "{obs.studioMode}", Category);
        yield return new("obs.transition.current", "Aktif geçiş", "{obs.transition.current}", Category);
        yield return new("obs.profile.current", "Aktif profil", "{obs.profile.current}", Category);
        yield return new("obs.sceneCollection.current", "Aktif sahne koleksiyonu", "{obs.sceneCollection.current}", Category);

        yield return new("obs.streaming", "Yayın açık mı", "{obs.streaming}", Category);
        yield return new("obs.stream.reconnecting", "Yayın yeniden bağlanıyor mu", "{obs.stream.reconnecting}", Category);
        yield return new("obs.stream.duration", "Yayın süresi", "{obs.stream.duration}", Category);
        yield return new("obs.stream.timecode", "Yayın zaman kodu", "{obs.stream.timecode}", Category);
        yield return new("obs.stream.congestion", "Yayın tıkanıklığı (%)", "{obs.stream.congestion|0}%", Category);
        yield return new("obs.stream.bytes", "Gönderilen bayt", "{obs.stream.bytes}", Category);
        yield return new("obs.stream.kbps", "Yayın hızı (kbps)", "{obs.stream.kbps|0}", Category);
        yield return new("obs.stream.frames.dropped", "Düşen kare", "{obs.stream.frames.dropped}", Category);
        yield return new("obs.stream.frames.total", "Toplam kare", "{obs.stream.frames.total}", Category);
        yield return new("obs.stream.frames.droppedPercent", "Düşen kare (%)", "{obs.stream.frames.droppedPercent|1}%", Category);

        yield return new("obs.recording", "Kayıt açık mı", "{obs.recording}", Category);
        yield return new("obs.record.paused", "Kayıt duraklatıldı mı", "{obs.record.paused}", Category);
        yield return new("obs.record.duration", "Kayıt süresi", "{obs.record.duration}", Category);
        yield return new("obs.record.timecode", "Kayıt zaman kodu", "{obs.record.timecode}", Category);
        yield return new("obs.record.bytes", "Kaydedilen bayt", "{obs.record.bytes}", Category);
        yield return new("obs.record.kbps", "Kayıt hızı (kbps)", "{obs.record.kbps|0}", Category);

        yield return new("obs.virtualcam", "Sanal kamera açık mı", "{obs.virtualcam}", Category);
        yield return new("obs.replayBuffer", "Tekrar arabelleği açık mı", "{obs.replayBuffer}", Category);

        yield return new("obs.stats.fps", "OBS render FPS", "{obs.stats.fps|0}", Category);
        yield return new("obs.stats.cpu", "OBS CPU kullanımı (%)", "{obs.stats.cpu|0}%", Category);
        yield return new("obs.stats.memory", "Bellek kullanımı (MB)", "{obs.stats.memory|0}", Category);
        yield return new("obs.stats.disk", "Boş disk (MB)", "{obs.stats.disk|0}", Category);
        yield return new("obs.stats.renderTime", "Ortalama render süresi (ms)", "{obs.stats.renderTime|1}", Category);
        yield return new("obs.stats.render.skipped", "Atlanan render karesi", "{obs.stats.render.skipped}", Category);
        yield return new("obs.stats.render.total", "Toplam render karesi", "{obs.stats.render.total}", Category);
        yield return new("obs.stats.render.skippedPercent", "Atlanan render karesi (%)", "{obs.stats.render.skippedPercent|1}%", Category);
        yield return new("obs.stats.output.skipped", "Atlanan çıkış karesi", "{obs.stats.output.skipped}", Category);
        yield return new("obs.stats.output.total", "Toplam çıkış karesi", "{obs.stats.output.total}", Category);
        yield return new("obs.stats.output.skippedPercent", "Atlanan çıkış karesi (%)", "{obs.stats.output.skippedPercent|1}%", Category);

        foreach (var input in _cache.AudioInputNames)
        {
            var slug = Slug(input);
            yield return new($"obs.input.{slug}.muted", $"{input} — sessiz mi", $"{{obs.input.{slug}.muted}}", Category);
            yield return new($"obs.input.{slug}.volumeDb", $"{input} — ses seviyesi (dB)", $"{{obs.input.{slug}.volumeDb|1}}", Category);
        }
        foreach (var scene in _cache.Scenes)
        {
            foreach (var item in _cache.SceneItems(scene))
            {
                var slug = $"{Slug(scene)}.{Slug(item.SourceName)}";
                yield return new($"obs.item.{slug}.visible", $"{scene} › {item.SourceName} — görünür mü", $"{{obs.item.{slug}.visible}}", Category);
            }
        }
    }

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
                LogOnce("OBS çalışmıyor; açılana kadar bağlantı denemeleri duraklatıldı.");
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
                host.Log($"OBS'e bağlanıldı ({settings.Host}:{settings.Port})");
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
                    host.Log($"OBS bağlantısı koptu: {failure.Message}");
            }
            catch (ObsAuthException ex) when (IsHardAuthFailure(ex.CloseStatus))
            {
                host.Log($"OBS kimlik doğrulaması başarısız, ayarlar değişene kadar tekrar denenmeyecek: {ex.Message}");
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
                LogOnce($"OBS'e bağlanılamadı: {ex.Message}");
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
                host.Log("OBS ayarları değişti, yeniden bağlanılıyor.");
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

    /// <summary>Batches GetStreamStatus+GetRecordStatus+GetStats into a single RequestBatch frame every
    /// tick — 1s while streaming or recording (duration/timecode need it live), 5s otherwise. Scene/input/
    /// profile/etc. come only from events (see HandleEvent), never polled.</summary>
    private async Task RunTickLoopAsync(ObsClient client, IVariableStore store, CancellationToken cancellationToken)
    {
        while (true)
        {
            var streaming = (bool?)store.Get("obs.streaming") ?? false;
            var recording = (bool?)store.Get("obs.recording") ?? false;
            var interval = streaming || recording ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(5);

            var tick = Task.Delay(interval, cancellationToken);
            var completed = await Task.WhenAny(tick, client.Completion);
            if (completed == client.Completion) return;
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                var results = await client.RequestBatchAsync(
                    [("GetStreamStatus", null), ("GetRecordStatus", null), ("GetStats", null)], cancellationToken);
                foreach (var result in results)
                {
                    if (!result.Success) continue;
                    switch (result.RequestType)
                    {
                        case "GetStreamStatus": PublishStreamStatus(store, result.ResponseData); break;
                        case "GetRecordStatus": PublishRecordStatus(store, result.ResponseData); break;
                        case "GetStats": PublishStats(store, result.ResponseData); break;
                    }
                }
                store.Set("obs.ws.in", client.FramesReceived);
                store.Set("obs.ws.out", client.FramesSent);
                UpdateConnectedStatusText(streaming, recording, store);
            }
            catch (Exception ex) when (ex is ObsRequestException or TimeoutException or IOException)
            {
                host.Log($"OBS durum sorgusu başarısız (muhtemelen geçici): {ex.Message}");
            }
        }
    }

    private static void PublishStreamStatus(IVariableStore store, JsonObject data)
    {
        var bytes = data.TryGetDouble("outputBytes");
        var prevBytes = (double?)store.Get("obs.stream.bytes") ?? bytes;
        store.Set("obs.streaming", data.TryGetBool("outputActive"));
        store.Set("obs.stream.reconnecting", data.TryGetBool("outputReconnecting"));
        store.Set("obs.stream.duration", TimeSpan.FromMilliseconds(data.TryGetDouble("outputDuration")));
        store.Set("obs.stream.timecode", data.TryGetString("outputTimecode"));
        store.Set("obs.stream.congestion", data.TryGetDouble("outputCongestion") * 100);
        store.Set("obs.stream.bytes", bytes);
        store.Set("obs.stream.kbps", Math.Max(0, (bytes - prevBytes) * 8 / 1000));
        var dropped = data.TryGetDouble("outputSkippedFrames");
        var total = data.TryGetDouble("outputTotalFrames");
        store.Set("obs.stream.frames.dropped", dropped);
        store.Set("obs.stream.frames.total", total);
        store.Set("obs.stream.frames.droppedPercent", total > 0 ? dropped / total * 100 : 0);
    }

    private static void PublishRecordStatus(IVariableStore store, JsonObject data)
    {
        var bytes = data.TryGetDouble("outputBytes");
        var prevBytes = (double?)store.Get("obs.record.bytes") ?? bytes;
        store.Set("obs.recording", data.TryGetBool("outputActive"));
        store.Set("obs.record.paused", data.TryGetBool("outputPaused"));
        store.Set("obs.record.duration", TimeSpan.FromMilliseconds(data.TryGetDouble("outputDuration")));
        store.Set("obs.record.timecode", data.TryGetString("outputTimecode"));
        store.Set("obs.record.bytes", bytes);
        store.Set("obs.record.kbps", Math.Max(0, (bytes - prevBytes) * 8 / 1000));
    }

    private static void PublishStats(IVariableStore store, JsonObject data)
    {
        store.Set("obs.stats.fps", data.TryGetDouble("activeFps"));
        store.Set("obs.stats.cpu", data.TryGetDouble("cpuUsage"));
        store.Set("obs.stats.memory", data.TryGetDouble("memoryUsage"));
        store.Set("obs.stats.disk", data.TryGetDouble("availableDiskSpace"));
        store.Set("obs.stats.renderTime", data.TryGetDouble("averageFrameRenderTime"));
        var renderSkipped = data.TryGetDouble("renderSkippedFrames");
        var renderTotal = data.TryGetDouble("renderTotalFrames");
        store.Set("obs.stats.render.skipped", renderSkipped);
        store.Set("obs.stats.render.total", renderTotal);
        store.Set("obs.stats.render.skippedPercent", renderTotal > 0 ? renderSkipped / renderTotal * 100 : 0);
        var outputSkipped = data.TryGetDouble("outputSkippedFrames");
        var outputTotal = data.TryGetDouble("outputTotalFrames");
        store.Set("obs.stats.output.skipped", outputSkipped);
        store.Set("obs.stats.output.total", outputTotal);
        store.Set("obs.stats.output.skippedPercent", outputTotal > 0 ? outputSkipped / outputTotal * 100 : 0);
    }

    private void PublishSceneAndInputVariables(IVariableStore store)
    {
        store.Set("obs.scene.current", _cache.CurrentScene);
        store.Set("obs.scene.preview", _cache.CurrentPreviewScene);
        store.Set("obs.studioMode", _cache.StudioMode);
        store.Set("obs.transition.current", _cache.CurrentTransition);
        store.Set("obs.profile.current", _cache.CurrentProfile);
        store.Set("obs.sceneCollection.current", _cache.CurrentSceneCollection);

        foreach (var input in _cache.AudioInputNames)
        {
            var slug = Slug(input);
            store.Set($"obs.input.{slug}.muted", _cache.GetInputMuted(input));
            store.Set($"obs.input.{slug}.volumeDb", _cache.GetInputVolumeDb(input));
        }
        foreach (var scene in _cache.Scenes)
        {
            foreach (var item in _cache.SceneItems(scene))
                store.Set($"obs.item.{Slug(scene)}.{Slug(item.SourceName)}.visible", item.Enabled);
        }
    }

    private void HandleEvent(IVariableStore store, string eventType, JsonObject data)
    {
        switch (eventType)
        {
            case "ExitStarted":
                // OBS itself is shutting down — close now instead of waiting for the OS to notice a dead
                // TCP connection. RunAsync's tick loop returns once client.Completion completes.
                _obsExitStarted = true;
                _ = CurrentClient?.DisposeAsync();
                break;

            case "CurrentProgramSceneChanged":
                _cache.OnCurrentSceneChanged(data.TryGetString("sceneName") ?? "");
                store.Set("obs.scene.current", _cache.CurrentScene);
                break;
            case "CurrentPreviewSceneChanged":
                _cache.OnCurrentPreviewSceneChanged(data.TryGetString("sceneName") ?? "");
                store.Set("obs.scene.preview", _cache.CurrentPreviewScene);
                break;
            case "SceneCreated":
                _cache.OnSceneCreated(data.TryGetString("sceneName") ?? "");
                break;
            case "SceneRemoved":
                _cache.OnSceneRemoved(data.TryGetString("sceneName") ?? "");
                break;
            case "SceneNameChanged":
                _cache.OnSceneRenamed(data.TryGetString("oldSceneName") ?? "", data.TryGetString("sceneName") ?? "");
                break;
            case "SceneListChanged":
                break; // individual Created/Removed/NameChanged events already keep the cache in sync.

            case "StudioModeStateChanged":
                _cache.OnStudioModeChanged(data.TryGetBool("studioModeEnabled"));
                store.Set("obs.studioMode", _cache.StudioMode);
                break;
            case "CurrentProfileChanged":
                _cache.OnCurrentProfileChanged(data.TryGetString("profileName") ?? "");
                store.Set("obs.profile.current", _cache.CurrentProfile);
                break;
            case "CurrentSceneTransitionChanged":
                _cache.OnCurrentTransitionChanged(data.TryGetString("transitionName") ?? "");
                store.Set("obs.transition.current", _cache.CurrentTransition);
                break;
            case "CurrentSceneCollectionChanged":
                // A new scene collection swaps out the entire scene/input graph — full resync.
                var client = CurrentClient;
                if (client is not null) _ = ResyncAsync(client, store);
                break;

            case "InputCreated":
                var caps = data.TryGetInt("inputKindCaps");
                _cache.OnInputCreated(data.TryGetString("inputName") ?? "", data.TryGetString("inputKind") ?? "", (caps & 0b10) != 0);
                break;
            case "InputRemoved":
                RemoveInputVariables(store, data.TryGetString("inputName") ?? "");
                _cache.OnInputRemoved(data.TryGetString("inputName") ?? "");
                break;
            case "InputNameChanged":
                RemoveInputVariables(store, data.TryGetString("oldInputName") ?? "");
                _cache.OnInputRenamed(data.TryGetString("oldInputName") ?? "", data.TryGetString("inputName") ?? "");
                PublishInputVariables(store, data.TryGetString("inputName") ?? "");
                break;
            case "InputMuteStateChanged":
            {
                var name = data.TryGetString("inputName") ?? "";
                _cache.OnInputMuteStateChanged(name, data.TryGetBool("inputMuted"));
                if (_cache.IsAudioInput(name)) store.Set($"obs.input.{Slug(name)}.muted", _cache.GetInputMuted(name));
                break;
            }
            case "InputVolumeChanged":
            {
                var name = data.TryGetString("inputName") ?? "";
                _cache.OnInputVolumeChanged(name, data.TryGetDouble("inputVolumeDb"));
                if (_cache.IsAudioInput(name)) store.Set($"obs.input.{Slug(name)}.volumeDb", _cache.GetInputVolumeDb(name));
                break;
            }

            case "SceneItemEnableStateChanged":
            {
                var sceneName = data.TryGetString("sceneName") ?? "";
                var itemId = data.TryGetInt("sceneItemId");
                var enabled = data.TryGetBool("sceneItemEnabled");
                _cache.OnSceneItemEnableStateChanged(sceneName, itemId, enabled);
                var item = _cache.SceneItems(sceneName).FirstOrDefault(i => i.SceneItemId == itemId);
                if (item is not null) store.Set($"obs.item.{Slug(sceneName)}.{Slug(item.SourceName)}.visible", enabled);
                break;
            }

            case "StreamStateChanged":
                store.Set("obs.streaming", data.TryGetBool("outputActive"));
                break;
            case "RecordStateChanged":
                store.Set("obs.recording", data.TryGetBool("outputActive"));
                break;
            case "VirtualcamStateChanged":
                store.Set("obs.virtualcam", data.TryGetBool("outputActive"));
                break;
            case "ReplayBufferStateChanged":
                store.Set("obs.replayBuffer", data.TryGetBool("outputActive"));
                break;
        }
    }

    private async Task ResyncAsync(ObsClient client, IVariableStore store)
    {
        try
        {
            await _cache.RefreshAllAsync(client, CancellationToken.None);
            PublishSceneAndInputVariables(store);
        }
        catch (Exception ex) when (ex is ObsRequestException or TimeoutException or IOException)
        {
            host.Log($"Sahne koleksiyonu değişikliğinden sonra yeniden senkronizasyon başarısız: {ex.Message}");
        }
    }

    private void PublishInputVariables(IVariableStore store, string name)
    {
        if (!_cache.IsAudioInput(name)) return;
        var slug = Slug(name);
        store.Set($"obs.input.{slug}.muted", _cache.GetInputMuted(name));
        store.Set($"obs.input.{slug}.volumeDb", _cache.GetInputVolumeDb(name));
    }

    private void RemoveInputVariables(IVariableStore store, string name)
    {
        var slug = Slug(name);
        store.Remove($"obs.input.{slug}.muted");
        store.Remove($"obs.input.{slug}.volumeDb");
    }

    /// <summary>On disconnect every obs.* value is reset (not just the three booleans ), and
    /// dynamic per-input/per-item variables are removed rather than left at a stale last-known value.</summary>
    private void ResetAllVariables(IVariableStore store)
    {
        foreach (var info in Describe())
            store.Remove(info.Name);
        store.Set("obs.connected", false);
    }

    private void SetState(IVariableStore store, ObsConnectionState state, TimeSpan? retryIn = null)
    {
        _connState = state;
        store.Set("obs.connected", state == ObsConnectionState.Connected);
        var (text, level) = state switch
        {
            ObsConnectionState.Disabled => ("OBS · kapalı", StatusLevel.Idle),
            ObsConnectionState.Connecting => ("OBS · bağlanıyor…", StatusLevel.Busy),
            ObsConnectionState.Connected => ("OBS · bağlı", StatusLevel.Ok),
            ObsConnectionState.Reconnecting => ($"OBS · {retryIn?.TotalSeconds:0}s sonra tekrar denenecek", StatusLevel.Warning),
            ObsConnectionState.WaitingForObs => ("OBS · çalışmıyor", StatusLevel.Idle),
            ObsConnectionState.AuthFailed => ("OBS · şifre hatalı", StatusLevel.Error),
            _ => ("OBS · hata", StatusLevel.Error),
        };
        store.Set("obs.status", text);
        _statusItem?.Update(text, level, "video");
    }

    private void UpdateConnectedStatusText(bool wasStreaming, bool wasRecording, IVariableStore store)
    {
        if (_connState != ObsConnectionState.Connected) return;
        var fps = (double?)store.Get("obs.stats.fps") ?? 0;
        var streaming = (bool?)store.Get("obs.streaming") ?? false;
        var duration = store.Get(streaming ? "obs.stream.duration" : "obs.record.duration") as TimeSpan?;
        var text = streaming || wasStreaming || (bool?)store.Get("obs.recording") == true || wasRecording
            ? $"OBS · {fps:0} fps · ● {duration?.ToString(@"hh\:mm\:ss") ?? "00:00:00"}"
            : $"OBS · {fps:0} fps";
        store.Set("obs.status", text);
        _statusItem?.Update(text, StatusLevel.Ok, "video");
    }
}
