using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs.Tests;

/// <summary>The polled stream/record/stats variables are written only while <see cref="IPluginHost.Demand"/> says something
/// observes them, and a demand change makes the next poll run at once.</summary>
public sealed class ObsDemandTests
{
    private sealed class FakeDemand : IVariableDemand
    {
        private readonly HashSet<string> _prefixes = [];
        private readonly Lock _lock = new();

        public bool IsObserved(string name) { lock (_lock) return _prefixes.Any(name.StartsWith); }

        public bool IsPrefixObserved(string prefix) { lock (_lock) return _prefixes.Any(p => p.StartsWith(prefix) || prefix.StartsWith(p)); }

        public event Action? Changed;

        public void Observe(string prefix)
        {
            lock (_lock) _prefixes.Add(prefix);
            Changed?.Invoke();
        }

        public void Raise() => Changed?.Invoke();
    }

    private sealed class DemandHost(FakePluginHost inner, IVariableDemand demand) : IPluginHost
    {
        public string ServerVersion => inner.ServerVersion;
        public string SdkVersion => inner.SdkVersion;
        public string DataDirectory => inner.DataDirectory;
        public IPluginSecrets Secrets => inner.Secrets;
        public IPluginWidgets Widgets => inner.Widgets;
        public IVariableDemand Demand => demand;
        public void Log(string message) => inner.Log(message);
        public void RegisterAction(IActionHandler handler) => inner.RegisterAction(handler);
        public void RegisterVariableProvider(IVariableProvider provider) => inner.RegisterVariableProvider(provider);
        public void RegisterSettingsPage(IPluginSettingsPage page) => inner.RegisterSettingsPage(page);
        public IPluginStatusItem CreateStatusItem(string id) => inner.CreateStatusItem(id);
        public void RegisterIconPack(IIconPackSource iconPack) => inner.RegisterIconPack(iconPack);
    }

    private static JsonObject PollData(string requestType) => requestType switch
    {
        "GetStreamStatus" => new JsonObject { ["outputActive"] = false, ["outputBytes"] = 5000.0, ["outputTimecode"] = "00:00:07.000" },
        "GetStats" => new JsonObject { ["activeFps"] = 30.0, ["cpuUsage"] = 12.0 },
        _ => [],
    };

    /// <summary>Connects, then answers every later batch with <see cref="PollData"/> and counts them.</summary>
    private static async Task<(ObsConnection Connection, FakeVariableStore Store, FakeDemand Demand, Func<int> Batches, CancellationTokenSource Cts, Task Run)> StartAsync(
        FakeObsServer server, FakeDemand demand)
    {
        var batches = 0;
        server.Start(async (conn, ct) =>
        {
            await ObsConnectionTests.AcceptAndIdentifyAsync(conn, responseDataFor: PollData, ct: ct);
            while (true)
            {
                var (op, data) = await conn.ReceiveAsync(ct);
                if (op != 8) continue;
                Interlocked.Increment(ref batches);
                var results = new JsonArray();
                foreach (var node in (JsonArray)data["requests"]!)
                {
                    var type = node!["requestType"]!.GetValue<string>();
                    results.Add(new JsonObject
                    {
                        ["requestType"] = type,
                        ["requestId"] = node["requestId"]?.DeepClone(),
                        ["requestStatus"] = new JsonObject { ["result"] = true, ["code"] = 100 },
                        ["responseData"] = PollData(type),
                    });
                }
                await conn.SendAsync(9, new JsonObject { ["requestId"] = data["requestId"]?.DeepClone(), ["results"] = results }, ct);
            }
        });

        var (_, fakeHost, store) = ObsConnectionTests.NewFixture(server.Port);
        var connection = new ObsConnection(new DemandHost(fakeHost, demand));
        var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);
        await ObsConnectionTests.WaitUntilAsync(() => store.Get("obs.connected") is true, TimeSpan.FromSeconds(5), "expected connect", server, fakeHost);
        // the tick loop subscribes to Changed right after the state turns Connected
        await Task.Delay(200);
        return (connection, store, demand, () => Volatile.Read(ref batches), cts, run);
    }

    [Fact(Timeout = 15_000)]
    public async Task Unobserved_polls_still_publish_streaming_flag_but_nothing_else()
    {
        await using var server = new FakeObsServer();
        var (_, store, demand, batches, cts, run) = await StartAsync(server, new FakeDemand());

        // reaching a batch needs a full session; the first poll is forced by the demand signal
        demand.Raise();
        await ObsConnectionTests.WaitUntilAsync(() => batches() >= 1 && store.Get("obs.streaming") is false, TimeSpan.FromSeconds(5), "expected a poll");
        await Task.Delay(200);

        Assert.Null(store.Get("obs.stream.bytes"));
        Assert.Null(store.Get("obs.stream.timecode"));
        Assert.Null(store.Get("obs.stats.fps"));
        Assert.Null(store.Get("obs.ws.in"));
        Assert.Equal("OBS · connected", (string?)store.Get("obs.status")); // no one observes obs.status, so the poll leaves it

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 15_000)]
    public async Task Demand_growing_publishes_only_the_observed_group_at_once()
    {
        await using var server = new FakeObsServer();
        var (_, store, demand, _, cts, run) = await StartAsync(server, new FakeDemand());

        demand.Observe("obs.stats.");
        await ObsConnectionTests.WaitUntilAsync(() => store.Get("obs.stats.fps") is 30.0, TimeSpan.FromSeconds(5), "expected stats to be published right after demand grew");

        Assert.Null(store.Get("obs.stream.bytes"));
        Assert.Null(store.Get("obs.stream.kbps"));

        demand.Observe("obs.stream.");
        await ObsConnectionTests.WaitUntilAsync(() => store.Get("obs.stream.bytes") is 5000.0, TimeSpan.FromSeconds(5), "expected stream group after it became observed");
        // first write after a stretch without details must not report a bitrate spike
        Assert.Equal(0.0, store.Get("obs.stream.kbps"));

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 15_000)]
    public async Task Default_host_demand_publishes_everything()
    {
        await using var server = new FakeObsServer();
        var batches = 0;
        server.Start(async (conn, ct) =>
        {
            await ObsConnectionTests.AcceptAndIdentifyAsync(conn, ct: ct);
            while (true)
            {
                var (op, data) = await conn.ReceiveAsync(ct);
                if (op != 8) continue;
                Interlocked.Increment(ref batches);
                await conn.RespondBatchAsync(data["requestId"]!.GetValue<string>(), (JsonArray)data["requests"]!, ct);
            }
        });
        var (_, host, store) = ObsConnectionTests.NewFixture(server.Port);
        var connection = new ObsConnection(host); // FakePluginHost uses the default NullVariableDemand
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);

        await ObsConnectionTests.WaitUntilAsync(() => store.Get("obs.ws.in") is not null && store.Get("obs.stats.fps") is not null, TimeSpan.FromSeconds(9),
            "expected the first 5 s poll to publish every group", server, host);
        Assert.NotNull(store.Get("obs.stream.bytes"));
        Assert.NotNull(store.Get("obs.record.bytes"));

        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }
}
