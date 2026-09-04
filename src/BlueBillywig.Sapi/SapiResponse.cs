using System.Text.Json;
using System.Text.Json.Nodes;
using BlueBillywig.Sapi.Exceptions;
using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi;

/// <summary>Represents a SAPI HTTP response with convenience accessors.</summary>
public sealed class SapiResponse
{
    /// <summary>The final resolved URL of the request (including query params).</summary>
    public string Url { get; }

    /// <summary>The HTTP method used for the request.</summary>
    public string Method { get; }

    /// <summary>The HTTP status code.</summary>
    public int StatusCode { get; }

    /// <summary>The HTTP status text (reason phrase).</summary>
    public string StatusText { get; }

    /// <summary>The response headers (case-insensitive keys).</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>The raw response body as a string.</summary>
    public string Body { get; }

    /// <summary>Creates a response.</summary>
    public SapiResponse(string url, string method, int statusCode, string statusText, IReadOnlyDictionary<string, string> headers, string body)
    {
        Url = url;
        Method = method;
        StatusCode = statusCode;
        StatusText = statusText ?? string.Empty;
        Headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        Body = body ?? string.Empty;
    }

    /// <summary><c>true</c> if the response has a 2xx status code.</summary>
    public bool Ok => StatusCategory == HttpStatusCodeCategory.Successful;

    /// <summary>The HTTP status code category (Informational, Successful, etc.).</summary>
    public HttpStatusCodeCategory StatusCategory => HttpStatusCodeCategoryExtensions.GetStatusCodeCategory(StatusCode);

    /// <summary>Throws an appropriate exception if the response is not 2xx.</summary>
    /// <exception cref="SapiClientErrorException">for 4xx responses</exception>
    /// <exception cref="SapiServerErrorException">for 5xx responses</exception>
    /// <exception cref="SapiRequestException">for other non-2xx responses</exception>
    public void AssertOk()
    {
        if (Ok) return;

        var responseBody = Body.Length > 0 ? Body : null;
        throw StatusCategory switch
        {
            HttpStatusCodeCategory.ClientError => new SapiClientErrorException(StatusText, StatusCode, responseBody),
            HttpStatusCodeCategory.ServerError => new SapiServerErrorException(StatusText, StatusCode, responseBody),
            _ => new SapiRequestException(StatusText, StatusCode, responseBody),
        };
    }

    /// <summary>Parses the response body as a JSON node.</summary>
    /// <returns>The parsed node, or <c>null</c> if the body is empty.</returns>
    /// <exception cref="JsonException">if the body is not valid JSON.</exception>
    public JsonNode? Json() => Body.Length == 0 ? null : JsonNode.Parse(Body);

    /// <summary>Deserializes the response body as <typeparamref name="T"/>.</summary>
    /// <returns>The deserialized value, or <c>default</c> if the body is empty.</returns>
    /// <exception cref="JsonException">if the body is not valid JSON.</exception>
    public T? Json<T>() => Body.Length == 0 ? default : JsonSerializer.Deserialize<T>(Body, SapiJson.Options);

    /// <summary>Returns a response header value by name (case-insensitive), or <c>null</c>.</summary>
    public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;

    /// <summary>Returns a query parameter from the request URL, or <c>null</c> if not present.</summary>
    public string? QueryParam(string name) => QueryParams.Get(Url, name);

    /// <summary>Returns <c>true</c> if all responses have 2xx status codes.</summary>
    public static bool AllOk(IEnumerable<SapiResponse> responses) => responses.All(r => r.Ok);

    /// <summary>Asserts all responses are 2xx. Throws on the first non-2xx response.</summary>
    public static void AssertAllOk(IEnumerable<SapiResponse> responses)
    {
        foreach (var response in responses)
        {
            response.AssertOk();
        }
    }

    /// <summary>Yields only the failed (non-2xx) responses.</summary>
    public static IEnumerable<SapiResponse> FailedResponses(IEnumerable<SapiResponse> responses) => responses.Where(r => !r.Ok);
}
