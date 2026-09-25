using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Polling: the periodic stream/record/stats batch and its publishing.
public sealed partial class ObsConnection
{
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
                host.Log($"OBS state query failed (probably temporary): {ex.Message}");
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
}
