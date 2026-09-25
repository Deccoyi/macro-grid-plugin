using System.Text.Json.Nodes;

namespace MacroGrid.Plugin.SoundBoard.Tests;

public sealed class SoundBoardSettingsTests : IDisposable
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "sound-tests-" + Guid.NewGuid().ToString("N"));
    private readonly SoundBoardEngine _engine;
    private readonly SoundBoardSettingsPage _page;

    public SoundBoardSettingsTests()
    {
        Directory.CreateDirectory(_dataDir);
        _engine = new SoundBoardEngine(new FakePluginHost(_dataDir));
        _page = new SoundBoardSettingsPage(new FakePluginHost(_dataDir), _engine);
    }

    public void Dispose()
    {
        _engine.Dispose();
        try { Directory.Delete(_dataDir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static JsonObject Row(string? id, string file, string name, double volume = 100, bool loop = false)
    {
        var row = new JsonObject { ["file"] = file, ["name"] = name, ["volume"] = volume, ["loop"] = loop };
        if (id is not null) row["id"] = id;
        return row;
    }

    [Fact]
    public void Save_assigns_a_permanent_id_to_a_new_row()
    {
        _page.Save(new JsonObject { ["sounds"] = new JsonArray { Row(null, "a.wav", "Alarm") } });

        var saved = Assert.Single(_engine.Settings.Sounds);
        Assert.Equal("s1", saved.Id);
    }

    [Fact]
    public void Save_gives_each_new_row_in_the_same_save_a_distinct_id()
    {
        _page.Save(new JsonObject
        {
            ["sounds"] = new JsonArray { Row(null, "a.wav", "A"), Row(null, "b.wav", "B"), Row(null, "c.wav", "C") },
        });

        var ids = _engine.Settings.Sounds.Select(s => s.Id).ToList();
        Assert.Equal(["s1", "s2", "s3"], ids);
    }

    [Fact]
    public void Save_never_reassigns_the_id_of_an_existing_row_across_rename_and_reorder()
    {
        _page.Save(new JsonObject { ["sounds"] = new JsonArray { Row(null, "a.wav", "Alarm"), Row(null, "b.wav", "Bell") } });
        var firstId = _engine.Settings.Sounds.First(s => s.Name == "Alarm").Id;
        var secondId = _engine.Settings.Sounds.First(s => s.Name == "Bell").Id;

        // Reordered (Bell first) and Alarm renamed — both keep their id.
        _page.Save(new JsonObject
        {
            ["sounds"] = new JsonArray { Row(secondId, "b.wav", "Bell"), Row(firstId, "a.wav", "Air raid") },
        });

        Assert.Equal(firstId, _engine.Settings.Sounds.Single(s => s.Name == "Air raid").Id);
        Assert.Equal(secondId, _engine.Settings.Sounds.Single(s => s.Name == "Bell").Id);
    }

    [Fact]
    public void Save_gives_a_new_row_an_id_that_does_not_collide_with_a_kept_explicit_one()
    {
        _page.Save(new JsonObject
        {
            // "s1" already taken explicitly; a fresh row must not also become "s1".
            ["sounds"] = new JsonArray { Row("s1", "a.wav", "Alarm"), Row(null, "b.wav", "Bell") },
        });

        var ids = _engine.Settings.Sounds.Select(s => s.Id).ToList();
        Assert.Equal(["s1", "s2"], ids);
    }

    [Fact]
    public void Load_reports_missing_true_for_a_file_that_does_not_exist()
    {
        _page.Save(new JsonObject { ["sounds"] = new JsonArray { Row(null, Path.Combine(_dataDir, "does-not-exist.wav"), "Gone") } });

        var sounds = Assert.IsType<JsonArray>(_page.Load()["sounds"]);
        Assert.True(Assert.IsType<JsonObject>(sounds[0])["missing"]!.GetValue<bool>());
    }

    [Fact]
    public void Load_reports_missing_false_for_a_file_that_exists()
    {
        var file = TestWav.CreateFullScale(_dataDir, TimeSpan.FromMilliseconds(50));
        _page.Save(new JsonObject { ["sounds"] = new JsonArray { Row(null, file, "Here") } });

        var sounds = Assert.IsType<JsonArray>(_page.Load()["sounds"]);
        Assert.False(Assert.IsType<JsonObject>(sounds[0])["missing"]!.GetValue<bool>());
    }

    [Fact]
    public void Save_and_Load_round_trip_every_field()
    {
        _page.Save(new JsonObject
        {
            ["outputDevice"] = "device-123",
            ["sounds"] = new JsonArray { Row(null, "a.wav", "Alarm", volume: 42, loop: true) },
            ["masterVolume"] = 55.0,
            ["overlapMode"] = "cut",
            ["stopStyle"] = "fade",
            ["fadeInMs"] = 100.0,
            ["fadeOutMs"] = 750.0,
        });

        var loaded = _page.Load();
        Assert.Equal("device-123", loaded["outputDevice"]!.GetValue<string>());
        Assert.Equal(55, loaded["masterVolume"]!.GetValue<double>());
        Assert.Equal("cut", loaded["overlapMode"]!.GetValue<string>());
        Assert.Equal("fade", loaded["stopStyle"]!.GetValue<string>());
        Assert.Equal(100, loaded["fadeInMs"]!.GetValue<double>());
        Assert.Equal(750, loaded["fadeOutMs"]!.GetValue<double>());

        var row = Assert.IsType<JsonObject>(Assert.IsType<JsonArray>(loaded["sounds"])[0]);
        Assert.Equal("Alarm", row["name"]!.GetValue<string>());
        Assert.Equal(42, row["volume"]!.GetValue<double>());
        Assert.True(row["loop"]!.GetValue<bool>());
    }

    [Fact]
    public void Save_persists_across_a_fresh_engine_instance()
    {
        _page.Save(new JsonObject { ["sounds"] = new JsonArray { Row(null, "a.wav", "Alarm") }, ["masterVolume"] = 33.0 });

        using var reloaded = new SoundBoardEngine(new FakePluginHost(_dataDir));
        Assert.Equal(33, reloaded.Settings.MasterVolume);
        Assert.Equal("Alarm", reloaded.Settings.Sounds.Single().Name);
    }
}
