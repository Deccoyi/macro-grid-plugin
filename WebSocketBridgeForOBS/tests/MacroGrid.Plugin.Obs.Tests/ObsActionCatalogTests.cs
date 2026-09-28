using System.Text.Json;
using System.Text.Json.Serialization;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs.Tests;

/// <summary>Pins what the plugin registers with the host: the registration order and every action's descriptor
/// (type id, labels, icon, settings fields). Users' profiles reference these ids and setting keys, so a
/// refactor must not change them. Regenerate the golden file only for an intentional change:
/// set <c>MG_UPDATE_GOLDEN=1</c> and run the tests once.</summary>
public sealed class ObsActionCatalogTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public void RegisteredActionsMatchTheGoldenCatalog()
    {
        var host = new FakePluginHost(Path.Combine(Path.GetTempPath(), "mg-obs-catalog-" + Guid.NewGuid().ToString("N")));
        new ObsPlugin().Initialize(host);

        var catalog = host.Actions.Select(h => (Handler: h, Descriptor: (IActionDescriptor)h)).Select(a => new
        {
            a.Handler.Type,
            a.Handler.DisplayName,
            a.Descriptor.Category,
            a.Descriptor.Description,
            a.Descriptor.Icon,
            HasOptionsSource = a.Handler is IOptionsSource,
            a.Descriptor.Fields,
        });
        var actual = JsonSerializer.Serialize(catalog, Options).ReplaceLineEndings("\n");

        var golden = Path.Combine(AppContext.BaseDirectory, "ObsActionCatalog.golden.json");
        var source = Environment.GetEnvironmentVariable("MG_UPDATE_GOLDEN") is { Length: > 0 } dir
            ? Path.Combine(dir, "ObsActionCatalog.golden.json") : null;
        if (source is not null) File.WriteAllText(source, actual + "\n");

        Assert.Equal(File.ReadAllText(source ?? golden).ReplaceLineEndings("\n").TrimEnd(), actual.TrimEnd());
    }
}
