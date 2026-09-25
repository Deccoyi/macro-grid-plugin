using System.Collections.Concurrent;
using MacroGrid.Plugin.Abstractions;
using NAudio.Wave;

namespace MacroGrid.Plugin.Sound.Tests;

/// <summary>In-memory <see cref="IVariableStore"/> — records every Set/Remove so a test can assert on what
/// the engine published, in addition to reading current values.</summary>
public sealed class FakeVariableStore : IVariableStore
{
    private readonly ConcurrentDictionary<string, object?> _values = new();

    public void Set(string name, object? value) => _values[name] = value;

    public object? Get(string name) => _values.TryGetValue(name, out var v) ? v : null;

    public void Remove(string name) => _values.TryRemove(name, out _);
}

/// <summary>Minimal <see cref="IPluginHost"/> — enough for <see cref="SoundEngine"/> (DataDirectory,
/// CreateStatusItem). Registration methods are no-ops: these tests drive SoundEngine directly rather than
/// through the real plugin loader.</summary>
public sealed class FakePluginHost(string dataDirectory) : IPluginHost
{
    public string ServerVersion => "0.0.0-test";
    public string SdkVersion => "0.0.0-test";
    public string DataDirectory { get; } = dataDirectory;

    public void Log(string message) { }
    public void RegisterAction(IActionHandler handler) { }
    public void RegisterVariableProvider(IVariableProvider provider) { }
    public void RegisterSettingsPage(IPluginSettingsPage page) { }
    public IPluginStatusItem CreateStatusItem(string id) => new FakeStatusItem();
    public void RegisterIconPack(IIconPackSource iconPack) { }
}

public sealed class FakeStatusItem : IPluginStatusItem
{
    public string? LastText { get; private set; }
    public StatusLevel LastLevel { get; private set; }

    public void Update(string text, StatusLevel level, string? icon = null, string? tooltip = null)
    {
        LastText = text;
        LastLevel = level;
    }
}

/// <summary>Writes a tiny mono 8-bit-per-sample-rate PCM WAV of constant full-scale amplitude, so tests can
/// exercise the real NAudio decode/resample/loop/fade chain without shipping a binary fixture.</summary>
public static class TestWav
{
    public static string CreateFullScale(string directory, TimeSpan duration, int sampleRate = 8000)
    {
        var path = Path.Combine(directory, $"test-{Guid.NewGuid():N}.wav");
        var format = new WaveFormat(sampleRate, 16, 1);
        using (var writer = new WaveFileWriter(path, format))
        {
            var totalSamples = (int)(duration.TotalSeconds * sampleRate);
            var buffer = new short[totalSamples];
            Array.Fill(buffer, short.MaxValue);
            var bytes = new byte[buffer.Length * 2];
            Buffer.BlockCopy(buffer, 0, bytes, 0, bytes.Length);
            writer.Write(bytes, 0, bytes.Length);
        }
        return path;
    }
}
