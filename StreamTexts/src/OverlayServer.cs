using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MacroGrid.Plugin.StreamTexts;

/// <summary>
/// Optional, opt-in HTTP server for browser sources. It listens on the loopback address only (<c>127.0.0.1</c>), answers GET only and
/// serves nothing but the configured texts:
/// <list type="bullet">
/// <item><c>/t/&lt;name&gt;</c> a transparent page that follows the text live over server-sent events; <c>?css=</c> adds styles.</item>
/// <item><c>/t/&lt;name&gt;.txt</c> the current text as plain text.</item>
/// <item><c>/t/&lt;name&gt;/events</c> the event stream itself.</item>
/// </list>
/// The engine pushes new texts in with <see cref="Publish"/>; connected pages are told only when something changed.
/// </summary>
public sealed class OverlayServer : IDisposable
{
    public const int MaxStreams = 16;
    private static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(15);

    private readonly object _gate = new();
    private Dictionary<string, string> _texts = new(StringComparer.OrdinalIgnoreCase);
    private TaskCompletionSource _changed = NewSignal();
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private int _streams;
    private int _port;

    public bool IsRunning
    {
        get { lock (_gate) return _listener is not null; }
    }

    /// <summary>Starts listening on the loopback address. Returns null on success, otherwise a short reason.</summary>
    public string? Start(int port)
    {
        lock (_gate)
        {
            if (_listener is not null && _port == port) return null;
        }
        Stop();

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        try { listener.Start(); }
        catch (Exception ex) when (ex is HttpListenerException or SocketException or ObjectDisposedException)
        {
            listener.Close();
            return $"Cannot listen on port {port}: {ex.Message}";
        }

        var cts = new CancellationTokenSource();
        lock (_gate)
        {
            _listener = listener;
            _cts = cts;
            _port = port;
        }
        _ = Task.Run(() => AcceptLoopAsync(listener, port, cts.Token));
        return null;
    }

    public void Stop()
    {
        HttpListener? listener;
        CancellationTokenSource? cts;
        lock (_gate)
        {
            listener = _listener;
            cts = _cts;
            _listener = null;
            _cts = null;
        }
        if (cts is not null) { cts.Cancel(); cts.Dispose(); }
        try { listener?.Close(); } catch (ObjectDisposedException) { }
    }

    /// <summary>Replaces the served texts. Pages are only woken up when at least one text differs.</summary>
    public void Publish(IReadOnlyDictionary<string, string> texts)
    {
        TaskCompletionSource? toComplete = null;
        lock (_gate)
        {
            var same = _texts.Count == texts.Count;
            if (same)
            {
                foreach (var (name, text) in texts)
                {
                    if (!_texts.TryGetValue(name, out var old) || old != text) { same = false; break; }
                }
            }
            if (same) return;

            _texts = new Dictionary<string, string>(texts, StringComparer.OrdinalIgnoreCase);
            toComplete = _changed;
            _changed = NewSignal();
        }
        toComplete.TrySetResult();
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private async Task AcceptLoopAsync(HttpListener listener, int port, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext context;
            try { context = await listener.GetContextAsync(); }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException) { return; }

            _ = Task.Run(() => HandleAsync(context, port, ct), ct);
        }
    }

    private async Task HandleAsync(HttpListenerContext context, int port, CancellationToken ct)
    {
        var response = context.Response;
        try
        {
            var request = context.Request;
            response.Headers["Cache-Control"] = "no-store";
            response.Headers["X-Content-Type-Options"] = "nosniff";

            // DNS-rebinding guard: only the numeric loopback name is a valid Host.
            if (request.Headers["Host"] != $"127.0.0.1:{port}") { await Reply(response, 400, "text/plain", "Bad host"); return; }
            if (request.HttpMethod != "GET") { await Reply(response, 405, "text/plain", "GET only"); return; }

            var path = request.Url?.AbsolutePath ?? "/";
            if (path == "/")
            {
                await Reply(response, 200, "text/html", IndexPage());
                return;
            }

            if (!path.StartsWith("/t/", StringComparison.Ordinal)) { await Reply(response, 404, "text/plain", "Not found"); return; }
            var rest = Uri.UnescapeDataString(path[3..]);

            var kind = Kind.Page;
            if (rest.EndsWith("/events", StringComparison.Ordinal)) { kind = Kind.Events; rest = rest[..^"/events".Length]; }
            else if (rest.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) { kind = Kind.Text; rest = rest[..^".txt".Length]; }

            string? current;
            lock (_gate) current = _texts.TryGetValue(rest, out var t) ? t : null;
            if (current is null) { await Reply(response, 404, "text/plain", "Not found"); return; }

            switch (kind)
            {
                case Kind.Text:
                    await Reply(response, 200, "text/plain", current);
                    break;
                case Kind.Page:
                    response.Headers["Content-Security-Policy"] =
                        "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'";
                    await Reply(response, 200, "text/html", TextPage);
                    break;
                default:
                    await StreamAsync(response, rest, ct);
                    break;
            }
        }
        catch (Exception ex) when (ex is HttpListenerException or IOException or ObjectDisposedException or OperationCanceledException)
        {
            // The browser source went away mid-response: nothing to do.
        }
        finally
        {
            try { response.Close(); } catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException) { }
        }
    }

    private enum Kind { Page, Text, Events }

    private async Task StreamAsync(HttpListenerResponse response, string name, CancellationToken ct)
    {
        if (Interlocked.Increment(ref _streams) > MaxStreams)
        {
            Interlocked.Decrement(ref _streams);
            await Reply(response, 503, "text/plain", "Too many open pages");
            return;
        }

        try
        {
            response.StatusCode = 200;
            response.ContentType = "text/event-stream";
            response.SendChunked = true;
            var output = response.OutputStream;

            string? last = null;
            while (!ct.IsCancellationRequested)
            {
                string? text;
                Task signal;
                lock (_gate)
                {
                    text = _texts.TryGetValue(name, out var t) ? t : null;
                    signal = _changed.Task;
                }
                if (text is null) return; // the entry was removed: end the stream, the page reconnects and gets a 404

                if (text != last)
                {
                    last = text;
                    await WriteAsync(output, $"data: {JsonSerializer.Serialize(text)}\n\n", ct);
                }

                var finished = await Task.WhenAny(signal, Task.Delay(Heartbeat, ct));
                if (finished != signal) await WriteAsync(output, ": keep-alive\n\n", ct);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _streams);
        }
    }

    private static async Task WriteAsync(Stream output, string chunk, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(chunk);
        await output.WriteAsync(bytes, ct);
        await output.FlushAsync(ct);
    }

    private static async Task Reply(HttpListenerResponse response, int status, string contentType, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        response.StatusCode = status;
        response.ContentType = contentType + "; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }

    private string IndexPage()
    {
        string[] names;
        lock (_gate) names = [.. _texts.Keys.Order(StringComparer.OrdinalIgnoreCase)];
        var sb = new StringBuilder("<!doctype html><meta charset=utf-8><title>Stream Texts</title><h1>Stream Texts</h1><ul>");
        foreach (var name in names)
        {
            var href = "/t/" + Uri.EscapeDataString(name);
            sb.Append("<li><a href=\"").Append(WebUtility.HtmlEncode(href)).Append("\">")
              .Append(WebUtility.HtmlEncode(name)).Append("</a></li>");
        }
        return sb.Append("</ul>").ToString();
    }

    // The text goes in through textContent only, never as markup. "?css=" is applied as a stylesheet of the page itself.
    private const string TextPage = """
        <!doctype html>
        <meta charset="utf-8">
        <title>Stream Texts</title>
        <style>html,body{margin:0;background:transparent}#t{white-space:pre-wrap;font:32px sans-serif;color:#fff}</style>
        <style id="user"></style>
        <div id="t"></div>
        <script>
        var q = new URLSearchParams(location.search).get('css');
        if (q) document.getElementById('user').textContent = q;
        var el = document.getElementById('t');
        var base = location.pathname.replace(/\/$/, '');
        var es = new EventSource(base + '/events');
        es.onmessage = function (e) { el.textContent = JSON.parse(e.data); };
        </script>
        """;

    public void Dispose() => Stop();
}
