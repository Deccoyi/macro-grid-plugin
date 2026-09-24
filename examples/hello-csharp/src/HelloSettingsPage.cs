using System.Text.Json;
using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace HelloCSharp;

public sealed record HelloSettings(string Name = "world", string Greeting = "Hello", bool Loud = false);

// #region settings
/// <summary>A settings form drawn by the editor from <see cref="Fields"/>. <see cref="Load"/> and <see cref="Save"/>
/// bridge it to a settings.json file in the plugin's own folder.</summary>
public sealed class HelloSettingsPage : IPluginSettingsPage
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;
    private readonly string _directory;

    public HelloSettingsPage(IPluginHost host)
    {
        _directory = host.DataDirectory;
        _path = Path.Combine(_directory, "settings.json");
        Current = Read();
    }

    /// <summary>What the action reads. Loaded once at start-up and replaced on every save.</summary>
    public HelloSettings Current { get; private set; }

    public IReadOnlyList<SettingField> Fields =>
    [
        new("name", "Name", SettingFieldKind.Text) { Default = "world", Placeholder = "world" },
        new("greeting", "Greeting", SettingFieldKind.Text) { Default = "Hello" },
        new("loud", "Loud", SettingFieldKind.Bool) { Description = "Greet in capital letters." },
    ];

    public JsonObject Load() => new()
    {
        ["name"] = Current.Name,
        ["greeting"] = Current.Greeting,
        ["loud"] = Current.Loud,
    };

    public void Save(JsonObject values)
    {
        Current = new HelloSettings(
            values["name"]?.GetValue<string>() ?? "world",
            values["greeting"]?.GetValue<string>() ?? "Hello",
            values["loud"]?.GetValue<bool>() ?? false);

        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, JsonSerializer.Serialize(Current, Json));
    }

    private HelloSettings Read()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<HelloSettings>(File.ReadAllText(_path), Json) ?? new()
                : new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
// #endregion settings
