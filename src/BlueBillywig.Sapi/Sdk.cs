using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueBillywig.Sapi.Authentication;
using BlueBillywig.Sapi.Entities;
using BlueBillywig.Sapi.Exceptions;
using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi;

/// <summary>
/// Main entry point for the Blue Billywig SAPI SDK. Provides authenticated access to SAPI
/// entities and handles HTTP communication.
/// </summary>
public sealed class Sdk : IDisposable
{
    private static readonly EntityRegistration[] EntityRegistrations =
    {
        new("mediaclip", sdk => new MediaClip(sdk)),
        new("mediacliplist", sdk => new Playlist(sdk)),
        new("playlist", sdk => new Playlist(sdk)),
        new("channel", sdk => new Channel(sdk)),
        new("playout", sdk => new Playout(sdk)),
        new("subtitle", sdk => new Subtitle(sdk)),
        new("thumbnail", sdk => new Thumbnail(sdk)),
    };

    private readonly IAuthenticator _authenticator;
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly TimeSpan _timeout;
    private readonly object _publicationLock = new();
    private Task<JsonObject>? _publicationData;

    /// <summary>The publication name.</summary>
    public string Publication { get; }

    /// <summary>The base URI used for resolving relative SAPI paths.</summary>
    public string BaseUri { get; }

    /// <summary>The entity register; entities are created lazily and cached.</summary>
    public EntityRegister Entities { get; }

    /// <summary>Creates an SDK instance.</summary>
    /// <param name="publication">The publication name.</param>
    /// <param name="authenticator">The request authenticator.</param>
    /// <param name="options">Optional SDK configuration.</param>
    public Sdk(string publication, IAuthenticator authenticator, SdkOptions? options = null)
    {
        Publication = publication ?? throw new ArgumentNullException(nameof(publication));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        options ??= new SdkOptions();
        BaseUri = options.BaseUri ?? $"https://{publication}.bbvms.com";
        _timeout = options.Timeout;
        if (options.HttpClient is null)
        {
            _http = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
            _ownsHttpClient = true;
        }
        else
        {
            _http = options.HttpClient;
        }
        Entities = new EntityRegister(this, EntityRegistrations);
    }

    /// <summary>Creates an SDK instance configured with RPC token authentication.</summary>
    /// <param name="publication">The publication name.</param>
    /// <param name="tokenId">The RPC token ID.</param>
    /// <param name="sharedSecret">The shared secret for HOTP token generation.</param>
    /// <param name="options">Optional SDK configuration.</param>
    public static Sdk WithRpcTokenAuthentication(string publication, int tokenId, string sharedSecret, SdkOptions? options = null)
        => new(publication, new RpcTokenAuthenticator(tokenId, sharedSecret), options);

    /// <summary>Access the MediaClip entity for CRUD operations on media clips.</summary>
    public MediaClip MediaClip => Entities.Get<MediaClip>("mediaclip")!;

    /// <summary>Access the Playlist entity via the legacy <c>mediacliplist</c> alias.</summary>
    public Playlist MediaClipList => Entities.Get<Playlist>("mediacliplist")!;

    /// <summary>Access the Playlist entity for CRUD operations on playlists.</summary>
    public Playlist Playlist => Entities.Get<Playlist>("playlist")!;

    /// <summary>Access the Channel entity for CRUD operations on channels.</summary>
    public Channel Channel => Entities.Get<Channel>("channel")!;

    /// <summary>Access the Playout entity for CRUD operations on playouts.</summary>
    public Playout Playout => Entities.Get<Playout>("playout")!;

    /// <summary>Access the Subtitle entity for CRUD operations on subtitles.</summary>
    public Subtitle Subtitle => Entities.Get<Subtitle>("subtitle")!;

    /// <summary>Access the Thumbnail entity for thumbnail path resolution.</summary>
    public Thumbnail Thumbnail => Entities.Get<Thumbnail>("thumbnail")!;

    /// <summary>
    /// True when <paramref name="url"/> targets the same origin as the SAPI base URI. Used to gate
    /// auth-header attachment: cross-origin URLs (presigned S3/CloudFront upload and progress URLs)
    /// must never receive the rpctoken, or the credential ends up in CDN access logs. An
    /// unparseable URL is treated as cross-origin (fail closed).
    /// </summary>
    private bool IsSameOrigin(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var target)) return false;
        if (!Uri.TryCreate(BaseUri, UriKind.Absolute, out var origin)) return false;
        return string.Equals(target.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(target.Host, origin.Host, StringComparison.OrdinalIgnoreCase)
            && target.Port == origin.Port;
    }

    /// <summary>
    /// Sends an authenticated HTTP request to the SAPI. Relative paths are resolved against the
    /// SDK's base URI.
    /// </summary>
    /// <param name="method">HTTP method (GET, PUT, DELETE, etc.).</param>
    /// <param name="path">URL path or absolute URL.</param>
    /// <param name="options">Optional query parameters, JSON body, raw body, or extra headers.</param>
    /// <param name="cancellationToken">A caller cancellation token.</param>
    /// <exception cref="SapiConnectionException">on transport failure or timeout.</exception>
    public async Task<SapiResponse> SendRequestAsync(string method, string path, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (method is null) throw new ArgumentNullException(nameof(method));
        if (path is null) throw new ArgumentNullException(nameof(path));
        options ??= new RequestOptions();

        // Resolve URL first: relative paths get resolved against BaseUri, and the resolved
        // origin decides whether auth headers may be attached.
        var url = path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? path
            : BaseUri + path;

        if (options.Query is { Count: > 0 })
        {
            url = QueryParams.Append(url, options.Query);
        }

        // Attach SAPI auth ONLY to same-origin requests. Presigned S3/CloudFront upload +
        // progress URLs are cross-origin; sending the rpctoken there leaks the credential into
        // CDN access logs. SkipAuth can also suppress it on a same-origin request.
        var skipAuth = options.SkipAuth ?? !IsSameOrigin(url);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!skipAuth)
        {
            foreach (var (key, value) in _authenticator.Authenticate()) headers[key] = value;
        }
        if (options.Headers is not null)
        {
            foreach (var (key, value) in options.Headers) headers[key] = value;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new SapiConnectionException($"Network request to {url} failed: invalid URL", new UriFormatException(url));
        }

        using var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), uri);
        if (options.Json is not null)
        {
            var content = new StringContent(JsonSerializer.Serialize(options.Json, SapiJson.Options), Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;
        }
        else if (options.Body is not null)
        {
            request.Content = options.Body;
        }

        foreach (var (key, value) in headers)
        {
            if (!request.Headers.TryAddWithoutValidation(key, value) && request.Content is not null)
            {
                request.Content.Headers.TryAddWithoutValidation(key, value);
            }
        }

        // Per-request timeout (SdkOptions default, overridable; zero disables). Without this a
        // stalled connection hangs the call forever.
        var timeout = options.Timeout ?? _timeout;
        var hasTimeout = timeout > TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan;
        using var cts = hasTimeout ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : null;
        if (cts is not null) cts.CancelAfter(timeout);
        var token = cts?.Token ?? cancellationToken;

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cts is not null && cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new SapiConnectionException($"Request to {url} timed out after {timeout.TotalMilliseconds}ms", ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SapiRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Wrap transport errors (network failure, DNS, reset) in the SDK exception hierarchy
            // instead of letting a raw HttpRequestException/IOException escape.
            throw new SapiConnectionException($"Network request to {url} failed: {ex.Message}", ex);
        }

        using (response)
        {
            var responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }
            foreach (var header in response.Content.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            var bodyText = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            return new SapiResponse(url, method.ToUpperInvariant(), (int)response.StatusCode, response.ReasonPhrase ?? string.Empty, responseHeaders, bodyText);
        }
    }

    /// <summary>
    /// Fetches and caches publication data from the SAPI. Subsequent calls return the cached
    /// result. A failed fetch is not cached, so the next call retries.
    /// </summary>
    public Task<JsonObject> GetPublicationDataAsync(CancellationToken cancellationToken = default)
    {
        lock (_publicationLock)
        {
            if (_publicationData is { IsFaulted: false, IsCanceled: false })
            {
                return _publicationData;
            }
            _publicationData = FetchPublicationDataAsync(cancellationToken);
            return _publicationData;
        }
    }

    private async Task<JsonObject> FetchPublicationDataAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync("GET", "/sapi/publication", null, cancellationToken).ConfigureAwait(false);
        response.AssertOk();
        return response.Json() as JsonObject
            ?? throw new SapiRequestException("Publication data is not a JSON object.", response.StatusCode, response.Body);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient) _http.Dispose();
    }
}
