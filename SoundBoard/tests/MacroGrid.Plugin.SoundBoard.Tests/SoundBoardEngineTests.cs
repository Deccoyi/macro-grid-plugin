using MacroGrid.Plugin.Abstractions;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MacroGrid.Plugin.SoundBoard.Tests;

/// <summary>Exercises the sample-provider chain and the engine's playback bookkeeping "by hand" — no WASAPI
/// device is ever opened (SoundBoardEngine degrades to logging a status instead of throwing when one isn't
/// available, see EnsureOutputOpen), so these run the same on a machine with no audio hardware.</summary>
public sealed class SoundBoardEngineTests : IDisposable
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "sound-tests-" + Guid.NewGuid().ToString("N"));
    private static readonly WaveFormat MixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    public SoundBoardEngineTests() => Directory.CreateDirectory(_dataDir);

    public void Dispose()
    {
        try { Directory.Delete(_dataDir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    // ---- the voice chain itself (loop / fade / natural end) ----

    [Fact]
    public void A_looping_voice_never_returns_zero_across_several_passes()
    {
        var file = TestWav.CreateFullScale(_dataDir, TimeSpan.FromMilliseconds(30), sampleRate: 8000);
        var voice = new SoundVoice(file, 100, loop: true, MixerFormat, "s1", "d", "p", "w", isPreview: false);
        var buffer = new float[512];

        // 30ms @ 8kHz mono resampled to 48kHz stereo is a few thousand floats; read enough to cross the
        // loop point several times over.
        for (var i = 0; i < 200; i++)
            Assert.True(voice.Read(buffer, 0, buffer.Length) > 0);

        Assert.False(voice.Finished);
        voice.Dispose();
    }

    [Fact]
    public void A_non_looping_voice_finishes_once_the_file_ends()
    {
        var file = TestWav.CreateFullScale(_dataDir, TimeSpan.FromMilliseconds(20), sampleRate: 8000);
        var voice = new SoundVoice(file, 100, loop: false, MixerFormat, "s1", "d", "p", "w", isPreview: false);
        var buffer = new float[512];

        var reads = 0;
        while (voice.Read(buffer, 0, buffer.Length) > 0 && reads < 10_000) reads++;

        Assert.True(voice.Finished);
        Assert.True(reads is > 0 and < 10_000);
        voice.Dispose();
    }

    [Fact]
    public void BeginFadeIn_starts_the_very_first_read_near_silent()
    {
        var file = TestWav.CreateFullScale(_dataDir, TimeSpan.FromMilliseconds(500), sampleRate: 8000);
        var voice = new SoundVoice(file, 100, loop: false, MixerFormat, "s1", "d", "p", "w", isPreview: false);
        voice.BeginFadeIn(200);

        var buffer = new float[64];
        voice.Read(buffer, 0, buffer.Length);

        Assert.All(buffer, sample => Assert.True(Math.Abs(sample) < 0.05f, $"expected near-silent, got {sample}"));
        voice.Dispose();
    }

    [Fact]
    public void A_mixer_keeps_reading_full_buffers_after_its_only_voice_ends()
    {
        // ReadFully: WasapiOut's callback must never starve just because nothing is playing.
        var mixer = new MixingSampleProvider(MixerFormat) { ReadFully = true };
        var file = TestWav.CreateFullScale(_dataDir, TimeSpan.FromMilliseconds(10), sampleRate: 8000);
        var voice = new SoundVoice(file, 100, loop: false, MixerFormat, "s1", "d", "p", "w", isPreview: false);
        mixer.AddMixerInput(voice);

        var buffer = new float[512];
        for (var i = 0; i < 200; i++)
            Assert.Equal(buffer.Length, mixer.Read(buffer, 0, buffer.Length));
    }

    // ---- SoundBoardEngine's own bookkeeping, added to the mixer directly (AddVoiceForTesting) ----

    private SoundVoice NewVoice(string soundId, TimeSpan duration, string deviceId = "d", string pageId = "p", string widgetId = "w", bool isPreview = false) =>
        new(TestWav.CreateFullScale(_dataDir, duration, sampleRate: 8000), 100, loop: false, MixerFormat, soundId, deviceId, pageId, widgetId, isPreview);

    [Fact]
    public void Stop_immediate_removes_the_voice_right_away()
    {
        using var engine = new SoundBoardEngine(new FakePluginHost(_dataDir));
        engine.AddVoiceForTesting(NewVoice("s1", TimeSpan.FromSeconds(5)));

        engine.Stop(new VoiceFilter(SoundId: "s1"), "immediate");

        Assert.Empty(engine.VoicesForTesting);
    }

    [Fact]
    public async Task Stop_fade_removes_the_voice_only_after_the_fade_out_delay()
    {
        using var engine = new SoundBoardEngine(new FakePluginHost(_dataDir));
        engine.AddVoiceForTesting(NewVoice("s1", TimeSpan.FromSeconds(5)));

        engine.Stop(new VoiceFilter(SoundId: "s1"), "fade"); // uses settings.FadeOutMs (default 500ms)

        Assert.NotEmpty(engine.VoicesForTesting); // still fading
        await Task.Delay(700);
        Assert.Empty(engine.VoicesForTesting); // reaped after the fade
    }

    [Fact]
    public void StopAll_never_touches_a_preview_voice()
    {
        using var engine = new SoundBoardEngine(new FakePluginHost(_dataDir));
        engine.AddVoiceForTesting(NewVoice("s1", TimeSpan.FromSeconds(5)));
        engine.AddVoiceForTesting(NewVoice("", TimeSpan.FromSeconds(5), isPreview: true));

        engine.StopAll("immediate");

        Assert.Single(engine.VoicesForTesting);
        Assert.True(engine.VoicesForTesting[0].IsPreview);
    }

    // ---- soundboard.play / soundboard.stop actions: overlap, cut, toggle, hold-release ----

    private static ActionContext Context(string deviceId = "dev1", string pageId = "page1", string widgetId = "w1") =>
        new(deviceId, pageId, widgetId, new FakeDeviceController());

    private (SoundBoardEngine Engine, string SoundAId, string SoundBId) NewEngineWithTwoSounds()
    {
        var host = new FakePluginHost(_dataDir);
        var engine = new SoundBoardEngine(host);
        var page = new SoundBoardSettingsPage(host, engine);
        var fileA = TestWav.CreateFullScale(_dataDir, TimeSpan.FromSeconds(5), sampleRate: 8000);
        var fileB = TestWav.CreateFullScale(_dataDir, TimeSpan.FromSeconds(5), sampleRate: 8000);
        page.Save(new System.Text.Json.Nodes.JsonObject
        {
            ["sounds"] = new System.Text.Json.Nodes.JsonArray
            {
                new System.Text.Json.Nodes.JsonObject { ["file"] = fileA, ["name"] = "A" },
                new System.Text.Json.Nodes.JsonObject { ["file"] = fileB, ["name"] = "B" },
            },
        });
        var ids = engine.Settings.Sounds.Select(s => s.Id).ToArray();
        return (engine, ids[0], ids[1]);
    }

    [Fact]
    public async Task Overlap_mode_lets_a_second_sound_play_alongside_the_first()
    {
        var (engine, soundA, soundB) = NewEngineWithTwoSounds();
        using var _ = engine;
        var action = new SoundBoardPlayAction(engine);

        await action.ExecuteAsync(Context(), SoundBoardSettingsForPlay(soundA), default);
        await action.ExecuteAsync(Context(), SoundBoardSettingsForPlay(soundB), default);

        Assert.True(engine.IsPlaying(soundA));
        Assert.True(engine.IsPlaying(soundB));
    }

    [Fact]
    public async Task Cut_mode_stops_everything_else_before_starting_the_new_sound()
    {
        var (engine, soundA, soundB) = NewEngineWithTwoSounds();
        using var _ = engine;
        SetOverlapMode(engine, "cut");
        var action = new SoundBoardPlayAction(engine);

        await action.ExecuteAsync(Context(), SoundBoardSettingsForPlay(soundA), default);
        Assert.True(engine.IsPlaying(soundA));

        await action.ExecuteAsync(Context(), SoundBoardSettingsForPlay(soundB), default);

        Assert.False(engine.IsPlaying(soundA));
        Assert.True(engine.IsPlaying(soundB));
    }

    [Fact]
    public async Task Toggle_mode_stops_the_sound_if_it_is_already_playing()
    {
        var (engine, soundA, _) = NewEngineWithTwoSounds();
        using var _ = engine;
        var action = new SoundBoardPlayAction(engine);
        var settings = SoundBoardSettingsForPlay(soundA, playMode: "toggle");

        await action.ExecuteAsync(Context(), settings, default);
        Assert.True(engine.IsPlaying(soundA));

        await action.ExecuteAsync(Context(), settings, default);
        Assert.False(engine.IsPlaying(soundA));
    }

    [Fact]
    public async Task Hold_mode_stops_only_on_release_of_the_same_widget()
    {
        var (engine, soundA, _) = NewEngineWithTwoSounds();
        using var _ = engine;
        var action = new SoundBoardPlayAction(engine);
        var settings = SoundBoardSettingsForPlay(soundA, playMode: "hold");

        await action.ExecuteAsync(Context(deviceId: "dev1", widgetId: "w1"), settings, default);
        Assert.True(engine.IsPlaying(soundA));

        // A release from a different widget must not stop it.
        await action.ReleaseAsync(Context(deviceId: "dev1", widgetId: "w2"), settings, default);
        Assert.True(engine.IsPlaying(soundA));

        // The same widget's release does.
        await action.ReleaseAsync(Context(deviceId: "dev1", widgetId: "w1"), settings, default);
        Assert.False(engine.IsPlaying(soundA));
    }

    [Fact]
    public async Task Release_in_full_mode_does_nothing()
    {
        var (engine, soundA, _) = NewEngineWithTwoSounds();
        using var _ = engine;
        var action = new SoundBoardPlayAction(engine);
        var settings = SoundBoardSettingsForPlay(soundA, playMode: "full");

        await action.ExecuteAsync(Context(), settings, default);
        await action.ReleaseAsync(Context(), settings, default);

        Assert.True(engine.IsPlaying(soundA));
    }

    private static System.Text.Json.Nodes.JsonObject SoundBoardSettingsForPlay(string soundId, string playMode = "full") => new()
    {
        ["sound"] = soundId,
        ["playMode"] = playMode,
    };

    private static void SetOverlapMode(SoundBoardEngine engine, string mode)
    {
        var current = engine.Settings;
        engine.ApplySettings(new SoundBoardSettingsData
        {
            OutputDeviceId = current.OutputDeviceId,
            Sounds = current.Sounds,
            MasterVolume = current.MasterVolume,
            OverlapMode = mode,
            StopStyle = current.StopStyle,
            FadeInMs = current.FadeInMs,
            FadeOutMs = current.FadeOutMs,
        });
    }
}

/// <summary>Records nothing — SoundBoardEngine tests never call into IDeviceController, only ActionContext needs one.</summary>
internal sealed class FakeDeviceController : IDeviceController
{
    public Task ShowPageAsync(string pageId) => Task.CompletedTask;
    public Task NextPageAsync() => Task.CompletedTask;
    public Task PreviousPageAsync() => Task.CompletedTask;
    public Task BackAsync() => Task.CompletedTask;
    public Task SwitchProfileAsync(string profileId) => Task.CompletedTask;
}
