using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Sound;

/// <summary>Entry point (plugin.json's "entry") — the host finds this type by reflection and instantiates it
/// with a parameterless constructor, then calls <see cref="Initialize"/> once.</summary>
public sealed class SoundPlugin : IPlugin, IDisposable
{
    private SoundEngine? _engine;

    public void Initialize(IPluginHost host)
    {
        var engine = new SoundEngine(host);
        _engine = engine;

        host.RegisterVariableProvider(engine);
        host.RegisterSettingsPage(new SoundSettingsPage(host, engine));

        host.RegisterAction(new SoundPlayAction(engine));
        host.RegisterAction(new SoundStopAction(engine));
        host.RegisterAction(new SoundSetMasterVolumeAction(engine));
    }

    public void Dispose() => _engine?.Dispose();
}
