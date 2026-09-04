using BlueBillywig.Sapi.Authentication;
using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Tests.Helpers;

public static class TestSdk
{
    public static (Sdk Sdk, MockHttpHandler Handler) Create(params MockResponse[] responses)
    {
        var handler = new MockHttpHandler(responses);
        var sdk = new Sdk("my-publication", new EmptyAuthenticator(), new SdkOptions { HttpClient = new HttpClient(handler) });
        return (sdk, handler);
    }

    public static (Sdk Sdk, MockHttpHandler Handler) CreateWithRpcToken(params MockResponse[] responses)
    {
        var handler = new MockHttpHandler(responses);
        var sdk = Sdk.WithRpcTokenAuthentication("my-publication", 1, "secret", new SdkOptions { HttpClient = new HttpClient(handler) });
        return (sdk, handler);
    }

    public static MockResponse Ok(string? body = null) => new() { Status = 200, Body = body };

    public static string? Query(string url, string name) => QueryParams.Get(url, name);

    public static string PathOf(string url) => new Uri(url).AbsolutePath;

    public static (string Path, Action Cleanup) TempFile(int size, byte fill)
    {
        var path = Path.Combine(Path.GetTempPath(), $"bb-sdk-test-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(path, Enumerable.Repeat(fill, size).ToArray());
        return (path, () => { try { File.Delete(path); } catch { } });
    }
}
