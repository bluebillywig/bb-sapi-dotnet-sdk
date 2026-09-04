using System.Net.Http;
using BlueBillywig.Sapi.Authentication;
using BlueBillywig.Sapi.Exceptions;
using BlueBillywig.Sapi.Tests.Helpers;
using static BlueBillywig.Sapi.Tests.Helpers.TestSdk;

namespace BlueBillywig.Sapi.Tests;

public class SdkTests
{
    [Fact]
    public async Task AddsRpctokenHeaderWithRpcTokenAuthentication()
    {
        var (sdk, handler) = CreateWithRpcToken(Ok());

        await sdk.SendRequestAsync("GET", "/sapi/test-method");

        Assert.Single(handler.Calls);
        Assert.Matches("^1-.+", handler.Calls[0].Header("rpctoken"));
    }

    [Fact]
    public async Task ReturnsSapiResponse()
    {
        var (sdk, _) = Create(Ok());
        var response = await sdk.SendRequestAsync("GET", "/sapi/test-method");
        response.AssertOk();
    }

    [Fact]
    public async Task Handles404()
    {
        var (sdk, _) = Create(new MockResponse { Status = 404, StatusText = "Not Found" });
        var response = await sdk.SendRequestAsync("GET", "/sapi/test-method");
        var ex = Assert.Throws<SapiClientErrorException>(() => response.AssertOk());
        Assert.Equal("Not Found", ex.Message);
    }

    [Fact]
    public async Task Handles500()
    {
        var (sdk, _) = Create(new MockResponse { Status = 500, StatusText = "Internal Server Error" });
        var response = await sdk.SendRequestAsync("GET", "/sapi/test-method");
        Assert.Throws<SapiServerErrorException>(() => response.AssertOk());
    }

    [Fact]
    public async Task ResolvesRelativeUrisAgainstBaseUri()
    {
        var (sdk, handler) = Create(Ok());
        await sdk.SendRequestAsync("GET", "/sapi/test-method");
        Assert.Equal("https://my-publication.bbvms.com/sapi/test-method", handler.Calls[0].Url);
    }

    [Fact]
    public async Task DoesNotModifyAbsoluteUris()
    {
        var (sdk, handler) = Create(Ok());
        await sdk.SendRequestAsync("GET", "https://www.bluebillywig.com/");
        Assert.Equal("https://www.bluebillywig.com/", handler.Calls[0].Url);
    }

    [Fact]
    public async Task GetsAndCachesPublicationData()
    {
        var (sdk, handler) = Create(Ok("{\"name\":\"my-publication\"}"));

        var data = await sdk.GetPublicationDataAsync();
        await sdk.GetPublicationDataAsync();

        Assert.Equal("my-publication", data["name"]!.ToString());
        Assert.Single(handler.Calls);
        Assert.Equal("https://my-publication.bbvms.com/sapi/publication", handler.Calls[0].Url);
    }

    [Fact]
    public async Task DoesNotCacheFailedPublicationFetch()
    {
        var (sdk, handler) = Create(new MockResponse { Status = 500 }, Ok("{\"name\":\"p\"}"));

        await Assert.ThrowsAsync<SapiServerErrorException>(() => sdk.GetPublicationDataAsync());
        var data = await sdk.GetPublicationDataAsync();

        Assert.Equal("p", data["name"]!.ToString());
        Assert.Equal(2, handler.Calls.Count);
    }

    [Fact]
    public async Task SendsJsonBody()
    {
        var (sdk, handler) = Create(Ok());
        await sdk.SendRequestAsync("PUT", "/sapi/mediaclip", new RequestOptions { Json = new { title = "Test" } });
        Assert.Equal("{\"title\":\"Test\"}", handler.Calls[0].BodyText);
        Assert.Equal("application/json", handler.Calls[0].Header("Content-Type"));
    }

    [Fact]
    public async Task SendsQueryParams()
    {
        var (sdk, handler) = Create(Ok());
        await sdk.SendRequestAsync("GET", "/sapi/mediaclip", new RequestOptions { Query = new Dictionary<string, string> { ["limit"] = "15", ["offset"] = "0" } });
        Assert.Equal("15", Query(handler.Calls[0].Url, "limit"));
        Assert.Equal("0", Query(handler.Calls[0].Url, "offset"));
    }

    [Fact]
    public async Task ExtraHeadersOverrideAuthHeaders()
    {
        var (sdk, handler) = CreateWithRpcToken(Ok());
        await sdk.SendRequestAsync("GET", "/sapi/x", new RequestOptions { Headers = new Dictionary<string, string> { ["rpctoken"] = "override", ["X-Custom"] = "1" } });
        Assert.Equal("override", handler.Calls[0].Header("rpctoken"));
        Assert.Equal("1", handler.Calls[0].Header("X-Custom"));
    }

    [Fact]
    public void ExposesEntitiesTypedAndCached()
    {
        var (sdk, _) = Create();
        Assert.IsType<global::BlueBillywig.Sapi.Entities.MediaClip>(sdk.MediaClip);
        Assert.IsType<global::BlueBillywig.Sapi.Entities.Playlist>(sdk.Playlist);
        Assert.IsType<global::BlueBillywig.Sapi.Entities.Playlist>(sdk.MediaClipList);
        Assert.IsType<global::BlueBillywig.Sapi.Entities.Channel>(sdk.Channel);
        Assert.IsType<global::BlueBillywig.Sapi.Entities.Playout>(sdk.Playout);
        Assert.IsType<global::BlueBillywig.Sapi.Entities.Subtitle>(sdk.Subtitle);
        Assert.IsType<global::BlueBillywig.Sapi.Entities.Thumbnail>(sdk.Thumbnail);
        Assert.Same(sdk.MediaClip, sdk.MediaClip);
        Assert.Same(sdk.Thumbnail, sdk.Thumbnail);
        Assert.Same(sdk, sdk.MediaClip.Sdk);
    }

    [Fact]
    public void EntityRegisterByName()
    {
        var (sdk, _) = Create();
        Assert.True(sdk.Entities.Contains("mediaclip"));
        Assert.False(sdk.Entities.Contains("nonexistent"));
        Assert.Contains("mediaclip", sdk.Entities.Names);
        Assert.Contains("playlist", sdk.Entities.Names);
        Assert.Contains("thumbnail", sdk.Entities.Names);
        Assert.Null(sdk.Entities.Get("nonexistent"));
        Assert.Null(sdk.Entities["nonexistent"]);
        Assert.Same(sdk.MediaClip, sdk.Entities["mediaclip"]);
        Assert.IsType<global::BlueBillywig.Sapi.Entities.Playlist>(sdk.Entities.Get<global::BlueBillywig.Sapi.Entities.Playlist>("mediacliplist"));
    }
}

public class SdkRequestHardeningTests
{
    private const string S3Url = "https://my-bucket.s3.amazonaws.com/upload?X-Amz-Signature=abc";

    private sealed class CountingAuthenticator : IAuthenticator
    {
        public int Calls;
        public IReadOnlyDictionary<string, string> Authenticate()
        {
            Calls++;
            return new Dictionary<string, string> { ["rpctoken"] = "1-token" };
        }
    }

    [Fact]
    public async Task DoesNotSendRpctokenToCrossOriginUrl()
    {
        var (sdk, handler) = CreateWithRpcToken(Ok());
        await sdk.SendRequestAsync("PUT", S3Url);
        Assert.Null(handler.Calls[0].Header("rpctoken"));
    }

    [Fact]
    public async Task SendsRpctokenToSameOriginRelativeAndAbsolute()
    {
        var (sdk, handler) = CreateWithRpcToken(Ok(), Ok());
        await sdk.SendRequestAsync("GET", "/sapi/mediaclip");
        await sdk.SendRequestAsync("GET", "https://my-publication.bbvms.com/sapi/mediaclip");
        Assert.Matches("^1-", handler.Calls[0].Header("rpctoken"));
        Assert.Matches("^1-", handler.Calls[1].Header("rpctoken"));
    }

    [Fact]
    public async Task SkipAuthSuppressesAuthOnSameOrigin()
    {
        var (sdk, handler) = CreateWithRpcToken(Ok());
        await sdk.SendRequestAsync("GET", "/sapi/mediaclip", new RequestOptions { SkipAuth = true });
        Assert.Null(handler.Calls[0].Header("rpctoken"));
    }

    [Fact]
    public async Task FailsClosedOnUnparseableUrl()
    {
        // A malformed absolute URL makes origin resolution fail; the guard must treat it as
        // cross-origin and never mint a token, and the transport failure is typed.
        var auth = new CountingAuthenticator();
        var handler = new MockHttpHandler(Ok());
        var sdk = new Sdk("my-publication", auth, new SdkOptions { HttpClient = new HttpClient(handler) });

        await Assert.ThrowsAsync<SapiConnectionException>(() => sdk.SendRequestAsync("PUT", "https://["));

        Assert.Equal(0, auth.Calls);
        Assert.Empty(handler.Calls);
    }

    private sealed class SlowHandler : HttpMessageHandler
    {
        private readonly int _delayMs;
        public SlowHandler(int delayMs) => _delayMs = delayMs;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delayMs, cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("") };
        }
    }

    [Fact]
    public async Task DefaultTimeoutAbortsASlowRequest()
    {
        var sdk = WithHandler(new SlowHandler(400), TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAsync<SapiConnectionException>(() => sdk.SendRequestAsync("GET", "/sapi/mediaclip"));
    }

    [Fact]
    public async Task ZeroTimeoutDisablesItForThatRequest()
    {
        // Streaming uploads pass zero: their duration scales with file size.
        var sdk = WithHandler(new SlowHandler(200), TimeSpan.FromMilliseconds(50));
        var response = await sdk.SendRequestAsync("PUT", S3Url, new RequestOptions { Timeout = TimeSpan.Zero });
        Assert.True(response.Ok);
    }

    [Fact]
    public async Task UploadBodyHasContentLengthNotChunked()
    {
        var (sdk, handler) = Create(Ok());
        await sdk.SendRequestAsync("PUT", S3Url, new RequestOptions { Body = new ByteArrayContent(new byte[10]), Timeout = TimeSpan.Zero });
        Assert.Equal("10", handler.Calls[0].Header("Content-Length"));
        Assert.Null(handler.Calls[0].Header("Transfer-Encoding"));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _impl;
        public ThrowingHandler(Func<CancellationToken, Task<HttpResponseMessage>> impl) => _impl = impl;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _impl(cancellationToken);
    }

    private static Sdk WithHandler(HttpMessageHandler handler, TimeSpan? timeout = null)
        => new("my-publication", new EmptyAuthenticator(), new SdkOptions { HttpClient = new HttpClient(handler), Timeout = timeout ?? TimeSpan.FromSeconds(30) });

    [Fact]
    public async Task WrapsNetworkFailurePreservingCause()
    {
        var cause = new HttpRequestException("fetch failed");
        var sdk = WithHandler(new ThrowingHandler(_ => throw cause));

        var ex = await Assert.ThrowsAsync<SapiConnectionException>(() => sdk.SendRequestAsync("GET", "/sapi/mediaclip"));

        Assert.Equal(0, ex.StatusCode);
        Assert.Same(cause, ex.InnerException);
        Assert.Contains("fetch failed", ex.Message);
    }

    [Fact]
    public async Task WrapsTimeout()
    {
        var sdk = WithHandler(new ThrowingHandler(async ct => { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(); }), TimeSpan.FromMilliseconds(50));

        var ex = await Assert.ThrowsAsync<SapiConnectionException>(() => sdk.SendRequestAsync("GET", "/sapi/mediaclip"));

        Assert.Matches("timed out after 50ms", ex.Message);
        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact]
    public async Task CallerCancellationIsNotWrapped()
    {
        var sdk = WithHandler(new ThrowingHandler(async ct => { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(); }));
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(20);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sdk.SendRequestAsync("GET", "/sapi/mediaclip", null, cts.Token));
    }

    [Fact]
    public async Task WrapsArbitraryTransportException()
    {
        var sdk = WithHandler(new ThrowingHandler(_ => throw new IOException("boom")));

        var ex = await Assert.ThrowsAsync<SapiConnectionException>(() => sdk.SendRequestAsync("GET", "/sapi/mediaclip"));

        Assert.IsType<IOException>(ex.InnerException);
    }
}
