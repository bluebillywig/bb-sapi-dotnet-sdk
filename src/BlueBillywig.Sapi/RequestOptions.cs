namespace BlueBillywig.Sapi;

/// <summary>Per-request options for <see cref="Sdk.SendRequestAsync"/>.</summary>
public sealed class RequestOptions
{
    /// <summary>Query parameters to set on the URL (replacing same-named existing ones).</summary>
    public IReadOnlyDictionary<string, string>? Query { get; set; }

    /// <summary>An object serialized as the JSON body (sets <c>Content-Type: application/json</c>).</summary>
    public object? Json { get; set; }

    /// <summary>A raw body, used when <see cref="Json"/> is not set.</summary>
    public HttpContent? Body { get; set; }

    /// <summary>Extra request headers. These override authentication headers of the same name.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Force-skip the SAPI auth headers for this request. By default auth is attached only to
    /// same-origin (SAPI) requests; cross-origin URLs (e.g. S3 presigned upload/progress URLs)
    /// are never sent the rpctoken. Set this to also suppress auth on a same-origin request.
    /// </summary>
    public bool? SkipAuth { get; set; }

    /// <summary>
    /// Per-request timeout, overriding <see cref="SdkOptions.Timeout"/>. Pass
    /// <see cref="TimeSpan.Zero"/> to disable the timeout for this request (used by uploads).
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
