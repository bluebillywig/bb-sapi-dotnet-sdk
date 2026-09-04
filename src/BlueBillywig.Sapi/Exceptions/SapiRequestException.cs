namespace BlueBillywig.Sapi.Exceptions;

/// <summary>
/// Base exception for non-successful HTTP responses from the SAPI.
/// (Named <c>HTTPRequestException</c> in the Node SDK; renamed here to avoid clashing with
/// <see cref="System.Net.Http.HttpRequestException"/>.)
/// </summary>
public class SapiRequestException : Exception
{
    /// <summary>The HTTP status code, or 0 when no response was received.</summary>
    public int StatusCode { get; }

    /// <summary>The raw response body, or <c>null</c> when empty or absent.</summary>
    public string? ResponseBody { get; }

    /// <summary>Creates a new request exception.</summary>
    public SapiRequestException(string message, int statusCode, string? responseBody = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
