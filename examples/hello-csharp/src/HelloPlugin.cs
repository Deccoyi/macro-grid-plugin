using MacroGrid.Plugin.Abstractions;

namespace HelloCSharp;

// #region plugin
/// <summary>The entry point: the server finds this type by reflection, creates it with a parameterless
/// constructor and calls <see cref="Initialize"/> once.</summary>
public sealed class HelloPlugin : IPlugin
{
    public void Initialize(IPluginHost host)
    {
        var settings = new HelloSettingsPage(host);
        var counter = new GreetingCounter();
        var status = host.CreateStatusItem("greetings");
        status.Update("Hello: ready", StatusLevel.Idle);

        host.RegisterVariableProvider(counter);
        host.RegisterSettingsPage(settings);
        host.RegisterAction(new GreetAction(host, settings, counter, status));
    }
}
// #endregion plugin
