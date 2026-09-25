using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.Sound;

/// <summary>One row of the "sounds" list. <see cref="Id"/> is a permanent short id ("s1", "s2", ...) assigned
/// by <see cref="SoundSettingsPage.Save"/> the first time a row is saved, and used to name its
/// <c>sound.&lt;id&gt;.*</c> variables and its "sound" action option — renaming or reordering the row later
/// never changes it. <see cref="File"/> is the absolute path as the person picked it; the file is never
/// copied into the plugin's own folder, so moving it means picking it again.</summary>
public sealed class SoundEntry
{
    public string Id { get; set; } = "";
    public string File { get; set; } = "";
    public string Name { get; set; } = "";
    public double Volume { get; set; } = 100;
    public bool Loop { get; set; }
}

/// <summary>Persisted as this plugin's own <c>settings.json</c> in <see cref="IPluginHost.DataDirectory"/>.</summary>
public sealed class SoundSettingsData
{
    /// <summary>A CoreAudio device id, or null/empty for the system default render device.</summary>
    public string? OutputDeviceId { get; set; }

    public List<SoundEntry> Sounds { get; set; } = [];

    public double MasterVolume { get; set; } = 100;

    /// <summary>"overlap" (pressing the same sound again starts another copy) or "cut" (a new sound stops
    /// the ones currently playing).</summary>
    public string OverlapMode { get; set; } = "overlap";

    /// <summary>"immediate" or "fade" — the default a <c>sound.stop</c>/<c>sound.play</c> binding falls back
    /// to when its own <c>stopStyle</c> field is left at "default".</summary>
    public string StopStyle { get; set; } = "immediate";

    public int FadeInMs { get; set; }
    public int FadeOutMs { get; set; } = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static SoundSettingsData LoadOrCreate(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        var path = Path.Combine(dataDirectory, "settings.json");
        if (!File.Exists(path))
        {
            var defaults = new SoundSettingsData();
            File.WriteAllText(path, JsonSerializer.Serialize(defaults, JsonOptions));
            return defaults;
        }

        try
        {
            return JsonSerializer.Deserialize<SoundSettingsData>(File.ReadAllText(path), JsonOptions) ?? new SoundSettingsData();
        }
        catch (JsonException)
        {
            return new SoundSettingsData();
        }
    }

    public void Save(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(Path.Combine(dataDirectory, "settings.json"), JsonSerializer.Serialize(this, JsonOptions));
    }
}

/// <summary>The schema-driven settings window: the output device, the list of sounds (file, name, volume,
/// loop, a preview button and a missing-file notice), master volume and the overlap/stop/fade defaults.
/// Also answers the "devices" dropdown (<see cref="IOptionsSource"/>) and the "preview" button
/// (<see cref="ISettingsCommandHandler"/>).</summary>
public sealed class SoundSettingsPage(IPluginHost host, SoundEngine engine) : IPluginSettingsPage, IOptionsSource, ISettingsCommandHandler
{
    private const string AudioFileFilter =
        "Audio files (*.wav;*.mp3;*.aac;*.m4a;*.wma;*.flac;*.aiff;*.ogg)|*.wav;*.mp3;*.aac;*.m4a;*.wma;*.flac;*.aiff;*.ogg";

    public IReadOnlyList<SettingField> Fields =>
    [
        new("outputDevice", "Output device", SettingFieldKind.Select) { OptionsSource = "devices" },
        new("sounds", "Sounds", SettingFieldKind.List)
        {
            ItemFields =
            [
                new("file", "File", SettingFieldKind.File) { FileFilter = AudioFileFilter },
                new("name", "Name", SettingFieldKind.Text),
                new("volume", "Volume (%)", SettingFieldKind.Slider) { Min = 0, Max = 100, Step = 1, Default = 100 },
                new("loop", "Loop", SettingFieldKind.Bool),
                new("preview", "Preview", SettingFieldKind.Button) { Command = "preview" },
                new("missingNotice", "File not found, pick it again.", SettingFieldKind.Notice) { VisibleWhen = "missing=true" },
            ],
        },
        new("masterVolume", "Master volume (%)", SettingFieldKind.Slider) { Min = 0, Max = 100, Step = 1, Default = 100 },
        new("overlapMode", "When the same sound plays again", SettingFieldKind.Segmented)
        {
            Options = [new("overlap", "Overlap"), new("cut", "Cut")],
            Default = "overlap",
        },
        new("stopStyle", "Default stop style", SettingFieldKind.Segmented)
        {
            Options = [new("immediate", "Immediate"), new("fade", "Fade")],
            Default = "immediate",
        },
        new("fadeInMs", "Fade in (ms)", SettingFieldKind.Number) { Min = 0, Step = 50, Default = 0 },
        new("fadeOutMs", "Fade out (ms)", SettingFieldKind.Number) { Min = 0, Step = 50, Default = 500 },
    ];

    public JsonObject Load()
    {
        var data = engine.Settings;
        var sounds = new JsonArray();
        foreach (var s in data.Sounds)
        {
            sounds.Add(new JsonObject
            {
                ["id"] = s.Id,
                ["file"] = s.File,
                ["name"] = s.Name,
                ["volume"] = s.Volume,
                ["loop"] = s.Loop,
                // Computed here, not stored: whether the row's own value needs a fresh Load() to catch a
                // moved/deleted file. Not a declared field — kept only because "missingNotice" reads it.
                ["missing"] = s.File.Length == 0 || !System.IO.File.Exists(s.File),
            });
        }

        return new JsonObject
        {
            ["outputDevice"] = data.OutputDeviceId,
            ["sounds"] = sounds,
            ["masterVolume"] = data.MasterVolume,
            ["overlapMode"] = data.OverlapMode,
            ["stopStyle"] = data.StopStyle,
            // Number fields round-trip as JSON numbers (double), same representation the Save() side reads with GetValue<double>().
            ["fadeInMs"] = (double)data.FadeInMs,
            ["fadeOutMs"] = (double)data.FadeOutMs,
        };
    }

    public void Save(JsonObject values)
    {
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (values["sounds"] is JsonArray rows)
        {
            foreach (var row in rows)
                if (row is JsonObject o && o["id"]?.GetValue<string>() is { Length: > 0 } id)
                    usedIds.Add(id);
        }

        var nextIdNumber = 1;
        string AssignId()
        {
            while (usedIds.Contains($"s{nextIdNumber}")) nextIdNumber++;
            var id = $"s{nextIdNumber}";
            usedIds.Add(id);
            return id;
        }

        var sounds = new List<SoundEntry>();
        if (values["sounds"] is JsonArray array)
        {
            foreach (var node in array)
            {
                if (node is not JsonObject row) continue;
                var id = row["id"]?.GetValue<string>();
                sounds.Add(new SoundEntry
                {
                    Id = string.IsNullOrEmpty(id) ? AssignId() : id,
                    File = row["file"]?.GetValue<string>() ?? "",
                    Name = row["name"]?.GetValue<string>() ?? "",
                    Volume = row["volume"]?.GetValue<double>() ?? 100,
                    Loop = row["loop"]?.GetValue<bool>() ?? false,
                });
            }
        }

        var data = new SoundSettingsData
        {
            OutputDeviceId = values["outputDevice"]?.GetValue<string>(),
            Sounds = sounds,
            MasterVolume = values["masterVolume"]?.GetValue<double>() ?? 100,
            OverlapMode = values["overlapMode"]?.GetValue<string>() ?? "overlap",
            StopStyle = values["stopStyle"]?.GetValue<string>() ?? "immediate",
            FadeInMs = (int)(values["fadeInMs"]?.GetValue<double>() ?? 0),
            FadeOutMs = (int)(values["fadeOutMs"]?.GetValue<double>() ?? 500),
        };

        data.Save(host.DataDirectory);
        engine.ApplySettings(data);
    }

    public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken) =>
        sourceId == "devices"
            ? Task.FromResult(engine.GetDeviceOptions())
            : Task.FromResult(new OptionsResult([], $"Unknown options source: {sourceId}"));

    public Task<string?> RunCommandAsync(string command, JsonObject values, CancellationToken cancellationToken)
    {
        if (command != "preview") return Task.FromResult<string?>($"Unknown command: {command}");

        var file = values["file"]?.GetValue<string>();
        if (string.IsNullOrEmpty(file)) return Task.FromResult<string?>("Pick a file first.");
        if (!System.IO.File.Exists(file)) return Task.FromResult<string?>("File not found.");

        var volume = values["volume"]?.GetValue<double>() ?? 100;
        return Task.FromResult(engine.TogglePreview(file, volume));
    }
}
