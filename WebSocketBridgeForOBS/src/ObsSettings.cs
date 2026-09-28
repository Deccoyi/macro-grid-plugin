using System.Text.Json;
using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Obs;

/// <summary>
/// OBS WebSocket (obs-websocket v5, built into OBS 28+) connection settings, persisted as this plugin's
/// own `settings.json` in <see cref="Plugin.Abstractions.IPluginHost.DataDirectory"/>. The password is
/// protected with <see cref="IPluginHost.Secrets"/> (host-side, DPAPI on Windows) before it is written, under
/// a different key (<c>protectedPassword</c>) than the legacy plain <c>password</c> one — a file from before
/// this is read once as plain text and rewritten protected on the next save. <see cref="Password"/> itself
/// always holds the plain value in memory; only the file on disk is protected.
/// </summary>
public sealed class ObsSettings
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4455;
    public string Password { get; set; } = "";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static ObsSettings LoadOrCreate(IPluginHost host)
    {
        Directory.CreateDirectory(host.DataDirectory);
        var path = Path.Combine(host.DataDirectory, "settings.json");
        if (!File.Exists(path))
        {
            var defaults = new ObsSettings();
            defaults.Save(host);
            return defaults;
        }

        try
        {
            var node = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            if (node is null) return new ObsSettings();

            var settings = new ObsSettings
            {
                Enabled = node["enabled"]?.GetValue<bool>() ?? false,
                Host = node["host"]?.GetValue<string>() ?? "127.0.0.1",
                Port = node["port"]?.GetValue<int>() ?? 4455,
            };

            if (node["protectedPassword"]?.GetValue<string>() is { Length: > 0 } protectedPassword)
                settings.Password = host.Secrets.Unprotect(protectedPassword) ?? "";
            else if (node["password"]?.GetValue<string>() is { Length: > 0 } legacyPlainPassword)
                settings.Password = legacyPlainPassword; // pre-migration file; Save() below writes it protected next time

            return settings;
        }
        catch (JsonException)
        {
            return new ObsSettings();
        }
    }

    public void Save(IPluginHost host)
    {
        Directory.CreateDirectory(host.DataDirectory);
        var node = new JsonObject
        {
            ["enabled"] = Enabled,
            ["host"] = Host,
            ["port"] = Port,
            ["protectedPassword"] = Password.Length > 0 ? host.Secrets.Protect(Password) : "",
        };
        File.WriteAllText(Path.Combine(host.DataDirectory, "settings.json"), node.ToJsonString(JsonOptions));
    }
}

/// <summary>The schema-driven settings window — replaces the raw-JSON
/// passthrough form the editor used to hand-build for this plugin specifically. Saving signals
/// <see cref="ObsConnection.NotifySettingsChanged"/> so a corrected host/port/password reconnects within
/// moments instead of waiting for the current backoff to expire.</summary>
public sealed class ObsSettingsPage(IPluginHost host, ObsConnection connection) : IPluginSettingsPage
{
    /// <summary>Shown at the bottom of the settings window; the same wording is in the README, NOTICE and the Turkish locale.</summary>
    public const string Disclaimer = "This plugin is an independent, third-party project. It is not affiliated with, endorsed by or sponsored by the OBS Project. OBS and OBS Studio are trademarks of their owners. Get OBS Studio at https://obsproject.com/.";

    public IReadOnlyList<SettingField> Fields =>
    [
        new("enabled", "Enabled", SettingFieldKind.Bool) { Default = true },
        new("host", "Host", SettingFieldKind.Text) { Default = "127.0.0.1", Placeholder = "127.0.0.1" },
        new("port", "Port", SettingFieldKind.Number) { Min = 1, Max = 65535, Step = 1, Default = 4455 },
        new("password", "Password", SettingFieldKind.Password),
        new("disclaimer", Disclaimer, SettingFieldKind.Notice),
    ];

    public JsonObject Load()
    {
        var settings = ObsSettings.LoadOrCreate(host);
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
        settings.Save(host);
        connection.NotifySettingsChanged();
    }
}
