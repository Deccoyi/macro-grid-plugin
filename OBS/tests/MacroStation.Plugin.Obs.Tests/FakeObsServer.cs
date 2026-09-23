using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace MacroStation.Plugin.Obs.Tests;

/// <summary>
/// An in-process fake obs-websocket v5 server — an <see cref="HttpListener"/> WebSocket, exactly like
/// the connection layer needs. Each test supplies its
/// own async script (<paramref name="handleConnection"/> below) describing what the fake server does when
/// a client connects — send Hello, wait for Identify, answer/ignore/hang on requests, close with a
/// specific code, drop the connection abruptly — so the scripts stay readable per-test instead of being
/// buried in one giant configurable server class.
/// </summary>
public sealed class FakeObsServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _acceptLoop;

    public int Port { get; }

    public FakeObsServer()
    {
        Port = GetFreePort();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
    }

    public void Start(Func<FakeObsConnection, CancellationToken, Task> handleConnection)
    {
        _listener.Start();
        _acceptLoop = AcceptLoopAsync(handleConnection);
    }

    private async Task AcceptLoopAsync(Func<FakeObsConnection, CancellationToken, Task> handleConnection)
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(_cts.Token);
            }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (HttpListenerException) { return; }

            _ = HandleOneAsync(ctx, handleConnection);
        }
    }

    /// <summary>Every exception a connection script raises (including a failed xUnit Assert inside the
    /// script) — the script runs fire-and-forget on the accept loop, so without this a bug in the script
    /// itself just looks like the client hanging, with no indication why.</summary>
    public List<Exception> ScriptErrors { get; } = [];

    private async Task HandleOneAsync(HttpListenerContext ctx, Func<FakeObsConnection, CancellationToken, Task> handleConnection)
    {
        if (!ctx.Request.IsWebSocketRequest)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.Close();
            return;
        }

        var wsCtx = await ctx.AcceptWebSocketAsync(null);
        var connection = new FakeObsConnection(wsCtx.WebSocket);
        try
        {
            await handleConnection(connection, _cts.Token);
        }
        catch (Exception) when (_cts.IsCancellationRequested)
        {
            // Server is shutting down mid-script — the client side of the test has already observed
            // whatever effect it was checking for.
        }
        catch (Exception ex)
        {
            lock (ScriptErrors) ScriptErrors.Add(ex);
        }
    }

    private static int GetFreePort()
    {
        using var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch (ObjectDisposedException) { }
        _listener.Close();
        if (_acceptLoop is not null)
        {
            try { await _acceptLoop; } catch { /* best-effort shutdown */ }
        }
    }
}

/// <summary>One accepted connection on the fake server side — raw op-framed JSON send/receive plus the
/// handful of obs-websocket v5 message shapes <see cref="ObsClient"/> actually speaks.</summary>
public sealed class FakeObsConnection(WebSocket socket)
{
    private readonly byte[] _buffer = new byte[16 * 1024];

    public Task SendHelloAsync(string? authSalt = null, string? authChallenge = null, CancellationToken ct = default)
    {
        var d = new JsonObject { ["obsWebSocketVersion"] = "5.0.0", ["rpcVersion"] = 1 };
        if (authSalt is not null)
            d["authentication"] = new JsonObject { ["challenge"] = authChallenge, ["salt"] = authSalt };
        return SendAsync(0, d, ct);
    }

    public Task SendIdentifiedAsync(CancellationToken ct = default) =>
        SendAsync(2, new JsonObject { ["negotiatedRpcVersion"] = 1 }, ct);

    /// <summary>Reads exactly one JSON text frame. Op -1 with empty data means the client closed the socket.</summary>
    public async Task<(int Op, JsonObject Data)> ReceiveAsync(CancellationToken ct = default)
    {
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(_buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) return (-1, []);
            stream.Write(_buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        stream.Position = 0;
        var msg = JsonNode.Parse(stream) as JsonObject ?? [];
        var op = msg.TryGetPropertyValue("op", out var opNode) && opNode is not null ? opNode.GetValue<int>() : -1;
        var data = msg.TryGetPropertyValue("d", out var dNode) ? dNode as JsonObject ?? [] : [];
        return (op, data);
    }

    /// <summary>Answers a single Request (op 6) with a RequestResponse (op 7).</summary>
    public Task RespondRequestAsync(string requestId, string requestType, bool success, JsonObject? responseData = null, int code = 100, string? comment = null, CancellationToken ct = default) =>
        SendAsync(7, new JsonObject
        {
            ["requestType"] = requestType,
            ["requestId"] = requestId,
            ["requestStatus"] = new JsonObject { ["result"] = success, ["code"] = code, ["comment"] = comment },
            ["responseData"] = responseData,
        }, ct);

    /// <summary>Answers a RequestBatch (op 8) with a RequestBatchResponse (op 9). Every item in
    /// <paramref name="requests"/> (as received) succeeds with an empty responseData unless overridden.</summary>
    public Task RespondBatchAsync(string batchRequestId, JsonArray requests, CancellationToken ct = default)
    {
        var results = new JsonArray();
        foreach (var node in requests)
        {
            var item = node as JsonObject;
            results.Add(new JsonObject
            {
                ["requestType"] = item?.TryGetPropertyValue("requestType", out var t) == true ? t?.DeepClone() : "",
                ["requestId"] = item?.TryGetPropertyValue("requestId", out var id) == true ? id?.DeepClone() : "",
                ["requestStatus"] = new JsonObject { ["result"] = true, ["code"] = 100 },
                ["responseData"] = new JsonObject(),
            });
        }
        return SendAsync(9, new JsonObject { ["requestId"] = batchRequestId, ["results"] = results }, ct);
    }

    public Task SendEventAsync(string eventType, JsonObject? eventData = null, CancellationToken ct = default) =>
        SendAsync(5, new JsonObject { ["eventType"] = eventType, ["eventIntent"] = 1, ["eventData"] = eventData }, ct);

    public Task SendAsync(int op, JsonObject data, CancellationToken ct = default)
    {
        var frame = new JsonObject { ["op"] = op, ["d"] = data };
        var bytes = Encoding.UTF8.GetBytes(frame.ToJsonString());
        return socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
    }

    public Task CloseAsync(int closeStatusCode, string? description, CancellationToken ct = default) =>
        socket.CloseAsync((WebSocketCloseStatus)closeStatusCode, description, ct);

    /// <summary>Ungraceful reset — no close handshake — to simulate a dropped Wi-Fi connection/killed OBS
    /// process rather than a clean shutdown.</summary>
    public void Abort() => socket.Abort();
}
