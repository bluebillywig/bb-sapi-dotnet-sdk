using System.Net;

namespace BlueBillywig.Sapi.Tests.Helpers;

public sealed class MockResponse
{
    public int Status { get; init; } = 200;
    public string? StatusText { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    public string? Body { get; init; }
}

public sealed class RecordedCall
{
    public required string Url { get; init; }
    public required string Method { get; init; }
    public required Dictionary<string, string> Headers { get; init; }
    public byte[]? Body { get; init; }
    public bool CanBeCanceled { get; init; }

    public string? BodyText => Body is null ? null : System.Text.Encoding.UTF8.GetString(Body);
    public string? Header(string name) => Headers.TryGetValue(name, out var v) ? v : null;
}

/// <summary>
/// Returns pre-configured responses in FIFO order and records every call. The .NET counterpart of
/// the Node SDK's createMockFetch helper.
/// </summary>
public sealed class MockHttpHandler : HttpMessageHandler
{
    private readonly Queue<MockResponse> _queue;
    private readonly object _lock = new();

    public List<RecordedCall> Calls { get; } = new();

    public MockHttpHandler(params MockResponse[] responses)
    {
        _queue = new Queue<MockResponse>(responses);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in request.Headers) headers[h.Key] = string.Join(", ", h.Value);
        byte[]? body = null;
        if (request.Content is not null)
        {
            // Content-Length is computed lazily by HttpContent; a real handler asks for it before
            // writing the body, so ask for it here too.
            var contentLength = request.Content.Headers.ContentLength;
            foreach (var h in request.Content.Headers) headers[h.Key] = string.Join(", ", h.Value);
            if (contentLength is not null) headers["Content-Length"] = contentLength.Value.ToString();
            body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        MockResponse? mock;
        int callNumber;
        lock (_lock)
        {
            Calls.Add(new RecordedCall
            {
                Url = request.RequestUri!.AbsoluteUri,
                Method = request.Method.Method,
                Headers = headers,
                Body = body,
                CanBeCanceled = cancellationToken.CanBeCanceled,
            });
            callNumber = Calls.Count;
            _queue.TryDequeue(out mock);
        }

        if (mock is null)
        {
            throw new InvalidOperationException($"No more mock responses available (call #{callNumber})");
        }

        var response = new HttpResponseMessage((HttpStatusCode)mock.Status)
        {
            ReasonPhrase = mock.StatusText ?? string.Empty,
            Content = new StringContent(mock.Body ?? string.Empty),
        };
        response.Content.Headers.ContentType = null;
        foreach (var (key, value) in mock.Headers ?? new Dictionary<string, string>())
        {
            if (!response.Headers.TryAddWithoutValidation(key, value))
            {
                response.Content.Headers.TryAddWithoutValidation(key, value);
            }
        }
        return response;
    }
}
