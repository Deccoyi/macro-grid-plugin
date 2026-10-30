using System.Net;
using System.Net.Sockets;

namespace MacroGrid.Plugin.StreamTexts.Tests;

public class OverlayServerTests
{
    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static (OverlayServer Server, HttpClient Client, string Base) Start()
    {
        var port = FreePort();
        var server = new OverlayServer();
        Assert.Null(server.Start(port));
        return (server, new HttpClient { Timeout = TimeSpan.FromSeconds(10) }, $"http://127.0.0.1:{port}");
    }

    [Fact]
    public async Task Serves_the_page_and_the_plain_text_for_a_known_name_only()
    {
        var (server, client, root) = Start();
        using var _ = server;
        server.Publish(new Dictionary<string, string> { ["cpu"] = "CPU 5%", ["My Text"] = "<b>x</b>" });

        Assert.Equal("CPU 5%", await client.GetStringAsync($"{root}/t/cpu.txt"));
        Assert.Equal("<b>x</b>", await client.GetStringAsync($"{root}/t/My%20Text.txt"));

        var page = await client.GetAsync($"{root}/t/cpu");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("textContent", await page.Content.ReadAsStringAsync());
        Assert.True(page.Headers.Contains("Content-Security-Policy"));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{root}/t/other.txt")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{root}/secret")).StatusCode);
    }

    [Fact]
    public async Task Refuses_other_methods_and_foreign_host_names()
    {
        var (server, client, root) = Start();
        using var _ = server;
        server.Publish(new Dictionary<string, string> { ["a"] = "A" });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsync($"{root}/t/a.txt", new StringContent(""))).StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{root}/t/a.txt");
        request.Headers.Host = "evil.example";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task The_event_stream_sends_the_text_and_then_every_change()
    {
        var (server, client, root) = Start();
        using var _ = server;
        server.Publish(new Dictionary<string, string> { ["a"] = "one" });

        using var response = await client.GetAsync($"{root}/t/a/events", HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());

        Assert.Equal("data: \"one\"", await reader.ReadLineAsync());
        await reader.ReadLineAsync();

        server.Publish(new Dictionary<string, string> { ["a"] = "two\nlines" });
        Assert.Equal("data: \"two\\nlines\"", await reader.ReadLineAsync());
    }

    [Fact]
    public void A_busy_port_is_reported_instead_of_thrown()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        try
        {
            using var server = new OverlayServer();
            var error = server.Start(((IPEndPoint)l.LocalEndpoint).Port);
            Assert.NotNull(error);
            Assert.False(server.IsRunning);
        }
        finally { l.Stop(); }
    }

    [Fact]
    public async Task Stop_closes_the_port()
    {
        var (server, client, root) = Start();
        server.Publish(new Dictionary<string, string> { ["a"] = "A" });
        server.Stop();

        await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.GetStringAsync($"{root}/t/a.txt"));
        server.Dispose();
    }
}
