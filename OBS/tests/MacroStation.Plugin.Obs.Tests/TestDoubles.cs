using System.Collections.Concurrent;
using MacroStation.Plugin.Abstractions;

namespace MacroStation.Plugin.Obs.Tests;

/// <summary>In-memory <see cref="IVariableStore"/> — records every Set/Remove so a test can assert on
/// what ObsConnection published, in addition to reading current values.</summary>
public sealed class FakeVariableStore : IVariableStore
{
    private readonly ConcurrentDictionary<string, object?> _values = new();

    public List<string> RemovedNames { get; } = [];

    public void Set(string name, object? value) => _values[name] = value;

    public object? Get(string name) => _values.TryGetValue(name, out var v) ? v : null;

    public void Remove(string name)
    {
        _values.TryRemove(name, out _);
        RemovedNames.Add(name);
    }
}

/// <summary>Minimal <see cref="IPluginHost"/> — enough for <see cref="ObsConnection"/> (DataDirectory,
/// Log, CreateStatusItem). The registration methods are no-ops since these tests drive ObsConnection
/// directly rather than through the real plugin loader.</summary>
public sealed class FakePluginHost(string dataDirectory) : IPluginHost
{
    public string ServerVersion => "0.0.0-test";
    public string SdkVersion => "0.0.0-test";
    public string DataDirectory { get; } = dataDirectory;

    public List<string> Logs { get; } = [];

    public void Log(string message) => Logs.Add(message);

    public void RegisterAction(IActionHandler handler) { }
    public void RegisterVariableProvider(IVariableProvider provider) { }
    public void RegisterSettingsPage(IPluginSettingsPage page) { }
    public IPluginStatusItem CreateStatusItem(string id) => new FakeStatusItem();
    public void RegisterIconPack(IIconPackSource iconPack) { }
}

public sealed class FakeStatusItem : IPluginStatusItem
{
    public void Update(string text, StatusLevel level, string? icon = null, string? tooltip = null) { }
}
