using MacroGrid.Plugin.Abstractions;
using System.Text;

namespace MacroGrid.Plugin.StreamTexts.Tests;

public class StreamTextsEngineTests
{
    private static StreamTextsEngine Create(TempFolders dirs, FakePluginHost host, params (string Name, string Template)[] entries)
    {
        var engine = new StreamTextsEngine(host);
        engine.ApplySettings(new StreamTextsSettingsData
        {
            OutputFolder = dirs.Output,
            Texts = [.. entries.Select(e => new TextEntry { Name = e.Name, Template = e.Template })],
        }.Normalized());
        return engine;
    }

    [Fact]
    public void Writes_a_file_without_a_BOM_and_only_rewrites_when_the_text_changes()
    {
        using var dirs = new TempFolders();
        var host = new FakePluginHost(dirs.Data);
        var store = new FakeVariableStore();
        store.Set("system.cpu", 41.6);
        using var engine = Create(dirs, host, ("cpu", "CPU {system.cpu|0}%"));

        engine.Tick(store);
        var path = Path.Combine(dirs.Output, "cpu.txt");
        Assert.Equal("CPU 42%", File.ReadAllText(path));
        Assert.False(File.ReadAllBytes(path).AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));

        var stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, stamp);
        engine.Tick(store); // same value: file must not be touched
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(path));

        store.Set("system.cpu", 7.0);
        engine.Tick(store);
        Assert.Equal("CPU 7%", File.ReadAllText(path));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Removing_an_entry_deletes_only_its_own_file()
    {
        using var dirs = new TempFolders();
        var host = new FakePluginHost(dirs.Data);
        var store = new FakeVariableStore();
        using var engine = Create(dirs, host, ("a", "A"), ("b", "B"));
        engine.Tick(store);
        File.WriteAllText(Path.Combine(dirs.Output, "mine.txt"), "not ours");

        engine.ApplySettings(new StreamTextsSettingsData { OutputFolder = dirs.Output, Texts = [new TextEntry { Name = "a", Template = "A" }] }.Normalized());
        engine.Tick(store);

        Assert.True(File.Exists(Path.Combine(dirs.Output, "a.txt")));
        Assert.False(File.Exists(Path.Combine(dirs.Output, "b.txt")));
        Assert.True(File.Exists(Path.Combine(dirs.Output, "mine.txt")));
    }

    [Fact]
    public void Changing_the_folder_removes_the_old_files_after_a_restart_too()
    {
        using var dirs = new TempFolders();
        var host = new FakePluginHost(dirs.Data);
        var store = new FakeVariableStore();
        var saved = new StreamTextsSettingsData { OutputFolder = dirs.Output, Texts = [new TextEntry { Name = "a", Template = "A" }] };
        saved.Save(dirs.Data);
        using (var first = new StreamTextsEngine(host)) first.Tick(store);

        // A new engine over the same data folder = the plugin loaded again; the settings now point elsewhere.
        var other = Path.Combine(dirs.Root, "other");
        saved.OutputFolder = other;
        saved.Save(dirs.Data);
        using var second = new StreamTextsEngine(host);
        second.Tick(store);

        Assert.False(File.Exists(Path.Combine(dirs.Output, "a.txt")));
        Assert.True(File.Exists(Path.Combine(other, "a.txt")));
    }

    [Fact]
    public void A_deleted_file_is_written_again()
    {
        using var dirs = new TempFolders();
        var host = new FakePluginHost(dirs.Data);
        var store = new FakeVariableStore();
        using var engine = Create(dirs, host, ("a", "A"));
        engine.Tick(store);
        var path = Path.Combine(dirs.Output, "a.txt");
        File.Delete(path);

        // The existence check runs every 10 s; re-applying the same settings forgets what was written, which is what it falls back to.
        engine.ApplySettings(engine.Settings);
        engine.Tick(store);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void A_locked_file_is_retried_on_the_next_tick_and_reported()
    {
        using var dirs = new TempFolders();
        var host = new FakePluginHost(dirs.Data);
        var store = new FakeVariableStore();
        using var engine = Create(dirs, host, ("a", "one"));
        engine.Tick(store);
        var path = Path.Combine(dirs.Output, "a.txt");

        store.Set("x", 1);
        engine.ApplySettings(new StreamTextsSettingsData { OutputFolder = dirs.Output, Texts = [new TextEntry { Name = "a", Template = "two" }] }.Normalized());
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            engine.Tick(store);
            Assert.Equal(StatusLevel.Warning, host.Status.LastLevel);
        }

        engine.Tick(store);
        Assert.Equal("two", File.ReadAllText(path));
        Assert.Equal(StatusLevel.Ok, host.Status.LastLevel);
    }

    [Fact]
    public void Turning_file_writing_off_deletes_the_files_it_made()
    {
        using var dirs = new TempFolders();
        var host = new FakePluginHost(dirs.Data);
        var store = new FakeVariableStore();
        using var engine = Create(dirs, host, ("a", "A"));
        engine.Tick(store);
        Assert.True(File.Exists(Path.Combine(dirs.Output, "a.txt")));

        engine.ApplySettings(new StreamTextsSettingsData
        {
            OutputFolder = dirs.Output,
            WriteFiles = false,
            Texts = [new TextEntry { Name = "a", Template = "A" }],
        }.Normalized());
        engine.Tick(store);

        Assert.False(File.Exists(Path.Combine(dirs.Output, "a.txt")));
    }

    [Fact]
    public async Task The_overlay_follows_the_settings_and_serves_the_rendered_text()
    {
        using var dirs = new TempFolders();
        var host = new FakePluginHost(dirs.Data);
        var store = new FakeVariableStore();
        store.Set("v", 3.0);
        var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        l.Start();
        var port = ((System.Net.IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();

        using var engine = new StreamTextsEngine(host);
        engine.ApplySettings(new StreamTextsSettingsData
        {
            OutputFolder = dirs.Output,
            WriteFiles = false,
            OverlayEnabled = true,
            OverlayPort = port,
            Texts = [new TextEntry { Name = "a", Template = "V={v|0}" }],
        }.Normalized());
        engine.Tick(store);

        Assert.True(engine.OverlayRunning);
        using var client = new HttpClient();
        Assert.Equal("V=3", await client.GetStringAsync($"http://127.0.0.1:{port}/t/a.txt"));

        engine.ApplySettings(new StreamTextsSettingsData { OutputFolder = dirs.Output, Texts = [new TextEntry { Name = "a", Template = "A" }] }.Normalized());
        engine.Tick(store);
        Assert.False(engine.OverlayRunning);
    }

    [Fact]
    public void A_relative_folder_is_refused()
    {
        using var dirs = new TempFolders();
        var host = new FakePluginHost(dirs.Data);
        using var engine = new StreamTextsEngine(host);
        engine.ApplySettings(new StreamTextsSettingsData { OutputFolder = "relative\\dir", Texts = [new TextEntry { Name = "a", Template = "A" }] }.Normalized());

        engine.Tick(new FakeVariableStore());

        Assert.Equal(StatusLevel.Warning, host.Status.LastLevel);
        Assert.False(Directory.Exists("relative"));
    }

    [Theory]
    [InlineData("cpu", true)]
    [InlineData("My Stats_1-a", true)]
    [InlineData("Çay", true)]
    [InlineData("", false)]
    [InlineData(" lead", false)]
    [InlineData("..", false)]
    [InlineData("a/b", false)]
    [InlineData("a\\b", false)]
    [InlineData("a.txt", false)]
    [InlineData("con", false)]
    [InlineData("NUL", false)]
    public void File_name_validation(string name, bool valid) =>
        Assert.Equal(valid, StreamTextsSettingsData.IsValidName(name));

    [Fact]
    public void Normalized_drops_bad_and_duplicate_rows_and_clamps_the_interval()
    {
        var data = new StreamTextsSettingsData
        {
            IntervalMs = 5,
            Texts =
            [
                new TextEntry { Name = "ok", Template = "1" },
                new TextEntry { Name = "OK", Template = "2" },
                new TextEntry { Name = "../x", Template = "3" },
            ],
        }.Normalized();

        Assert.Equal(StreamTextsSettingsData.MinIntervalMs, data.IntervalMs);
        Assert.Single(data.Texts);
        Assert.Equal("1", data.Texts[0].Template);
    }
}
