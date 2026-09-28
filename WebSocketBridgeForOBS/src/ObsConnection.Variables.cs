using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

// Variables: the catalog (Describe), publishing from the cache, and removal on disconnect.
public sealed partial class ObsConnection
{
    public IEnumerable<VariableInfo> Describe()
    {
        yield return new("obs.connected", "Connected to OBS", "{obs.connected}", Category);
        yield return new("obs.status", "Connection status text", "{obs.status}", Category);
        yield return new("obs.ws.in", "Messages received this session", "{obs.ws.in}", Category);
        yield return new("obs.ws.out", "Messages sent this session", "{obs.ws.out}", Category);

        yield return new("obs.scene.current", "Active program scene", "{obs.scene.current}", Category);
        yield return new("obs.scene.preview", "Preview scene (studio mode)", "{obs.scene.preview}", Category);
        yield return new("obs.studioMode", "Studio mode is on", "{obs.studioMode}", Category);
        yield return new("obs.transition.current", "Active transition", "{obs.transition.current}", Category);
        yield return new("obs.profile.current", "Active profile", "{obs.profile.current}", Category);
        yield return new("obs.sceneCollection.current", "Active scene collection", "{obs.sceneCollection.current}", Category);

        yield return new("obs.streaming", "Streaming is on", "{obs.streaming}", Category);
        yield return new("obs.stream.reconnecting", "Stream is reconnecting", "{obs.stream.reconnecting}", Category);
        yield return new("obs.stream.duration", "Stream duration", "{obs.stream.duration}", Category);
        yield return new("obs.stream.timecode", "Stream timecode", "{obs.stream.timecode}", Category);
        yield return new("obs.stream.congestion", "Stream congestion (%)", "{obs.stream.congestion|0}%", Category);
        yield return new("obs.stream.bytes", "Bytes sent", "{obs.stream.bytes}", Category);
        yield return new("obs.stream.kbps", "Stream bitrate (kbps)", "{obs.stream.kbps|0}", Category);
        yield return new("obs.stream.frames.dropped", "Dropped frames", "{obs.stream.frames.dropped}", Category);
        yield return new("obs.stream.frames.total", "Total frames", "{obs.stream.frames.total}", Category);
        yield return new("obs.stream.frames.droppedPercent", "Dropped frames (%)", "{obs.stream.frames.droppedPercent|1}%", Category);

        yield return new("obs.recording", "Recording is on", "{obs.recording}", Category);
        yield return new("obs.record.paused", "Recording is paused", "{obs.record.paused}", Category);
        yield return new("obs.record.duration", "Recording duration", "{obs.record.duration}", Category);
        yield return new("obs.record.timecode", "Recording timecode", "{obs.record.timecode}", Category);
        yield return new("obs.record.bytes", "Bytes recorded", "{obs.record.bytes}", Category);
        yield return new("obs.record.kbps", "Recording bitrate (kbps)", "{obs.record.kbps|0}", Category);

        yield return new("obs.virtualcam", "Virtual camera is on", "{obs.virtualcam}", Category);
        yield return new("obs.replayBuffer", "Replay buffer is on", "{obs.replayBuffer}", Category);

        yield return new("obs.stats.fps", "OBS render FPS", "{obs.stats.fps|0}", Category);
        yield return new("obs.stats.cpu", "OBS CPU usage (%)", "{obs.stats.cpu|0}%", Category);
        yield return new("obs.stats.memory", "Memory usage (MB)", "{obs.stats.memory|0}", Category);
        yield return new("obs.stats.disk", "Free disk (MB)", "{obs.stats.disk|0}", Category);
        yield return new("obs.stats.renderTime", "Average render time (ms)", "{obs.stats.renderTime|1}", Category);
        yield return new("obs.stats.render.skipped", "Skipped render frames", "{obs.stats.render.skipped}", Category);
        yield return new("obs.stats.render.total", "Total render frames", "{obs.stats.render.total}", Category);
        yield return new("obs.stats.render.skippedPercent", "Skipped render frames (%)", "{obs.stats.render.skippedPercent|1}%", Category);
        yield return new("obs.stats.output.skipped", "Skipped output frames", "{obs.stats.output.skipped}", Category);
        yield return new("obs.stats.output.total", "Total output frames", "{obs.stats.output.total}", Category);
        yield return new("obs.stats.output.skippedPercent", "Skipped output frames (%)", "{obs.stats.output.skippedPercent|1}%", Category);

        foreach (var input in _cache.AudioInputNames)
        {
            var slug = Slug(input);
            yield return new($"obs.input.{slug}.muted", $"{input} — muted", $"{{obs.input.{slug}.muted}}", Category);
            yield return new($"obs.input.{slug}.volumeDb", $"{input} — volume (dB)", $"{{obs.input.{slug}.volumeDb|1}}", Category);
        }
        foreach (var scene in _cache.Scenes)
        {
            foreach (var item in _cache.SceneItems(scene))
            {
                var slug = $"{Slug(scene)}.{Slug(item.SourceName)}";
                yield return new($"obs.item.{slug}.visible", $"{scene} › {item.SourceName} — visible", $"{{obs.item.{slug}.visible}}", Category);
            }
        }
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
}
