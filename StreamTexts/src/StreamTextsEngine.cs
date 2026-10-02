using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MacroGrid.Plugin.Abstractions;

namespace MacroGrid.Plugin.StreamTexts;

/// <summary>
/// Keeps one UTF-8 text file per configured entry in step with the variables its template uses. Cheap on purpose: one loop on
/// the plugin's own provider task, rendering a handful of short templates every <see cref="StreamTextsSettingsData.IntervalMs"/>,
/// and a file is touched only when its rendered text actually changed. With no entries the loop sleeps until settings change.
/// <para>A file is written to <c>name.txt.tmp</c> first and then moved over the real one, so a streaming app never reads a
/// half-written file. If the move fails (the app has the file open at that very moment) the text is simply tried again on the
/// next tick. The files this plugin created are remembered in <c>managed.json</c>, so a removed or renamed entry (or a changed
/// folder) deletes only its own old file, never anything else.</para>
/// </summary>
public sealed class StreamTextsEngine : IVariableProvider, IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly TimeSpan ExistenceCheckInterval = TimeSpan.FromSeconds(10);

    private readonly IPluginHost _host;
    private readonly Lock _lock = new();
    /// <summary>Serializes everything that writes files or touches <c>_managed</c>: the provider loop's <see cref="Tick"/> and the
    /// settings thread's <see cref="RemoveOrphans"/> (never held together with <c>_lock</c> in the other order).</summary>
    private readonly Lock _ioLock = new();
    private readonly SemaphoreSlim _wake = new(0, int.MaxValue);
    private readonly IPluginStatusItem _status;
    private readonly Dictionary<string, string> _lastWritten = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _managedPath;

    private StreamTextsSettingsData _settings;
    private ManagedFiles _managed;
    private DateTime _nextExistenceCheckUtc = DateTime.UtcNow + ExistenceCheckInterval;
    private string? _lastStatusText;
    private readonly OverlayServer _overlay = new();
    private string? _overlayError;
    private int _overlayFailedPort = -1;

    /// <summary>True while the browser source server is listening.</summary>
    public bool OverlayRunning => _overlay.IsRunning;

    /// <summary>The files this plugin created: the folder they are in and their names (without ".txt").</summary>
    private sealed class ManagedFiles
    {
        public string? Folder { get; set; }
        public List<string> Names { get; set; } = [];
    }

    public StreamTextsEngine(IPluginHost host)
    {
        _host = host;
        _settings = StreamTextsSettingsData.LoadOrCreate(host.DataDirectory);
        _managedPath = Path.Combine(host.DataDirectory, "managed.json");
        _managed = LoadManaged();
        _status = host.CreateStatusItem("files");
        _status.Update("Stream Texts starting", StatusLevel.Ok);
        RemoveOrphans();
    }

    public StreamTextsSettingsData Settings
    {
        get { lock (_lock) return _settings; }
    }

    public void ApplySettings(StreamTextsSettingsData data)
    {
        lock (_lock)
        {
            _settings = data;
            _lastWritten.Clear();
            _overlayFailedPort = -1;
        }
        RemoveOrphans();
        Wake();
    }

    public async Task RunAsync(IVariableStore store, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Tick(store);

                StreamTextsSettingsData settings;
                lock (_lock) settings = _settings;
                // Nothing to keep up to date: sleep until the settings change instead of waking up for nothing.
                var wait = settings.Texts.Count == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(settings.IntervalMs);
                await _wake.WaitAsync(wait, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Plugin unloading — normal shutdown of the provider loop. The files keep their last text.
        }
    }

    /// <summary>One pass: render every entry and write the ones whose text changed. Public so tests can drive it directly.</summary>
    public void Tick(IVariableStore store)
    {
        lock (_ioLock) TickCore(store);
    }

    private void TickCore(IVariableStore store)
    {
        StreamTextsSettingsData settings;
        lock (_lock) settings = _settings;

        var rendered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in settings.Texts)
            rendered[entry.Name] = TextTemplate.Render(entry.Template, store);

        var overlayError = SyncOverlay(settings, rendered);

        if (settings.Texts.Count == 0)
        {
            SetStatus("No text files set up", StatusLevel.Ok);
            return;
        }

        if (!settings.WriteFiles)
        {
            if (overlayError is not null) SetStatus("Browser source not running", StatusLevel.Warning, overlayError);
            else SetStatus(settings.OverlayEnabled ? $"Browser source on port {settings.OverlayPort}" : "Nothing is turned on", StatusLevel.Ok);
            return;
        }

        var folder = settings.ResolvedFolder;
        if (!Path.IsPathRooted(folder))
        {
            SetStatus("The folder must be a full path", StatusLevel.Warning);
            return;
        }

        try { Directory.CreateDirectory(folder); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            SetStatus("Cannot use the folder", StatusLevel.Warning, ex.Message);
            return;
        }

        var existenceCheck = DateTime.UtcNow >= _nextExistenceCheckUtc;
        if (existenceCheck) _nextExistenceCheckUtc = DateTime.UtcNow + ExistenceCheckInterval;

        // Files from an older folder that could not be deleted yet stay tracked there; only retarget an empty list.
        var managedChanged = false;
        if (_managed.Names.Count == 0 && !string.Equals(_managed.Folder, folder, StringComparison.OrdinalIgnoreCase))
        {
            _managed.Folder = folder;
            managedChanged = true;
        }

        var failed = 0;
        string? firstError = null;
        foreach (var entry in settings.Texts)
        {
            var text = rendered[entry.Name];
            var path = settings.FilePath(entry.Name);

            lock (_lock)
            {
                if (existenceCheck && _lastWritten.ContainsKey(entry.Name) && !File.Exists(path))
                    _lastWritten.Remove(entry.Name); // someone deleted it: write it again
                if (_lastWritten.TryGetValue(entry.Name, out var last) && last == text) continue;
            }

            try
            {
                WriteAtomically(path, text);
                lock (_lock) _lastWritten[entry.Name] = text;
                if (string.Equals(_managed.Folder, folder, StringComparison.OrdinalIgnoreCase)
                    && !_managed.Names.Contains(entry.Name, StringComparer.OrdinalIgnoreCase))
                {
                    _managed.Names.Add(entry.Name);
                    managedChanged = true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failed++;
                firstError ??= ex.Message;
            }
        }

        if (managedChanged) SaveManaged();

        if (failed > 0) SetStatus($"{failed} text file(s) could not be written", StatusLevel.Warning, firstError);
        else if (overlayError is not null) SetStatus("Browser source not running", StatusLevel.Warning, overlayError);
        else SetStatus($"{settings.Texts.Count} text file(s) up to date", StatusLevel.Ok, folder);
    }

    /// <summary>Starts, stops or feeds the browser source server to match the settings. Returns why it is not running, if it should be.</summary>
    private string? SyncOverlay(StreamTextsSettingsData settings, Dictionary<string, string> rendered)
    {
        if (!settings.OverlayEnabled)
        {
            _overlay.Stop();
            _overlayError = null;
            _overlayFailedPort = -1;
            return null;
        }

        // Start is a no-op on the port it already serves; a port that failed is retried only after the next settings change.
        if (_overlayFailedPort != settings.OverlayPort)
        {
            _overlayError = _overlay.Start(settings.OverlayPort);
            _overlayFailedPort = _overlayError is null ? -1 : settings.OverlayPort;
            if (_overlayError is not null) _host.Log($"Stream Texts: {_overlayError}");
        }

        if (_overlay.IsRunning) _overlay.Publish(rendered);
        return _overlayError;
    }

    /// <summary>Opens the output folder in Explorer. Returns a message to show, or null on success.</summary>
    public string? OpenFolder(string folder)
    {
        if (!Path.IsPathRooted(folder)) return "The folder must be a full path.";
        try
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or ArgumentException)
        {
            return $"Cannot open the folder: {ex.Message}";
        }
    }

    private static void WriteAtomically(string path, string text)
    {
        var temp = path + ".tmp";
        try
        {
            File.WriteAllBytes(temp, Utf8NoBom.GetBytes(text));
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    /// <summary>Deletes the files this plugin created earlier that no entry (or folder) wants any more.</summary>
    private void RemoveOrphans()
    {
        lock (_ioLock) RemoveOrphansCore();
    }

    private void RemoveOrphansCore()
    {
        StreamTextsSettingsData settings;
        lock (_lock) settings = _settings;

        var folder = settings.ResolvedFolder;
        var sameFolder = string.Equals(_managed.Folder, folder, StringComparison.OrdinalIgnoreCase);
        var wanted = sameFolder && settings.WriteFiles
            ? new HashSet<string>(settings.Texts.Select(t => t.Name), StringComparer.OrdinalIgnoreCase)
            : [];

        var kept = new List<string>();
        foreach (var name in _managed.Names)
        {
            if (wanted.Contains(name)) { kept.Add(name); continue; }
            if (_managed.Folder is null || !Path.IsPathRooted(_managed.Folder) || !StreamTextsSettingsData.IsValidName(name)) continue;
            try { File.Delete(Path.Combine(_managed.Folder, name + ".txt")); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                kept.Add(name); // try again at the next settings change
            }
        }

        var changed = kept.Count != _managed.Names.Count;
        _managed = new ManagedFiles { Folder = _managed.Folder, Names = kept };
        if (changed) SaveManaged();
    }

    private ManagedFiles LoadManaged()
    {
        try
        {
            if (File.Exists(_managedPath))
                return JsonSerializer.Deserialize<ManagedFiles>(File.ReadAllText(_managedPath)) ?? new ManagedFiles();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException) { }
        return new ManagedFiles();
    }

    private void SaveManaged()
    {
        try { File.WriteAllText(_managedPath, JsonSerializer.Serialize(_managed)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _host.Log($"Stream Texts: could not save managed.json: {ex.Message}");
        }
    }

    private void SetStatus(string text, StatusLevel level, string? tooltip = null)
    {
        if (text == _lastStatusText) return;
        _lastStatusText = text;
        _status.Update(text, level, tooltip: tooltip);
    }

    private void Wake()
    {
        if (_wake.CurrentCount == 0) _wake.Release();
    }

    public void Dispose()
    {
        _overlay.Dispose();
        _wake.Dispose();
    }
}
