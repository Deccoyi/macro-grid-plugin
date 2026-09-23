using System.Text.Json;

namespace MacroStation.Plugin.Obs;

/// <summary>
/// OBS WebSocket (obs-websocket v5, built into OBS 28+) connection settings. This plugin has no editor
/// settings UI yet (the host doesn't expose one to plugins — see plugin-authoring.md §5), so it persists
/// its own `settings.json` in <see cref="Plugin.Abstractions.IPluginHost.DataDirectory"/>: the user edits
/// that file by hand to set host/port/password and flip <see cref="Enabled"/> to true. The password is
/// stored in plain text, consistent with how the main program stores its own OBS/pairing secrets today.
/// </summary>
public sealed class ObsSettings
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4455;
    public string Password { get; set; } = "";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static ObsSettings LoadOrCreate(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        var path = Path.Combine(dataDirectory, "settings.json");
        if (!File.Exists(path))
        {
            var defaults = new ObsSettings();
            File.WriteAllText(path, JsonSerializer.Serialize(defaults, JsonOptions));
            return defaults;
        }

        try
        {
            return JsonSerializer.Deserialize<ObsSettings>(File.ReadAllText(path), JsonOptions) ?? new ObsSettings();
        }
        catch (JsonException)
        {
            return new ObsSettings();
        }
    }
}
