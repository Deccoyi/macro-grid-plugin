using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace MacroStation.Plugin.Obs;

/// <summary>Thrown when OBS accepts the connection but rejects a request (wrong scene name, no such input, ...).</summary>
public sealed class ObsRequestException(string requestType, int statusCode, string? comment)
    : Exception($"obs-websocket request '{requestType}' failed ({statusCode}){(comment is null ? "" : $": {comment}")}")
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>Thrown when the initial Hello/Identify handshake itself fails (bad password, protocol mismatch, timeout).</summary>
public sealed class ObsAuthException(string message, WebSocketCloseStatus? closeStatus = null) : Exception(message)
{
    /// <summary>4009/4010/4012 mean "don't bother retrying until settings change" — see ObsConnection's
    /// close-code handling. Null when the failure wasn't a clean close
    /// (timeout, TCP drop) and should retry with normal backoff instead.</summary>
    public WebSocketCloseStatus? CloseStatus { get; } = closeStatus;
}

public readonly record struct ObsBatchResult(string RequestType, bool Success, int StatusCode, string? Comment, JsonObject ResponseData);

/// <summary>
/// A single obs-websocket v5 connection: raw <see cref="ClientWebSocket"/> plumbing, the Hello/Identify
/// handshake (see <see cref="ObsAuth"/>), request/response correlation by requestId, RequestBatch support,
/// and an event feed. One instance = one live connection; <see cref="ObsConnection"/> owns reconnecting a
/// new instance when this one dies. Not thread-safe beyond what ClientWebSocket itself guarantees (one
/// concurrent send, one concurrent receive) — sends are serialized behind <see cref="_sendLock"/>.
/// </summary>
public sealed class ObsClient : IAsyncDisposable
{
    private const int ConnectTimeoutMs = 5000;
    private const int RequestTimeoutMs = 5000;

    private readonly ClientWebSocket _socket = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonObject>> _pending = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonArray>> _pendingBatches = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly TaskCompletionSource<Exception?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly byte[] _receiveBuffer = new byte[16 * 1024];
    private volatile bool _closed;
    private Task? _receiveLoop;

    /// <summary>Fires on the thread pool for every `Event` message once identified. eventType, eventData.</summary>
    public event Action<string, JsonObject>? EventReceived;

    /// <summary>Completes exactly once, when the receive loop exits for any reason (server closed, network
    /// error, ...). Safe to await at any time, unlike a plain event which can fire before a late subscriber
    /// attaches. The result is the failure exception, or null for a clean close.</summary>
    public Task<Exception?> Completion => _completion.Task;

    /// <summary>The WebSocket close status once the connection has ended, if the server sent one (e.g. 4009
    /// wrong password) — used by ObsConnection to stop retrying on an auth failure instead of backing off.</summary>
    public WebSocketCloseStatus? CloseStatus { get; private set; }

    /// <summary>Session frame counters, exposed as obs.ws.in/obs.ws.out — debugging aid for §4b's "one
    /// frame in, one frame out per tick" claim, not used for any decision-making.</summary>
    public int FramesSent { get; private set; }
    public int FramesReceived { get; private set; }

    public static async Task<ObsClient> ConnectAsync(string host, int port, string password, CancellationToken cancellationToken)
    {
        var client = new ObsClient();
        try
        {
            // ClientWebSocketOptions can only be set before ConnectAsync — setting them after throws
            // "The WebSocket has already been started." (caught by manual testing against real OBS).
            client._socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            client._socket.Options.KeepAliveTimeout = TimeSpan.FromSeconds(10);

            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(ConnectTimeoutMs);
            try
            {
                await client._socket.ConnectAsync(new Uri($"ws://{host}:{port}"), connectCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ObsAuthException($"OBS'e {ConnectTimeoutMs / 1000}s içinde bağlanılamadı.");
            }

            await client.HandshakeAsync(password, cancellationToken);
            client._receiveLoop = client.ReceiveLoopAsync();
            return client;
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }

    private async Task HandshakeAsync(string password, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ConnectTimeoutMs);
        JsonObject? hello;
        try
        {
            hello = await ReceiveOneAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ObsAuthException("OBS Hello mesajı zaman aşımına uğradı.");
        }
        if (hello is null) throw new ObsAuthException("Sunucu bağlantıyı Hello mesajı göndermeden kapattı.", _socket.CloseStatus);
        if (hello.TryGetInt("op") != 0) throw new ObsAuthException("Beklenmeyen ilk mesaj (Hello değil).");

        var data = hello.TryGetObject("d") ?? [];
        var identify = new JsonObject
        {
            ["rpcVersion"] = 1,
            ["eventSubscriptions"] = (int)(ObsEventSubscription.General | ObsEventSubscription.Config | ObsEventSubscription.Scenes
                | ObsEventSubscription.Inputs | ObsEventSubscription.Transitions | ObsEventSubscription.Outputs
                | ObsEventSubscription.SceneItems | ObsEventSubscription.Ui),
        };

        if (data.TryGetObject("authentication") is { } auth)
        {
            var salt = auth.TryGetString("salt") ?? "";
            var challenge = auth.TryGetString("challenge") ?? "";
            identify["authentication"] = ObsAuth.ComputeAuthenticationString(password, salt, challenge);
        }

        await SendAsync(1, identify, cancellationToken);

        JsonObject? identified;
        try
        {
            identified = await ReceiveOneAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ObsAuthException("OBS Identify yanıtı zaman aşımına uğradı.");
        }
        if (identified is null)
            throw new ObsAuthException("Identify sonrası bağlantı kapandı (yanlış şifre olabilir).", _socket.CloseStatus);
        if (identified.TryGetInt("op") != 2)
            throw new ObsAuthException("Kimlik doğrulama başarısız (yanlış şifre olabilir).", _socket.CloseStatus);
    }

    /// <summary>Sends a Request and awaits its matching RequestResponse, with a per-request timeout. Throws
    /// <see cref="ObsRequestException"/> if OBS reports failure.</summary>
    public async Task<JsonObject> RequestAsync(string requestType, JsonObject? requestData, CancellationToken cancellationToken)
    {
        if (_closed) throw new IOException("OBS bağlantısı kapalı.");

        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<JsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        // A request registered after the receive loop already ended would otherwise wait forever.
        // Re-check _closed right after registering, since ReceiveLoopAsync's finally only
        // fails requests that were already in _pending at the moment it ran.
        if (_closed)
        {
            _pending.TryRemove(requestId, out _);
            throw new IOException("OBS bağlantısı kapalı.");
        }

        try
        {
            var payload = new JsonObject { ["requestType"] = requestType, ["requestId"] = requestId };
            if (requestData is not null) payload["requestData"] = requestData;
            await SendAsync(6, payload, cancellationToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(RequestTimeoutMs);
            using var registration = timeoutCts.Token.Register(() => tcs.TrySetCanceled(timeoutCts.Token));
            JsonObject response;
            try
            {
                response = await tcs.Task;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"OBS isteği '{requestType}' {RequestTimeoutMs / 1000}s içinde yanıtlanmadı.");
            }

            var status = response.TryGetObject("requestStatus");
            if (!(status.TryGetBool("result")))
            {
                var code = status.TryGetInt("code", -1);
                var comment = status.TryGetString("comment");
                throw new ObsRequestException(requestType, code, comment);
            }

            return response.TryGetObject("responseData") ?? [];
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    /// <summary>Sends every request in one RequestBatch (op 8) frame — one frame in, one frame out,
    /// regardless of how many requests it carries (this is what keeps
    /// OBS's per-second message counter from climbing). <paramref name="haltOnFailure"/> false so one
    /// failing request (e.g. a stale scene name) doesn't cancel the rest of the batch.</summary>
    public async Task<IReadOnlyList<ObsBatchResult>> RequestBatchAsync(IReadOnlyList<(string Type, JsonObject? Data)> requests, CancellationToken cancellationToken)
    {
        if (requests.Count == 0) return [];
        if (_closed) throw new IOException("OBS bağlantısı kapalı.");

        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<JsonArray>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingBatches[requestId] = tcs;
        if (_closed)
        {
            _pendingBatches.TryRemove(requestId, out _);
            throw new IOException("OBS bağlantısı kapalı.");
        }

        try
        {
            var items = new JsonArray();
            foreach (var (type, data) in requests)
            {
                var item = new JsonObject { ["requestType"] = type, ["requestId"] = Guid.NewGuid().ToString("N") };
                if (data is not null) item["requestData"] = data;
                items.Add(item);
            }

            var payload = new JsonObject { ["requestId"] = requestId, ["haltOnFailure"] = false, ["requests"] = items };
            await SendAsync(8, payload, cancellationToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(RequestTimeoutMs);
            using var registration = timeoutCts.Token.Register(() => tcs.TrySetCanceled(timeoutCts.Token));
            JsonArray results;
            try
            {
                results = await tcs.Task;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"OBS batch isteği {RequestTimeoutMs / 1000}s içinde yanıtlanmadı.");
            }

            var parsed = new List<ObsBatchResult>(results.Count);
            foreach (var node in results)
            {
                var item = node as JsonObject;
                var status = item.TryGetObject("requestStatus");
                parsed.Add(new ObsBatchResult(
                    item?.TryGetString("requestType") ?? "",
                    status.TryGetBool("result"),
                    status.TryGetInt("code", -1),
                    status.TryGetString("comment"),
                    item.TryGetObject("responseData") ?? []));
            }
            return parsed;
        }
        finally
        {
            _pendingBatches.TryRemove(requestId, out _);
        }
    }

    private async Task SendAsync(int op, JsonObject data, CancellationToken cancellationToken)
    {
        var frame = new JsonObject { ["op"] = op, ["d"] = data };
        var bytes = Encoding.UTF8.GetBytes(frame.ToJsonString());
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            FramesSent++;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>Reads exactly one JSON text frame, reusing the instance's own buffer (no more
    /// allocating a fresh 16 KB buffer + MemoryStream per message).</summary>
    private async Task<JsonObject?> ReceiveOneAsync(CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await _socket.ReceiveAsync(_receiveBuffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close) return null;
            stream.Write(_receiveBuffer, 0, result.Count);
        } while (!result.EndOfMessage);

        stream.Position = 0;
        try
        {
            return JsonNode.Parse(stream) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private async Task ReceiveLoopAsync()
    {
        Exception? failure = null;
        try
        {
            while (_socket.State == WebSocketState.Open)
            {
                var message = await ReceiveOneAsync(CancellationToken.None);
                if (message is null) break;
                FramesReceived++;
                Dispatch(message);
            }
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            _closed = true;
            CloseStatus = _socket.CloseStatus;
            // Any request still awaiting a response when the socket dies would otherwise hang forever.
            var lost = failure ?? new IOException("OBS bağlantısı koptu.");
            foreach (var (_, tcs) in _pending) tcs.TrySetException(lost);
            foreach (var (_, tcs) in _pendingBatches) tcs.TrySetException(lost);
            _completion.TrySetResult(failure);
        }
    }

    private void Dispatch(JsonObject message)
    {
        var op = message.TryGetInt("op", -1);
        var data = message.TryGetObject("d") ?? [];
        switch (op)
        {
            case 7 when data.TryGetString("requestId") is { } requestId && _pending.TryGetValue(requestId, out var tcs):
                tcs.TrySetResult(data);
                break;
            case 9 when data.TryGetString("requestId") is { } batchId && _pendingBatches.TryGetValue(batchId, out var batchTcs):
                batchTcs.TrySetResult(data.TryGetArray("results") ?? []);
                break;
            case 5 when data.TryGetString("eventType") is { } eventType:
                EventReceived?.Invoke(eventType, data.TryGetObject("eventData") ?? []);
                break;
        }
    }

    /// <summary>Closes the socket right away instead of waiting for the OS to notice a dead TCP connection
    /// (used both for a normal Dispose and for OBS's own `ExitStarted` event).</summary>
    public async ValueTask DisposeAsync()
    {
        _closed = true;
        try
        {
            if (_socket.State == WebSocketState.Open)
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch { /* best-effort close */ }

        if (_receiveLoop is not null)
        {
            try { await _receiveLoop.WaitAsync(TimeSpan.FromSeconds(2)); } catch { /* loop already exiting */ }
        }

        _socket.Dispose();
        _completion.TrySetResult(null);
    }
}

/// <summary>obs-websocket's EventSubscription bitmask — only the low bits (General..Ui) are ever
/// requested; 1&lt;&lt;16 and up (InputVolumeMeters, ...) are deliberately never subscribed to, since
/// they fire many times a second per input (Inputs was wrongly `1&lt;&lt;4`, which is actually
/// Transitions, so InputMuteStateChanged etc. never arrived).</summary>
[Flags]
public enum ObsEventSubscription
{
    General = 1 << 0,
    Config = 1 << 1,
    Scenes = 1 << 2,
    Inputs = 1 << 3,
    Transitions = 1 << 4,
    Filters = 1 << 5,
    Outputs = 1 << 6,
    SceneItems = 1 << 7,
    MediaInputs = 1 << 8,
    Vendors = 1 << 9,
    Ui = 1 << 10,
}
