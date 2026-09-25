using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.SoundBoard;

/// <summary>Entry point (plugin.json's "entry") — the host finds this type by reflection and instantiates it
/// with a parameterless constructor, then calls <see cref="Initialize"/> once.</summary>
public sealed class SoundBoardPlugin : IPlugin, IDisposable
{
    private SoundBoardEngine? _engine;

    public void Initialize(IPluginHost host)
    {
        var engine = new SoundBoardEngine(host);
        _engine = engine;

        host.RegisterVariableProvider(engine);
        host.RegisterSettingsPage(new SoundBoardSettingsPage(host, engine));

        host.RegisterAction(new SoundBoardPlayAction(engine));
        host.RegisterAction(new SoundBoardStopAction(engine));
        host.RegisterAction(new SoundBoardSetMasterVolumeAction(engine));
    }

    public void Dispose() => _engine?.Dispose();
}
