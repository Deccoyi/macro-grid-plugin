using MacroStation.Plugin.Abstractions;

namespace MacroStation.Plugin.Obs;

/// <summary>Entry point (plugin.json's "entry") — the host finds this type by reflection and instantiates it
/// with a parameterless constructor, then calls <see cref="Initialize"/> once.</summary>
public sealed class ObsPlugin : IPlugin
{
    public void Initialize(IPluginHost host)
    {
        var obs = new ObsConnection(host);
        host.RegisterVariableProvider(obs);

        host.RegisterAction(new ObsSetSceneAction(obs));
        host.RegisterAction(new ObsStartStreamAction(obs));
        host.RegisterAction(new ObsStopStreamAction(obs));
        host.RegisterAction(new ObsToggleStreamAction(obs));
        host.RegisterAction(new ObsStartRecordAction(obs));
        host.RegisterAction(new ObsStopRecordAction(obs));
        host.RegisterAction(new ObsToggleRecordAction(obs));
        host.RegisterAction(new ObsSetMuteAction(obs));
        host.RegisterAction(new ObsToggleMuteAction(obs));
        host.RegisterAction(new ObsSetVolumeAction(obs));
    }
}
