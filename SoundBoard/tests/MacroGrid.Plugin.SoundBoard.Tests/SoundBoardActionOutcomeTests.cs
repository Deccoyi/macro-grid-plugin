using System.Text.Json.Nodes;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.SoundBoard.Tests;

/// <summary>What the sound board's actions report when they cannot do their job (a coded outcome, not a silent no-op).</summary>
public sealed class SoundBoardActionOutcomeTests : IDisposable
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "sound-outcome-" + Guid.NewGuid().ToString("N"));
    private static readonly ActionContext Context = new("d", "p", "w", null!);

    public SoundBoardActionOutcomeTests() => Directory.CreateDirectory(_dataDir);

    public void Dispose()
    {
        try { Directory.Delete(_dataDir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    [Fact]
    public async Task Play_without_a_chosen_sound_is_not_configured()
    {
        using var engine = new SoundBoardEngine(new FakePluginHost(_dataDir));

        var outcome = await new SoundBoardPlayAction(engine).ExecuteWithOutcomeAsync(Context, [], CancellationToken.None);

        Assert.Equal(ActionFailureCode.NotConfigured, outcome.Code);
    }

    [Fact]
    public async Task Play_of_a_sound_that_is_gone_is_not_found()
    {
        using var engine = new SoundBoardEngine(new FakePluginHost(_dataDir));

        var outcome = await new SoundBoardPlayAction(engine).ExecuteWithOutcomeAsync(Context, new JsonObject { ["sound"] = "nope" }, CancellationToken.None);

        Assert.Equal(ActionFailureCode.NotFound, outcome.Code);
    }

    [Fact]
    public async Task Play_with_a_missing_file_names_the_sound_and_never_the_path()
    {
        var host = new FakePluginHost(_dataDir);
        using var engine = new SoundBoardEngine(host);
        var missing = Path.Combine(_dataDir, "private-folder", "bell.wav");
        engine.ApplySettings(new SoundBoardSettingsData { Sounds = [new SoundEntry { Id = "s1", Name = "Bell", File = missing }] });

        var outcome = await new SoundBoardPlayAction(engine).ExecuteWithOutcomeAsync(Context, new JsonObject { ["sound"] = "s1" }, CancellationToken.None);

        Assert.Equal(ActionFailureCode.NotFound, outcome.Code);
        Assert.Contains("Bell", outcome.Message);
        Assert.DoesNotContain("private-folder", outcome.Message);
        Assert.Contains(host.Logs, l => l.Contains("private-folder")); // the path still reaches the plugin log
    }

    [Fact]
    public async Task Stop_succeeds_when_nothing_plays_and_needs_a_sound_for_one()
    {
        using var engine = new SoundBoardEngine(new FakePluginHost(_dataDir));
        var stop = new SoundBoardStopAction(engine);

        Assert.Equal(ActionOutcomeKind.Success, (await stop.ExecuteWithOutcomeAsync(Context, [], CancellationToken.None)).Kind);
        Assert.Equal(ActionOutcomeKind.Success, (await stop.ExecuteWithOutcomeAsync(Context, new JsonObject { ["target"] = "one", ["sound"] = "s1" }, CancellationToken.None)).Kind);
        Assert.Equal(ActionFailureCode.NotConfigured, (await stop.ExecuteWithOutcomeAsync(Context, new JsonObject { ["target"] = "one" }, CancellationToken.None)).Code);
    }
}
