using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Sound;

// Variables: the catalog, the RunAsync loop (event-driven except "remaining", which only polls while
// something plays), and cleanup on a settings change.
public sealed partial class SoundEngine
{
    public IEnumerable<VariableInfo> Describe()
    {
        yield return new("sound.nowPlaying", "Name of the most recently started sound", "{sound.nowPlaying}", Category);
        yield return new("sound.masterVolume", "Master volume (%)", "{sound.masterVolume|0}", Category) { Type = VariableType.Number, Unit = "%" };

        SoundSettingsData settings;
        lock (_lock) settings = _settings;
        foreach (var entry in settings.Sounds)
        {
            var label = entry.Name.Length > 0 ? entry.Name : Path.GetFileNameWithoutExtension(entry.File);
            yield return new($"sound.{entry.Id}.name", $"{label} — name", $"{{sound.{entry.Id}.name}}", Category);
            yield return new($"sound.{entry.Id}.playing", $"{label} — playing", $"{{sound.{entry.Id}.playing}}", Category) { Type = VariableType.Boolean };
            yield return new($"sound.{entry.Id}.remaining", $"{label} — remaining", $"{{sound.{entry.Id}.remaining}}", Category) { Type = VariableType.Duration };
            yield return new($"sound.{entry.Id}.duration", $"{label} — duration", $"{{sound.{entry.Id}.duration}}", Category) { Type = VariableType.Duration };
        }
    }

    public async Task RunAsync(IVariableStore store, CancellationToken cancellationToken)
    {
        _store = store;
        PublishStaticVariables(store);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                bool anyPlaying;
                lock (_lock) anyPlaying = _voices.Count > 0;

                if (anyPlaying)
                {
                    TickPlaying(store);
                    await Task.Delay(RemainingTickInterval, cancellationToken);
                }
                else
                {
                    if (_output is not null && DateTime.UtcNow - _lastActivityUtc >= IdleCloseDelay)
                        CloseOutput();

                    // Nothing to poll while idle — wait for the next Play()/preview (or wake periodically
                    // anyway, so the idle-close check above still runs even without new activity).
                    try { await _activitySignal.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken); }
                    catch (OperationCanceledException) { throw; }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Plugin unloading — normal shutdown of the provider loop.
        }
        finally
        {
            _store = null;
        }
    }

    private void TickPlaying(IVariableStore store)
    {
        List<SoundVoice> finished;
        List<SoundVoice> active;
        lock (_lock)
        {
            finished = [.. _voices.Where(v => v.Finished)];
            foreach (var voice in finished) _voices.Remove(voice);
            active = [.. _voices];
        }
        foreach (var voice in finished)
        {
            try { _mixer.RemoveMixerInput(voice); } catch (ArgumentException) { }
            voice.Dispose();
        }

        var playing = active.Where(v => !v.IsPreview).ToLookup(v => v.SoundId);
        SoundSettingsData settings;
        lock (_lock) settings = _settings;
        foreach (var entry in settings.Sounds)
        {
            var voice = playing[entry.Id].FirstOrDefault();
            store.Set($"sound.{entry.Id}.playing", voice is not null);
            if (voice is null) continue;

            var remaining = voice.Duration - voice.Position;
            store.Set($"sound.{entry.Id}.remaining", remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining);
        }
    }

    private void PublishStaticVariables(IVariableStore store)
    {
        SoundSettingsData settings;
        lock (_lock) settings = _settings;
        store.Set("sound.masterVolume", settings.MasterVolume);
        foreach (var entry in settings.Sounds)
        {
            store.Set($"sound.{entry.Id}.name", entry.Name.Length > 0 ? entry.Name : Path.GetFileNameWithoutExtension(entry.File));
            store.Set($"sound.{entry.Id}.duration", TryGetDuration(entry.File));
        }
    }

    private static TimeSpan TryGetDuration(string file)
    {
        if (file.Length == 0 || !File.Exists(file)) return TimeSpan.Zero;
        try
        {
            using var reader = new NAudio.Wave.AudioFileReader(file);
            return reader.TotalTime;
        }
        catch (Exception)
        {
            return TimeSpan.Zero;
        }
    }

    private void RemoveSoundVariables(string soundId)
    {
        _store?.Remove($"sound.{soundId}.name");
        _store?.Remove($"sound.{soundId}.playing");
        _store?.Remove($"sound.{soundId}.remaining");
        _store?.Remove($"sound.{soundId}.duration");
    }
}
