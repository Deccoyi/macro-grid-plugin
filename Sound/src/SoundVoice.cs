using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MacroGrid.Plugin.Sound;

/// <summary>Rewinds the underlying reader instead of ending, while <see cref="Loop"/> is on. A natural end
/// with <see cref="Loop"/> off returns 0, same as any other finished ISampleProvider — that is what lets
/// <see cref="SoundEngine"/>'s mixer (<c>ReadFully</c>) notice and drop the voice on its own.</summary>
internal sealed class LoopingSampleProvider(AudioFileReader reader) : ISampleProvider
{
    public bool Loop { get; set; }

    public WaveFormat WaveFormat => reader.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var read = reader.Read(buffer, offset, count);
        if (read == 0 && Loop)
        {
            reader.Position = 0;
            read = reader.Read(buffer, offset, count);
        }
        return read;
    }
}

/// <summary>One playing (or fading out) instance of a sound. Doubles as the ISampleProvider added straight
/// to <see cref="SoundEngine"/>'s mixer: file reader (own volume via <see cref="AudioFileReader.Volume"/>) →
/// loop wrapper → fade provider → resampled/channel-converted to the mixer's fixed format. Tagged with
/// (soundId, deviceId, pageId, widgetId) so <c>sound.stop</c>/hold-release can target the right voices.</summary>
internal sealed class SoundVoice : ISampleProvider, IDisposable
{
    private readonly AudioFileReader _reader;
    private readonly LoopingSampleProvider _loop;
    private readonly FadeInOutSampleProvider _fade;
    private readonly ISampleProvider _output;

    public string SoundId { get; }
    public string DeviceId { get; }
    public string PageId { get; }
    public string WidgetId { get; }
    public bool IsPreview { get; }

    /// <summary>True once the chain has read 0 samples (file ended without looping, or an immediate stop
    /// forced it) — <see cref="SoundEngine"/> reaps it from <see cref="_voices"/> and disposes it on the
    /// next tick. Not observed by the mixer itself; that happens independently via its own ReadFully logic.</summary>
    public bool Finished { get; private set; }

    public WaveFormat WaveFormat => _output.WaveFormat;
    public TimeSpan Duration => _reader.TotalTime;
    public TimeSpan Position => _reader.CurrentTime;

    public SoundVoice(string path, double volumePercent, bool loop, WaveFormat mixerFormat,
        string soundId, string deviceId, string pageId, string widgetId, bool isPreview)
    {
        SoundId = soundId;
        DeviceId = deviceId;
        PageId = pageId;
        WidgetId = widgetId;
        IsPreview = isPreview;

        _reader = new AudioFileReader(path) { Volume = ToLinear(volumePercent) };
        _loop = new LoopingSampleProvider(_reader) { Loop = loop };
        _fade = new FadeInOutSampleProvider(_loop, initiallySilent: false);
        _output = ToMixerFormat(_fade, mixerFormat);
    }

    public void SetVolume(double percent) => _reader.Volume = ToLinear(percent);

    public void SetLoop(bool loop) => _loop.Loop = loop;

    public void BeginFadeIn(int milliseconds)
    {
        if (milliseconds > 0) _fade.BeginFadeIn(milliseconds);
    }

    /// <summary>"fade" fades to silence over <paramref name="fadeOutMs"/> and lets the caller reap the voice
    /// after that delay; anything else (including an unknown style) stops the loop and reads as finished
    /// right away, so the caller can remove it from the mixer immediately.</summary>
    public void BeginStop(string style, int fadeOutMs)
    {
        if (style == "fade" && fadeOutMs > 0)
        {
            _fade.BeginFadeOut(fadeOutMs);
        }
        else
        {
            _loop.Loop = false;
            Finished = true;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var read = _output.Read(buffer, offset, count);
        if (read == 0) Finished = true;
        return read;
    }

    public void Dispose() => _reader.Dispose();

    private static float ToLinear(double volumePercent) => (float)Math.Clamp(volumePercent / 100.0, 0, 4);

    private static ISampleProvider ToMixerFormat(ISampleProvider source, WaveFormat target)
    {
        if (source.WaveFormat.SampleRate != target.SampleRate)
            source = new WdlResamplingSampleProvider(source, target.SampleRate);

        if (source.WaveFormat.Channels != target.Channels)
        {
            source = source.WaveFormat.Channels == 1 && target.Channels == 2
                ? new MonoToStereoSampleProvider(source)
                : new StereoToMonoSampleProvider(source);
        }
        return source;
    }
}

/// <summary>Which voices a stop targets — every non-null field must match. See
/// <see cref="SoundEngine.Stop"/>.</summary>
public readonly record struct VoiceFilter(
    string? SoundId = null, string? DeviceId = null, string? PageId = null, string? WidgetId = null, bool? IsPreview = null)
{
    internal bool Matches(SoundVoice voice) =>
        (SoundId is null || voice.SoundId == SoundId) &&
        (DeviceId is null || voice.DeviceId == DeviceId) &&
        (PageId is null || voice.PageId == PageId) &&
        (WidgetId is null || voice.WidgetId == WidgetId) &&
        (IsPreview is null || voice.IsPreview == IsPreview);
}
