using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BlueBillywig.Sapi.Tests;

public class RedirectTests
{
    /// <summary>Minimal loopback HTTP/1.1 server (BCL only) that answers every request with a fixed response.</summary>
    private sealed class LoopbackServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();
        private int _hits;
        private string _requestHeaders = string.Empty;

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public int Hits => Volatile.Read(ref _hits);
        public string RequestHeaders => _requestHeaders;

        public LoopbackServer(Func<string> responseFactory)
        {
            _listener.Start();
            _ = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    TcpClient client;
                    try { client = await _listener.AcceptTcpClientAsync(_cts.Token); }
                    catch { return; }
                    _ = Task.Run(async () =>
                    {
                        using var c = client;
                        var stream = c.GetStream();
                        var buf = new byte[8192];
                        var sb = new StringBuilder();
                        while (!sb.ToString().Contains("\r\n\r\n"))
                        {
                            var n = await stream.ReadAsync(buf);
                            if (n == 0) return;
                            sb.Append(Encoding.ASCII.GetString(buf, 0, n));
                        }
                        Interlocked.Increment(ref _hits);
                        _requestHeaders = sb.ToString();
                        var bytes = Encoding.ASCII.GetBytes(responseFactory());
                        await stream.WriteAsync(bytes);
                    });
                }
            });
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }

    [Fact]
    public async Task SdkOwnedClientDoesNotFollowRedirectsToAnotherOrigin()
    {
        using var target = new LoopbackServer(() => "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        using var origin = new LoopbackServer(() =>
            $"HTTP/1.1 302 Found\r\nLocation: http://127.0.0.1:{target.Port}/stolen\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");

        using var sdk = Sdk.WithRpcTokenAuthentication(
            "my-publication", 1, "secret", new SdkOptions { BaseUri = $"http://127.0.0.1:{origin.Port}" });

        var response = await sdk.SendRequestAsync("GET", "/sapi/publication");

        Assert.Equal(302, response.StatusCode);
        Assert.False(response.Ok);
        Assert.Equal(1, origin.Hits);
        Assert.Contains("rpctoken", origin.RequestHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, target.Hits);
    }
}
