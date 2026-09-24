using System.Text.Json;
using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

/// <summary>
/// OBS WebSocket (obs-websocket v5, built into OBS 28+) connection settings, persisted as this plugin's
/// own `settings.json` in <see cref="Plugin.Abstractions.IPluginHost.DataDirectory"/>. The password is
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

    public void Save(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(Path.Combine(dataDirectory, "settings.json"), JsonSerializer.Serialize(this, JsonOptions));
    }
}

/// <summary>The schema-driven settings window — replaces the raw-JSON
/// passthrough form the editor used to hand-build for this plugin specifically. Saving signals
/// <see cref="ObsConnection.NotifySettingsChanged"/> so a corrected host/port/password reconnects within
/// moments instead of waiting for the current backoff to expire.</summary>
public sealed class ObsSettingsPage(IPluginHost host, ObsConnection connection) : IPluginSettingsPage
{
    public IReadOnlyList<SettingField> Fields =>
    [
        new("enabled", "Etkin", SettingFieldKind.Bool) { Default = true },
        new("host", "Sunucu", SettingFieldKind.Text) { Default = "127.0.0.1", Placeholder = "127.0.0.1" },
        new("port", "Port", SettingFieldKind.Number) { Min = 1, Max = 65535, Step = 1, Default = 4455 },
        new("password", "Şifre", SettingFieldKind.Password),
    ];

    public JsonObject Load()
    {
        var settings = ObsSettings.LoadOrCreate(host.DataDirectory);
        return new JsonObject
        {
            ["enabled"] = settings.Enabled,
            ["host"] = settings.Host,
            ["port"] = settings.Port,
            ["password"] = settings.Password,
        };
    }

    public void Save(JsonObject values)
    {
        var settings = new ObsSettings
        {
            Enabled = values["enabled"]?.GetValue<bool>() ?? false,
            Host = values["host"]?.GetValue<string>() ?? "127.0.0.1",
            Port = values["port"]?.GetValue<int>() ?? 4455,
            Password = values["password"]?.GetValue<string>() ?? "",
        };
        settings.Save(host.DataDirectory);
        connection.NotifySettingsChanged();
    }
}
