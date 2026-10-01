using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;
using static MacroGrid.Plugin.Obs.Tests.ObsConnectionTests;

namespace MacroGrid.Plugin.Obs.Tests;

/// <summary>What an OBS action reports when it cannot do its job: a code the host can show, not just an exception.</summary>
public sealed class ObsActionOutcomeTests
{
    private static readonly ActionContext Context = new("d", "p", "w", null!);

    private static Task<ActionOutcome> Run(ObsActionBase action, JsonObject? settings = null) =>
        action.ExecuteWithOutcomeAsync(Context, settings ?? [], CancellationToken.None);

    [Fact]
    public async Task Not_connected_is_reported_as_not_connected()
    {
        var host = new FakePluginHost(Directory.CreateTempSubdirectory("obs-plugin-test-").FullName);

        var outcome = await Run(new ObsStartStreamAction(new ObsConnection(host)));

        Assert.Equal(ActionFailureCode.NotConnected, outcome.Code);
        Assert.Equal("Not connected to OBS.", outcome.Message);
    }

    [Fact]
    public async Task An_empty_required_setting_is_reported_as_not_configured()
    {
        var host = new FakePluginHost(Directory.CreateTempSubdirectory("obs-plugin-test-").FullName);
        var obs = new ObsConnection(host);

        Assert.Equal(ActionFailureCode.NotConfigured, (await Run(new ObsSetSceneAction(obs))).Code);
        Assert.Equal(ActionFailureCode.NotConfigured, (await Run(new ObsToggleMuteAction(obs))).Code);
    }

    [Fact]
    public async Task A_scene_that_is_not_in_obs_is_reported_as_not_found()
    {
        var host = new FakePluginHost(Directory.CreateTempSubdirectory("obs-plugin-test-").FullName);

        var outcome = await Run(new ObsSetSceneAction(new ObsConnection(host)), ObsSetSceneAction.Settings("Gone"));

        Assert.Equal(ActionFailureCode.NotFound, outcome.Code);
        Assert.Contains("Gone", outcome.Message);
    }

    [Fact(Timeout = 10_000)]
    public async Task A_request_obs_refuses_is_reported_as_rejected_with_obs_comment()
    {
        await using var server = new FakeObsServer();
        server.Start(async (conn, ct) =>
        {
            await AcceptAndIdentifyAsync(conn, ct: ct);
            var (_, data) = await conn.ReceiveAsync(ct);
            await conn.RespondRequestAsync(data["requestId"]!.GetValue<string>(), "StartStream", success: false, code: 500, comment: "Output is busy.", ct: ct);
            await Task.Delay(Timeout.Infinite, ct);
        });
        var (_, host, store) = NewFixture(server.Port);
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);
        await WaitUntilAsync(() => store.Get("obs.connected") is true, TimeSpan.FromSeconds(5), "expected connect", server, host);

        var outcome = await Run(new ObsStartStreamAction(connection));

        Assert.Equal(ActionFailureCode.ProviderRejected, outcome.Code);
        Assert.Equal("Output is busy.", outcome.Message);
        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }

    [Fact(Timeout = 15_000)]
    public async Task A_request_obs_never_answers_is_reported_as_a_timeout()
    {
        await using var server = new FakeObsServer();
        server.Start(async (conn, ct) =>
        {
            await AcceptAndIdentifyAsync(conn, ct: ct);
            await conn.ReceiveAsync(ct); // the request: never answered
            await Task.Delay(Timeout.Infinite, ct);
        });
        var (_, host, store) = NewFixture(server.Port);
        var connection = new ObsConnection(host);
        using var cts = new CancellationTokenSource();
        var run = connection.RunAsync(store, cts.Token);
        await WaitUntilAsync(() => store.Get("obs.connected") is true, TimeSpan.FromSeconds(5), "expected connect", server, host);

        var outcome = await Run(new ObsStartStreamAction(connection));

        Assert.Equal(ActionFailureCode.Timeout, outcome.Code);
        cts.Cancel();
        await Task.WhenAny(run, Task.Delay(2000));
    }
}
