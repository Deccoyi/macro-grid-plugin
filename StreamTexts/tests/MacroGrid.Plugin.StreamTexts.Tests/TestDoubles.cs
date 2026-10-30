
namespace MacroGrid.Plugin.StreamTexts.Tests;

/// <summary>A throwaway data folder plus output folder for one test.</summary>
public sealed class TempFolders : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "streamtexts-test-" + Guid.NewGuid().ToString("N"));
    public string Data => Path.Combine(Root, "data");
    public string Output => Path.Combine(Root, "out");

    public TempFolders() => Directory.CreateDirectory(Root);

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); } catch (IOException) { }
    }
}
