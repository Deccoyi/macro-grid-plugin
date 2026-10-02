using System.Collections.Concurrent;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.StreamTexts.Tests;

/// <summary>In-memory <see cref="IVariableStore"/>.</summary>
public sealed class FakeVariableStore : IVariableStore
{
    private readonly ConcurrentDictionary<string, object?> _values = new();

    public void Set(string name, object? value) => _values[name] = value;

    public object? Get(string name) => _values.TryGetValue(name, out var v) ? v : null;

    public void Remove(string name) => _values.TryRemove(name, out _);
}

/// <summary>Minimal <see cref="IPluginHost"/>: enough for <see cref="StreamTextsEngine"/> (DataDirectory, CreateStatusItem).</summary>
public sealed class FakePluginHost(string dataDirectory) : IPluginHost
{
    public FakeStatusItem Status { get; } = new();

    public string ServerVersion => "0.0.0-test";
    public string SdkVersion => "0.0.0-test";
    public string DataDirectory { get; } = dataDirectory;
    public IPluginSecrets Secrets => throw new NotSupportedException();
    public IPluginWidgets Widgets => throw new NotSupportedException();

    public void Log(string message) { }
    public void RegisterAction(IActionHandler handler) { }
    public void RegisterVariableProvider(IVariableProvider provider) { }
    public void RegisterSettingsPage(IPluginSettingsPage page) { }
    public IPluginStatusItem CreateStatusItem(string id) => Status;
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
