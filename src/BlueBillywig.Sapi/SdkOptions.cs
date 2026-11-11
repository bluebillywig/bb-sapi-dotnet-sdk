namespace BlueBillywig.Sapi;

/// <summary>Optional SDK configuration.</summary>
public sealed class SdkOptions
{
    /// <summary>Override the base URI (e.g. for testing). Defaults to <c>https://{publication}.bbvms.com</c>.</summary>
    public string? BaseUri { get; set; }

    /// <summary>
    /// Injectable <see cref="System.Net.Http.HttpClient"/> (for testing, or to share a pooled client).
    /// When omitted the SDK creates and owns one. The SDK manages request timeouts itself, so a
    /// supplied client's own <see cref="System.Net.Http.HttpClient.Timeout"/> should be infinite
    /// or larger than <see cref="Timeout"/>.
    /// <para>
    /// <b>Redirects:</b> the SDK-owned client does not follow redirects. The SDK cannot inspect the
    /// handler of an injected client, so that client MUST be built with auto-redirect disabled
    /// (for example <c>new SocketsHttpHandler { AllowAutoRedirect = false }</c>); otherwise
    /// <see cref="System.Net.Http.HttpClient"/> re-sends custom headers such as the <c>rpctoken</c>
    /// to the redirect target, which can be another origin.
    /// </para>
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Default per-request timeout. A request that produces no response within this window is
    /// aborted and surfaces as a <see cref="Exceptions.SapiConnectionException"/>. Pass
    /// <see cref="TimeSpan.Zero"/> to disable the default timeout. Defaults to 30 seconds.
    /// Override per request via <see cref="RequestOptions.Timeout"/> (streaming uploads pass zero,
    /// since their duration scales with file size).
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
