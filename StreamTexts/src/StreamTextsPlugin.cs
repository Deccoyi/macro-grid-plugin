using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.StreamTexts;

/// <summary>Entry point (plugin.json's "entry") — the host finds this type by reflection and instantiates it
/// with a parameterless constructor, then calls <see cref="Initialize"/> once.</summary>
public sealed class StreamTextsPlugin : IPlugin, IDisposable
{
    private StreamTextsEngine? _engine;

    public void Initialize(IPluginHost host)
    {
        var engine = new StreamTextsEngine(host);
        _engine = engine;

        host.RegisterVariableProvider(engine);
        host.RegisterSettingsPage(new StreamTextsSettingsPage(host, engine));
    }

    public void Dispose() => _engine?.Dispose();
}
