using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Polling: the periodic stream/record/stats batch and its publishing.
public sealed partial class ObsConnection
{
    private static readonly TimeSpan UnobservedInterval = TimeSpan.FromSeconds(15);

    /// <summary>Batches GetStreamStatus+GetRecordStatus+GetStats into a single RequestBatch frame every
    /// tick: 1s while streaming or recording (the status text shows the duration live), 5s otherwise, 15s while no
    /// page, rule or widget observes any of the polled variables (<see cref="IPluginHost.Demand"/>). Scene/input/
    /// profile/etc. come only from events (see HandleEvent), never polled. A group whose variables nobody observes is
    /// not written to the store (every write makes the server rescan widgets); the status text is still fed from
    /// the batch. <c>obs.streaming</c> and <c>obs.recording</c> are always written: actions and this loop read them.
    /// When demand grows the host raises <see cref="IVariableDemand.Changed"/> and the next batch runs at once.</summary>
    private async Task RunTickLoopAsync(ObsClient client, IVariableStore store, CancellationToken cancellationToken)
    {
        while (_demandSignal.Wait(0)) { }
        Action onDemandChanged = SignalDemandChanged;
        var demand = host.Demand;
        demand.Changed += onDemandChanged;
        try
        {
            var streamPublished = true;
            var recordPublished = true;
            double fps = 0;
            TimeSpan? streamDuration = null, recordDuration = null;
            while (true)
            {
                var streaming = (bool?)store.Get("obs.streaming") ?? false;
                var recording = (bool?)store.Get("obs.recording") ?? false;
                var observesAny = demand.IsPrefixObserved("obs.stream.") || demand.IsPrefixObserved("obs.record.")
                    || demand.IsPrefixObserved("obs.stats.") || demand.IsObserved("obs.status") || demand.IsPrefixObserved("obs.ws.");
                var interval = streaming || recording ? TimeSpan.FromSeconds(1) : observesAny ? TimeSpan.FromSeconds(5) : UnobservedInterval;

                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var tick = _demandSignal.WaitAsync(interval, waitCts.Token);
                var completed = await Task.WhenAny(tick, client.Completion);
                waitCts.Cancel();
                if (completed == client.Completion) return;
                if (cancellationToken.IsCancellationRequested) return;

                try
                {
                    var publishStream = demand.IsPrefixObserved("obs.stream.");
                    var publishRecord = demand.IsPrefixObserved("obs.record.");
                    var publishStats = demand.IsPrefixObserved("obs.stats.");
                    var results = await client.RequestBatchAsync(
                        [("GetStreamStatus", null), ("GetRecordStatus", null), ("GetStats", null)], cancellationToken);
                    foreach (var result in results)
                    {
                        if (!result.Success) continue;
                        switch (result.RequestType)
                        {
                            case "GetStreamStatus":
                                PublishStreamStatus(store, result.ResponseData, publishStream, rebase: publishStream && !streamPublished);
                                streamPublished = publishStream;
                                streamDuration = TimeSpan.FromMilliseconds(result.ResponseData.TryGetDouble("outputDuration"));
                                break;
                            case "GetRecordStatus":
                                PublishRecordStatus(store, result.ResponseData, publishRecord, rebase: publishRecord && !recordPublished);
                                recordPublished = publishRecord;
                                recordDuration = TimeSpan.FromMilliseconds(result.ResponseData.TryGetDouble("outputDuration"));
                                break;
                            case "GetStats":
                                fps = result.ResponseData.TryGetDouble("activeFps");
                                if (publishStats) PublishStats(store, result.ResponseData);
                                break;
                        }
                    }
                    if (demand.IsObserved("obs.ws.in")) store.Set("obs.ws.in", client.FramesReceived);
                    if (demand.IsObserved("obs.ws.out")) store.Set("obs.ws.out", client.FramesSent);
                    UpdateConnectedStatusText(streaming, recording, store, fps, streamDuration, recordDuration);
                }
                catch (Exception ex) when (ex is ObsRequestException or TimeoutException or IOException)
                {
                    host.Log($"OBS state query failed (probably temporary): {ex.Message}");
                }
            }
        }
        finally
        {
            demand.Changed -= onDemandChanged;
        }
    }

    private void SignalDemandChanged()
    {
        try { _demandSignal.Release(); }
        catch (SemaphoreFullException) { } // a wake-up is already pending.
        catch (ObjectDisposedException) { }
    }

    /// <summary><paramref name="publishDetails"/> false writes only <c>obs.streaming</c>. <paramref name="rebase"/> is the first write after
    /// a stretch without details: the old byte count is stale, so the first bitrate is 0 instead of a spike.</summary>
    private static void PublishStreamStatus(IVariableStore store, JsonObject data, bool publishDetails = true, bool rebase = false)
    {
        store.Set("obs.streaming", data.TryGetBool("outputActive"));
        if (!publishDetails) return;
        var bytes = data.TryGetDouble("outputBytes");
        var prevBytes = rebase ? bytes : (double?)store.Get("obs.stream.bytes") ?? bytes;
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

    private static void PublishRecordStatus(IVariableStore store, JsonObject data, bool publishDetails = true, bool rebase = false)
    {
        store.Set("obs.recording", data.TryGetBool("outputActive"));
        if (!publishDetails) return;
        var bytes = data.TryGetDouble("outputBytes");
        var prevBytes = rebase ? bytes : (double?)store.Get("obs.record.bytes") ?? bytes;
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
}
