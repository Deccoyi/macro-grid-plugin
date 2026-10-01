using NAudio.Wave;

namespace MacroGrid.Plugin.SoundBoard.Tests;

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
