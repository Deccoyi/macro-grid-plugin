using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.StreamTexts;

/// <summary>One text file: <see cref="Name"/> (the file name without ".txt") and the <see cref="Template"/> written into it.</summary>
public sealed class TextEntry
{
    public string Name { get; set; } = "";
    public string Template { get; set; } = "";
}

/// <summary>Persisted as this plugin's own <c>settings.json</c> in <see cref="IPluginHost.DataDirectory"/>.</summary>
public sealed class StreamTextsSettingsData
{
    public const int MinIntervalMs = 250;
    public const int MaxIntervalMs = 10_000;
    public const int DefaultIntervalMs = 500;
    public const int DefaultOverlayPort = 9830;
    public const int MinOverlayPort = 1024;
    public const int MaxOverlayPort = 65_535;
    public const int MaxEntries = 64;
    public const int MaxTemplateLength = 1000;

    /// <summary>Null or empty means <see cref="DefaultFolder"/>.</summary>
    public string? OutputFolder { get; set; }

    public int IntervalMs { get; set; } = DefaultIntervalMs;

    /// <summary>Write the .txt files. Turn off to use only the overlay (the files this plugin made are then removed).</summary>
    public bool WriteFiles { get; set; } = true;

    /// <summary>Serve the texts to browser sources on <c>http://127.0.0.1:OverlayPort/t/&lt;name&gt;</c>. Off by default.</summary>
    public bool OverlayEnabled { get; set; }

    public int OverlayPort { get; set; } = DefaultOverlayPort;

    public List<TextEntry> Texts { get; set; } = [];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static readonly Regex ValidName = new(@"^[\p{L}\p{N}_\- ]{1,64}$", RegexOptions.Compiled);
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Macro Grid", "Stream Texts");

    [JsonIgnore]
    public string ResolvedFolder => string.IsNullOrWhiteSpace(OutputFolder) ? DefaultFolder : OutputFolder.Trim();

    /// <summary>A name is a plain file name only: letters, digits, '_', '-' and spaces, 1-64 characters, no leading/trailing space
    /// and no Windows device name. The plugin adds ".txt" itself, so a name can never point outside the folder or at another file type.</summary>
    public static bool IsValidName(string? name) =>
        name is not null && name == name.Trim() && ValidName.IsMatch(name) && !ReservedNames.Contains(name);

    public string FilePath(string name) => Path.Combine(ResolvedFolder, name + ".txt");

    public static StreamTextsSettingsData LoadOrCreate(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        var path = Path.Combine(dataDirectory, "settings.json");
        if (!File.Exists(path))
        {
            var defaults = new StreamTextsSettingsData
            {
                Texts =
                [
                    new TextEntry { Name = "system", Template = "CPU {system.cpu|0}% | RAM {system.ram|0}%" },
                ],
            };
            File.WriteAllText(path, JsonSerializer.Serialize(defaults, JsonOptions));
            return defaults;
        }

        try
        {
            var data = JsonSerializer.Deserialize<StreamTextsSettingsData>(File.ReadAllText(path), JsonOptions) ?? new StreamTextsSettingsData();
            return data.Normalized();
        }
        catch (JsonException)
        {
            return new StreamTextsSettingsData();
        }
    }

    public void Save(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(Path.Combine(dataDirectory, "settings.json"), JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>Clamps the interval and drops what the engine would refuse anyway (bad or duplicate names, rows past the cap).</summary>
    public StreamTextsSettingsData Normalized()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var texts = new List<TextEntry>();
        foreach (var entry in Texts ?? [])
        {
            if (texts.Count >= MaxEntries) break;
            if (!IsValidName(entry.Name) || !seen.Add(entry.Name)) continue;
            var template = entry.Template ?? "";
            if (template.Length > MaxTemplateLength) template = template[..MaxTemplateLength];
            texts.Add(new TextEntry { Name = entry.Name, Template = template });
        }

        return new StreamTextsSettingsData
        {
            OutputFolder = string.IsNullOrWhiteSpace(OutputFolder) ? null : OutputFolder.Trim(),
            IntervalMs = Math.Clamp(IntervalMs, MinIntervalMs, MaxIntervalMs),
            WriteFiles = WriteFiles,
            OverlayEnabled = OverlayEnabled,
            OverlayPort = Math.Clamp(OverlayPort, MinOverlayPort, MaxOverlayPort),
            Texts = texts,
        };
    }
}

/// <summary>The schema-driven settings window: the output folder, how often values are checked and the list of text files
/// (name + template). Also answers the "open folder" and per-row "show path" buttons (<see cref="ISettingsCommandHandler"/>).</summary>
public sealed class StreamTextsSettingsPage(IPluginHost host, StreamTextsEngine engine) : IPluginSettingsPage, ISettingsCommandHandler
{
    public IReadOnlyList<SettingField> Fields =>
    [
        new("outputFolder", "Folder", SettingFieldKind.Text)
        {
            Placeholder = StreamTextsSettingsData.DefaultFolder,
            Description = "Where the text files are written. Leave empty for the default folder.",
        },
        new("openFolder", "Open folder", SettingFieldKind.Button) { Command = "openFolder" },
        new("intervalMs", "Check every (ms)", SettingFieldKind.Number)
        {
            Min = StreamTextsSettingsData.MinIntervalMs,
            Max = StreamTextsSettingsData.MaxIntervalMs,
            Step = 50,
            Default = StreamTextsSettingsData.DefaultIntervalMs,
            Description = "A file is only rewritten when its text changed. A streaming app re-reads a file about once a second.",
        },
        new("writeFiles", "Write text files", SettingFieldKind.Bool)
        {
            Default = true,
            Description = "Turn off to use only the browser source. The files this plugin made are then deleted.",
        },
        new("overlayEnabled", "Browser source server", SettingFieldKind.Bool)
        {
            Default = false,
            Description = "Serves each text at http://127.0.0.1:<port>/t/<name> for a browser source. This PC only. Add ?css=... to style it. Uses more memory in the streaming app than a text file.",
        },
        new("overlayPort", "Port", SettingFieldKind.Number)
        {
            Min = StreamTextsSettingsData.MinOverlayPort,
            Max = StreamTextsSettingsData.MaxOverlayPort,
            Step = 1,
            Default = StreamTextsSettingsData.DefaultOverlayPort,
        },
        new("texts", "Text files", SettingFieldKind.List)
        {
            ItemFields =
            [
                // Name first: it is what a collapsed row shows (the editor titles a collapsed row by its "name" value).
                new("name", "File name", SettingFieldKind.Text)
                {
                    Description = "Letters, digits, space, - and _ (up to 64). \".txt\" is added for you.",
                },
                new("template", "Text", SettingFieldKind.Text)
                {
                    AllowVariables = true,
                    Placeholder = "CPU {system.cpu|0}%",
                    Description = "Use {variable}, {variable|format} or {variable|format|shown when empty}. Write \\n for a new line.",
                },
                new("showPath", "Show file path", SettingFieldKind.Button) { Command = "showPath" },
            ],
        },
    ];

    public JsonObject Load()
    {
        var data = engine.Settings;
        var texts = new JsonArray();
        foreach (var t in data.Texts)
            texts.Add(new JsonObject { ["name"] = t.Name, ["template"] = t.Template });

        return new JsonObject
        {
            ["outputFolder"] = data.OutputFolder ?? "",
            ["intervalMs"] = (double)data.IntervalMs,
            ["writeFiles"] = data.WriteFiles,
            ["overlayEnabled"] = data.OverlayEnabled,
            ["overlayPort"] = (double)data.OverlayPort,
            ["texts"] = texts,
        };
    }

    public void Save(JsonObject values)
    {
        var texts = new List<TextEntry>();
        if (values["texts"] is JsonArray array)
        {
            foreach (var node in array)
            {
                if (node is not JsonObject row) continue;
                texts.Add(new TextEntry
                {
                    Name = row["name"]?.GetValue<string>()?.Trim() ?? "",
                    Template = row["template"]?.GetValue<string>() ?? "",
                });
            }
        }

        var data = new StreamTextsSettingsData
        {
            OutputFolder = values["outputFolder"]?.GetValue<string>(),
            IntervalMs = (int)(values["intervalMs"]?.GetValue<double>() ?? StreamTextsSettingsData.DefaultIntervalMs),
            WriteFiles = values["writeFiles"]?.GetValue<bool>() ?? true,
            OverlayEnabled = values["overlayEnabled"]?.GetValue<bool>() ?? false,
            OverlayPort = (int)(values["overlayPort"]?.GetValue<double>() ?? StreamTextsSettingsData.DefaultOverlayPort),
            Texts = texts,
        }.Normalized();

        data.Save(host.DataDirectory);
        engine.ApplySettings(data);
    }

    public Task<string?> RunCommandAsync(string command, JsonObject values, CancellationToken cancellationToken)
    {
        var settings = new StreamTextsSettingsData { OutputFolder = values["outputFolder"]?.GetValue<string>() };

        switch (command)
        {
            case "openFolder":
                return Task.FromResult(engine.OpenFolder(settings.ResolvedFolder));
            case "showPath":
                var name = values["name"]?.GetValue<string>()?.Trim();
                if (!StreamTextsSettingsData.IsValidName(name))
                    return Task.FromResult<string?>("Give the file a valid name first (letters, digits, space, - and _).");
                return Task.FromResult<string?>(settings.FilePath(name!));
            default:
                return Task.FromResult<string?>($"Unknown command: {command}");
        }
    }
}
