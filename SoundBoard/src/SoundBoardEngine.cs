using System.Runtime.CompilerServices;
using MacroGrid.Plugin.Abstractions;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

[assembly: InternalsVisibleTo("MacroGrid.Plugin.SoundBoard.Tests")]

namespace MacroGrid.Plugin.SoundBoard;

/// <summary>
/// Owns the plugin's whole audio lifetime: one shared-mode <see cref="WasapiOut"/> on the configured device,
/// one <see cref="MixingSampleProvider"/> (fixed 48 kHz stereo float, <c>ReadFully</c> so the device callback
/// never starves and a source that reads 0 is dropped automatically) behind a master
/// <see cref="VolumeSampleProvider"/>, and every currently playing <see cref="SoundVoice"/>. The output opens
/// on the first <see cref="Play"/> and closes after <see cref="IdleCloseDelay"/> of nothing playing (see
/// <see cref="RunAsync"/>) — a soundboard that is not being used costs nothing.
/// </summary>
public sealed partial class SoundBoardEngine : IVariableProvider, IVariableCatalogSource, IDisposable
{
    private const string Category = "SoundBoard";
    private static readonly WaveFormat MixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
    internal static readonly TimeSpan IdleCloseDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RemainingTickInterval = TimeSpan.FromMilliseconds(250); // ~4 Hz, only while something plays

    private readonly IPluginHost _host;
    private readonly Lock _lock = new();
    private readonly List<SoundVoice> _voices = [];
    private readonly MixingSampleProvider _mixer = new(MixerFormat) { ReadFully = true };
    private readonly VolumeSampleProvider _masterVolume;
    private readonly SemaphoreSlim _activitySignal = new(0, int.MaxValue);
    private readonly IPluginStatusItem _fileStatus;
    private readonly IPluginStatusItem _deviceStatus;

    private WasapiOut? _output;
    private DateTime _lastActivityUtc = DateTime.UtcNow;
    private SoundBoardSettingsData _settings;
    private IVariableStore? _store;

    public SoundBoardEngine(IPluginHost host)
    {
        _host = host;
        _settings = SoundBoardSettingsData.LoadOrCreate(host.DataDirectory);
        _masterVolume = new VolumeSampleProvider(_mixer) { Volume = (float)(_settings.MasterVolume / 100.0) };
        _fileStatus = host.CreateStatusItem("soundboard-files");
        _deviceStatus = host.CreateStatusItem("soundboard-device");
        UpdateFileStatus();
    }

    public SoundBoardSettingsData Settings { get { lock (_lock) return _settings; } }

    /// <summary>Applied by <see cref="SoundBoardSettingsPage.Save"/>. Persists, updates the master and every
    /// currently playing voice's volume/loop live (no restart), reopens the output on a device change, and
    /// removes the variables of any sound row that was deleted.</summary>
    public void ApplySettings(SoundBoardSettingsData next)
    {
        List<string> removedIds;
        var deviceChanged = false;
        lock (_lock)
        {
            var oldIds = _settings.Sounds.Select(s => s.Id).ToHashSet();
            var newIds = next.Sounds.Select(s => s.Id).ToHashSet();
            removedIds = [.. oldIds.Except(newIds)];
            deviceChanged = _settings.OutputDeviceId != next.OutputDeviceId;

            _settings = next;
            _masterVolume.Volume = (float)(next.MasterVolume / 100.0);

            foreach (var voice in _voices)
            {
                var entry = next.Sounds.FirstOrDefault(s => s.Id == voice.SoundId);
                if (entry is null) continue;
                voice.SetVolume(entry.Volume);
                voice.SetLoop(entry.Loop);
            }
        }

        if (deviceChanged) CloseOutput();
        foreach (var id in removedIds) RemoveSoundVariables(id);
        UpdateFileStatus();
        if (_store is { } store) PublishStaticVariables(store);
    }

    /// <summary>Fire-and-forget by design: starts the voice and returns immediately, so a widget press never
    /// waits for playback to finish and the device queue never blocks on it. Throws when the sound is unknown
    /// (a stale binding after a row was deleted logs nothing further — ActionDispatcher already does) or its
    /// file is missing (surfaced to the person as a toast and in the status bar, same as any action error).</summary>
    public void Play(string soundId, ActionContext context, double? volumeOverride = null)
    {
        SoundEntry? entry;
        double fadeInMs;
        lock (_lock)
        {
            entry = _settings.Sounds.FirstOrDefault(s => s.Id == soundId);
            fadeInMs = _settings.FadeInMs;
        }
        if (entry is null) throw new InvalidOperationException($"Unknown sound: {soundId}");
        if (entry.File.Length == 0 || !File.Exists(entry.File))
            throw new InvalidOperationException($"Sound file not found: {entry.File} — pick it again in the settings.");

        EnsureOutputOpen();

        var voice = new SoundVoice(entry.File, volumeOverride ?? entry.Volume, entry.Loop, _mixer.WaveFormat,
            soundId, context.DeviceId, context.PageId, context.WidgetId, isPreview: false);
        voice.BeginFadeIn((int)fadeInMs);

        lock (_lock) _voices.Add(voice);
        _mixer.AddMixerInput(voice);
        TouchActivity();
        WakeLoop();

        var name = entry.Name.Length > 0 ? entry.Name : Path.GetFileNameWithoutExtension(entry.File);
        _store?.Set("soundboard.nowPlaying", name);
        _store?.Set($"soundboard.{soundId}.playing", true);
    }

    public bool IsPlaying(string soundId)
    {
        lock (_lock) return _voices.Any(v => !v.IsPreview && v.SoundId == soundId);
    }

    /// <summary>Test seam: adds a voice straight to the mixer without opening a real output device, so
    /// Stop/StopAll/IsPlaying and the fade-removal timing can be exercised "by hand" (see
    /// MacroGrid.Plugin.SoundBoard.Tests) without a WASAPI device being available.</summary>
    internal void AddVoiceForTesting(SoundVoice voice)
    {
        lock (_lock) _voices.Add(voice);
        _mixer.AddMixerInput(voice);
    }

    internal IReadOnlyList<SoundVoice> VoicesForTesting { get { lock (_lock) return [.. _voices]; } }

    /// <summary>"default" resolves to the settings' own <see cref="SoundBoardSettingsData.StopStyle"/>; anything
    /// else (immediate/fade) overrides it for this stop only.</summary>
    public void Stop(VoiceFilter filter, string style)
    {
        List<SoundVoice> matched;
        lock (_lock) matched = [.. _voices.Where(filter.Matches)];
        foreach (var voice in matched) StopVoice(voice, style);
    }

    public void StopAll(string style) => Stop(new VoiceFilter(IsPreview: false), style);

    private void StopVoice(SoundVoice voice, string style)
    {
        int fadeOutMs;
        lock (_lock) fadeOutMs = _settings.FadeOutMs;
        var effectiveStyle = style == "default" ? Settings.StopStyle : style;

        voice.BeginStop(effectiveStyle, fadeOutMs);
        if (effectiveStyle == "fade" && fadeOutMs > 0)
            _ = RemoveAfterDelayAsync(voice, fadeOutMs);
        else
            RemoveVoice(voice);
    }

    private async Task RemoveAfterDelayAsync(SoundVoice voice, int delayMs)
    {
        try { await Task.Delay(delayMs); }
        catch (OperationCanceledException) { }
        RemoveVoice(voice);
    }

    private void RemoveVoice(SoundVoice voice)
    {
        bool removed;
        lock (_lock) removed = _voices.Remove(voice);
        if (!removed) return; // already reaped (e.g. it had already finished on its own)

        try { _mixer.RemoveMixerInput(voice); } catch (ArgumentException) { /* already gone from the mixer */ }
        voice.Dispose();
        if (!voice.IsPreview) _store?.Set($"soundboard.{voice.SoundId}.playing", IsPlaying(voice.SoundId));
    }

    /// <summary>Starts (or, if one is already playing, stops) a one-off preview voice for the settings
    /// window's Preview button — never persisted, never counted by "soundboard.*" variables or <see cref="Stop"/>
    /// with a sound-scoped filter, and always at most one at a time.</summary>
    public string? TogglePreview(string file, double volumePercent)
    {
        lock (_lock)
        {
            var existing = _voices.FirstOrDefault(v => v.IsPreview);
            if (existing is not null)
            {
                _voices.Remove(existing);
                try { _mixer.RemoveMixerInput(existing); } catch (ArgumentException) { }
                existing.Dispose();
                return null;
            }
        }

        EnsureOutputOpen();
        var voice = new SoundVoice(file, volumePercent, loop: false, _mixer.WaveFormat, "", "", "", "", isPreview: true);
        lock (_lock) _voices.Add(voice);
        _mixer.AddMixerInput(voice);
        TouchActivity();
        WakeLoop();
        return null;
    }

    public void SetMasterVolume(double percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        lock (_lock)
        {
            _settings.MasterVolume = percent;
            _masterVolume.Volume = (float)(percent / 100.0);
            _settings.Save(_host.DataDirectory);
        }
        _store?.Set("soundboard.masterVolume", percent);
    }

    public double GetMasterVolume() { lock (_lock) return _settings.MasterVolume; }

    public OptionsResult GetDeviceOptions()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var options = new List<SettingOption> { new("", "Default device") };
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                options.Add(new SettingOption(device.ID, device.FriendlyName));
            return new OptionsResult(options);
        }
        catch (Exception ex)
        {
            return new OptionsResult([], ex.Message);
        }
    }

    /// <summary>Opening WASAPI can fail for reasons outside anyone's control (no render device at all, the
    /// configured one just got unplugged, it's held exclusively by another app, ...); that must never turn a
    /// button press into an unhandled exception. On failure this only reports it through
    /// <see cref="_deviceStatus"/> and leaves <see cref="_output"/> null (retried on the next Play) — voice
    /// bookkeeping (overlap/cut, toggle, hold-release, the soundboard.* variables) proceeds either way, so nothing
    /// but the actual sound is missing until a device is available again.</summary>
    private void EnsureOutputOpen()
    {
        lock (_lock)
        {
            if (_output is not null) return;
            try
            {
                var output = OpenOutput(_settings.OutputDeviceId, out var usedFallback);
                output.Init(_masterVolume);
                output.Play();
                _output = output;
                _deviceStatus.Update(
                    usedFallback ? "Output device not found, using the default" : "Output ready",
                    usedFallback ? StatusLevel.Warning : StatusLevel.Ok);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _deviceStatus.Update($"No audio output available: {ex.Message}", StatusLevel.Error);
            }
        }
    }

    private static WasapiOut OpenOutput(string? deviceId, out bool usedFallback)
    {
        usedFallback = false;
        if (string.IsNullOrEmpty(deviceId)) return new WasapiOut(AudioClientShareMode.Shared, 200);

        using var enumerator = new MMDeviceEnumerator();
        MMDevice? device = null;
        try { device = enumerator.GetDevice(deviceId); }
        catch (Exception) { /* removed/disabled device — fall back below */ }

        if (device is null || device.State != DeviceState.Active)
        {
            usedFallback = true;
            return new WasapiOut(AudioClientShareMode.Shared, 200);
        }
        return new WasapiOut(device, AudioClientShareMode.Shared, true, 200);
    }

    private void CloseOutput()
    {
        WasapiOut? output;
        lock (_lock) { output = _output; _output = null; }
        if (output is null) return;
        try { output.Stop(); } catch (Exception) { }
        output.Dispose();
    }

    private void TouchActivity() => _lastActivityUtc = DateTime.UtcNow;

    /// <summary>Nudges <see cref="RunAsync"/>'s idle wait so a fresh Play()/preview is picked up within
    /// milliseconds instead of up to its 5 s idle poll. Safe to over-release: the loop only ever awaits one
    /// permit at a time and a stray extra one just shortens the next idle wait by nothing that matters.</summary>
    private void WakeLoop()
    {
        if (_activitySignal.CurrentCount == 0) _activitySignal.Release();
    }

    private void UpdateFileStatus()
    {
        SoundBoardSettingsData settings;
        lock (_lock) settings = _settings;
        var missing = settings.Sounds.Count(s => s.File.Length == 0 || !File.Exists(s.File));
        _fileStatus.Update(
            missing == 0 ? "All sound files found" : $"{missing} sound file(s) missing",
            missing == 0 ? StatusLevel.Ok : StatusLevel.Warning);
    }

    public void Dispose()
    {
        List<SoundVoice> voices;
        lock (_lock) { voices = [.. _voices]; _voices.Clear(); }
        foreach (var voice in voices)
        {
            try { _mixer.RemoveMixerInput(voice); } catch (ArgumentException) { }
            voice.Dispose();
        }
        CloseOutput();
    }
}
