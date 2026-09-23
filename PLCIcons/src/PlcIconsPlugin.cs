using System.Reflection;
using MacroStation.Plugin.Abstractions;

namespace MacroStation.Plugin.PlcIcons;

/// <summary>Entry point (plugin.json's "entry") — the host finds this type by reflection and instantiates it
/// with a parameterless constructor, then calls <see cref="Initialize"/> once.</summary>
public sealed class PlcIconsPlugin : IPlugin
{
    public void Initialize(IPluginHost host) => host.RegisterIconPack(new PlcIconPack());
}

/// <summary>A static icon pack: ladder-logic function block icons (coil, timers, comparisons, arithmetic),
/// embedded as SVG resources in this assembly. No connection, no settings — the icons never change at
/// runtime, so <see cref="IIconPackSource"/> is the whole plugin.</summary>
public sealed class PlcIconPack : IIconPackSource
{
    // Sorted so the picker's default ordering doesn't depend on the filesystem's own order.
    private static readonly string[] Names =
    [
        "add", "calculate", "close-branch", "coil", "convert", "divide", "empty-block", "equal",
        "f-trig", "greater", "greater-equal", "lesser", "lesser-equal", "move", "multiply", "n",
        "nc", "no", "not-equal", "open-branch", "p", "r-trig", "reset-coil", "set-coil", "subtract",
        "timer-convert", "tof-timer", "ton-timer", "tp-timer",
    ];

    public string Id => "plc-icons";

    public string DisplayName => "PLC İkonları";

    public IReadOnlyList<string> IconNames => Names;

    public string? GetIconSvg(string name)
    {
        if (Array.IndexOf(Names, name) < 0) return null;

        var assembly = typeof(PlcIconPack).Assembly;
        using var stream = assembly.GetManifestResourceStream($"MacroStation.Plugin.PlcIcons.icons.{name}.svg");
        if (stream is null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
