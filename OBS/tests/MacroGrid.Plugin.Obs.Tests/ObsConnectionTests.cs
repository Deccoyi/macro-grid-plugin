using System.Text.Json.Nodes;

namespace MacroGrid.Plugin.Obs.Tests;

/// <summary>
/// Integration tests for <see cref="ObsConnection"/> against a fake obs-websocket v5 server
/// (<see cref="FakeObsServer"/>), covering the reconnect/handshake behavior described in
/// the connection layer's behavior. Each test drives ObsConnection's
/// real <see cref="ObsConnection.RunAsync"/> loop against a script controlling what the fake server does,
/// then asserts on the resulting <see cref="FakeVariableStore"/>/<see cref="FakePluginHost"/> state.
/// </summary>
public sealed class ObsConnectionTests
{
    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, string message, FakeObsServer? server = null, FakePluginHost? host = null)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return;
            if (server is { ScriptErrors.Count: > 0 })
                Assert.Fail($"{message} — fake server script threw: {server.ScriptErrors[0]}");
            await Task.Delay(20);
        }
        var logs = host is null ? "" : $"\nhost.Logs:\n  {string.Join("\n  ", host.Logs)}";
        Assert.Fail($"{message} (timed out after {timeout.TotalSeconds:0}s){logs}");
    }

    /// <summary>Accepts one connection, does the Hello/Identify handshake (with or without auth), then
    /// answers RefreshAllAsync's startup RequestBatch so the connection reaches "Connected".</summary>
    private static async Task AcceptAndIdentifyAsync(FakeObsConnection conn, string? authSalt = null, string? authChallenge = null, Func<string, JsonObject>? responseDataFor = null, CancellationToken ct = default)
    {
        await conn.SendHelloAsync(authSalt, authChallenge, ct: ct);
        var (op, data) = await conn.ReceiveAsync(ct);
        Assert.Equal(1, op); // Identify

        if (authSalt is not null)
        {
            var expected = ObsAuth.ComputeAuthenticationString("secret", authSalt, authChallenge!);
            Assert.Equal(expected, data["authentication"]?.GetValue<string>());
        }

        await conn.SendIdentifiedAsync(ct);
        await RespondToRefreshAllAsync(conn, ct, responseDataFor);
    }

    /// <summary>
    /// Answers RefreshAllAsync's whole request sequence generically: the initial 7-request RequestBatch,
    /// any number of follow-up RequestBatch calls (e.g. GetInputMute/GetInputVolume probes — sent only
    /// when there are audio inputs), and the trailing single GetSceneTransitionList Request. Every
    /// response succeeds with empty responseData unless <paramref name="responseDataFor"/> overrides it
    /// for a given requestType (e.g. to seed GetInputList with a fake input).
    /// </summary>
    private static async Task RespondToRefreshAllAsync(FakeObsConnection conn, CancellationToken ct, Func<string, JsonObject>? responseDataFor = null)
    {
        responseDataFor ??= static _ => [];
        while (true)
        {
            var (op, data) = await conn.ReceiveAsync(ct);
            switch (op)
            {
                case 8:
                {
                    var requests = data["requests"] as JsonArray ?? [];
                    var results = new JsonArray();
                    foreach (var node in requests)
                    {
                        var item = node as JsonObject;
                        var requestType = item?["requestType"]?.GetValue<string>() ?? "";
                        results.Add(new JsonObject
                        {
                            ["requestType"] = requestType,
                            ["requestId"] = item?["requestId"]?.DeepClone(),
                            ["requestStatus"] = new JsonObject { ["result"] = true, ["code"] = 100 },
                            ["responseData"] = responseDataFor(requestType),
                        });
                    }
                    await conn.SendAsync(9, new JsonObject { ["requestId"] = data["requestId"]?.DeepClone(), ["results"] = results }, ct);
                    break;
                }
                case 6:
                {
                    var requestType = data["requestType"]?.GetValue<string>() ?? "";
                    Assert.Equal("GetSceneTransitionList", requestType); // the one single Request RefreshAllAsync sends
                    await conn.RespondRequestAsync(data["requestId"]!.GetValue<string>(), requestType, success: true, ct: ct);
                    return; // RefreshAllAsync's request sequence always ends here.
                }
                default:
                    Assert.Fail($"unexpected op {op} while answering RefreshAllAsync");
                    return;
            }
        }
    }

    private static (string DataDir, FakePluginHost Host, FakeVariableStore Store) NewFixture(int port, string password = "")
    {
        var dataDir = Directory.CreateTempSubdirectory("obs-plugin-test-").FullName;
        new ObsSettings { Enabled = true, Host = "127.0.0.1", Port = port, Password = password }.Save(dataDir);
        return (dataDir, new FakePluginHost(dataDir), new FakeVariableStore());
    }

    [Fact(Timeout = 10_000)]
    public async Task Connects_without_password_when_server_requires_none()
    {
        await using var server = new FakeObsServer();
        server.Start(async (conn, ct) =>
        {
            await AcceptAndIdentifyAsync(conn, authSalt: null, ct: ct);
            await Task.Delay(Timeout.Infinite, ct);
        });

        var (_, host, store) = NewFixture(server.Port);
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);

        await WaitUntilAsync(() => store.Get("obs.connected") is true, TimeSpan.FromSeconds(5),
            "expected obs.connected to become true", server, host);

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 10_000)]
    public async Task Connects_with_correct_password_hash_when_server_requires_auth()
    {
        await using var server = new FakeObsServer();
        server.Start(async (conn, ct) =>
        {
            await AcceptAndIdentifyAsync(conn, authSalt: "s4lt==", authChallenge: "ch4llenge==", ct: ct);
            await Task.Delay(Timeout.Infinite, ct);
        });

        var (_, host, store) = NewFixture(server.Port, password: "secret");
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);

        await WaitUntilAsync(() => store.Get("obs.connected") is true, TimeSpan.FromSeconds(5),
            "expected obs.connected to become true with a correctly-hashed password");

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 15_000)]
    public async Task Hard_auth_failure_stops_retrying_until_settings_change()
    {
        var attempts = 0;
        await using var server = new FakeObsServer();
        server.Start(async (conn, ct) =>
        {
            Interlocked.Increment(ref attempts);
            await conn.SendHelloAsync(authSalt: "s4lt==", authChallenge: "ch4llenge==", ct: ct);
            await conn.ReceiveAsync(ct); // Identify — content doesn't matter, always reject
            await conn.CloseAsync(4009, "wrong password", ct);
        });

        var (_, host, store) = NewFixture(server.Port, password: "wrong");
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);

        await WaitUntilAsync(() => (string?)store.Get("obs.status") == "OBS · wrong password", TimeSpan.FromSeconds(5),
            "expected AuthFailed status after a 4009 close");

        await Task.Delay(800); // normal backoff would have retried at least once by now if it were still active
        Assert.Equal(1, Volatile.Read(ref attempts));

        connection.NotifySettingsChanged(); // e.g. the user fixed the password in the settings page
        await WaitUntilAsync(() => Volatile.Read(ref attempts) >= 2, TimeSpan.FromSeconds(5),
            "expected an immediate reconnect attempt after NotifySettingsChanged");

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 20_000)]
    public async Task Unresponsive_server_times_out_and_then_reconnects()
    {
        var attempts = 0;
        await using var server = new FakeObsServer();
        server.Start(async (conn, ct) =>
        {
            var attempt = Interlocked.Increment(ref attempts);
            await conn.SendHelloAsync(ct: ct);
            var (op, _) = await conn.ReceiveAsync(ct);
            Assert.Equal(1, op);
            await conn.SendIdentifiedAsync(ct);

            if (attempt == 1)
            {
                // Never answer the startup RequestBatch — ObsClient's 5s request timeout should fire,
                // ObsConnection should log it as a normal failure and reconnect with backoff.
                await conn.ReceiveAsync(ct);
                await Task.Delay(Timeout.Infinite, ct);
            }
            else
            {
                await RespondToRefreshAllAsync(conn, ct);
                await Task.Delay(Timeout.Infinite, ct);
            }
        });

        var (_, host, store) = NewFixture(server.Port);
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);

        await WaitUntilAsync(() => store.Get("obs.connected") is true, TimeSpan.FromSeconds(15),
            "expected the connection to time out on the silent first attempt, then succeed on the retry", server, host);
        Assert.True(Volatile.Read(ref attempts) >= 2);

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 10_000)]
    public async Task Abrupt_drop_reconnects_with_backoff()
    {
        var attempts = 0;
        await using var server = new FakeObsServer();
        server.Start(async (conn, ct) =>
        {
            var attempt = Interlocked.Increment(ref attempts);
            if (attempt == 1)
            {
                await AcceptAndIdentifyAsync(conn, ct: ct);
                await Task.Delay(150, ct); // let ObsConnection observe "Connected" briefly
                conn.Abort(); // no close handshake — simulates a dropped Wi-Fi connection
            }
            else
            {
                await AcceptAndIdentifyAsync(conn, ct: ct);
                await Task.Delay(Timeout.Infinite, ct);
            }
        });

        var (_, host, store) = NewFixture(server.Port);
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);

        await WaitUntilAsync(() => Volatile.Read(ref attempts) >= 2, TimeSpan.FromSeconds(8),
            "expected a second connection attempt after the abrupt drop");
        await WaitUntilAsync(() => store.Get("obs.connected") is true, TimeSpan.FromSeconds(5),
            "expected the reconnect to succeed");

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 10_000)]
    public async Task ExitStarted_event_ends_the_session_promptly_instead_of_waiting_on_a_dead_socket()
    {
        await using var server = new FakeObsServer();
        var serverDone = new TaskCompletionSource();
        server.Start(async (conn, ct) =>
        {
            await AcceptAndIdentifyAsync(conn, ct: ct);
            await conn.SendEventAsync("ExitStarted", ct: ct);
            // Deliberately do NOT close the socket — a real dead TCP connection can sit "open" for a long
            // time before the OS notices. ObsConnection is expected to close its own side immediately on
            // seeing ExitStarted rather than waiting that out.
            serverDone.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
        });

        var (_, host, store) = NewFixture(server.Port);
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);

        // ExitStarted follows the handshake immediately, so obs.connected can flip true -> false between two
        // polls; check the recorded history instead of polling for the transient `true`.
        await WaitUntilAsync(() => store.WasEverSet("obs.connected", true), TimeSpan.FromSeconds(5), "expected initial connect");
        await serverDone.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // The session should end (obs.connected reset to false) well within a couple seconds of
        // ExitStarted, not only after some multi-second TCP-level timeout.
        await WaitUntilAsync(() => store.Get("obs.connected") is false, TimeSpan.FromSeconds(3),
            "expected ExitStarted to end the session promptly");

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 10_000)]
    public async Task Input_removed_event_removes_its_variables_instead_of_leaving_a_stale_value()
    {
        await using var server = new FakeObsServer();
        server.Start(async (conn, ct) =>
        {
            await AcceptAndIdentifyAsync(conn, ct: ct, responseDataFor: requestType => requestType == "GetInputList"
                ? new JsonObject
                {
                    ["inputs"] = new JsonArray
                    {
                        // inputKindCaps bit 0b10 (OBS_SOURCE_AUDIO) marks it audio-capable — see
                        // ObsState.RefreshAllAsync's isAudio computation.
                        new JsonObject { ["inputName"] = "Mic", ["inputKind"] = "wasapi_input_capture", ["inputKindCaps"] = 0b10 },
                    },
                }
                : []);

            await conn.SendEventAsync("InputRemoved", new JsonObject { ["inputName"] = "Mic" }, ct);
            await Task.Delay(Timeout.Infinite, ct);
        });

        var (_, host, store) = NewFixture(server.Port);
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);

        await WaitUntilAsync(() => store.Get("obs.connected") is true, TimeSpan.FromSeconds(5), "expected initial connect", server, host);
        await WaitUntilAsync(() => store.RemovedNames.Contains("obs.input.mic.muted"), TimeSpan.FromSeconds(5),
            "expected obs.input.mic.muted to be removed after InputRemoved", server, host);
        Assert.Contains("obs.input.mic.volumeDb", store.RemovedNames);

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }
}
