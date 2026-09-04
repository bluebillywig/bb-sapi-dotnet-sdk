namespace BlueBillywig.Sapi.Util;

/// <summary>The class of an HTTP status code.</summary>
public enum HttpStatusCodeCategory
{
    /// <summary>1xx.</summary>
    Informational,
    /// <summary>2xx.</summary>
    Successful,
    /// <summary>3xx.</summary>
    Redirection,
    /// <summary>4xx.</summary>
    ClientError,
    /// <summary>5xx.</summary>
    ServerError,
}

/// <summary>Helpers for <see cref="HttpStatusCodeCategory"/>.</summary>
public static class HttpStatusCodeCategoryExtensions
{
    /// <summary>Returns the category of a status code.</summary>
    /// <exception cref="ArgumentOutOfRangeException">When the status code is outside 100-599.</exception>
    public static HttpStatusCodeCategory GetStatusCodeCategory(int statusCode)
    {
        if (statusCode >= 100 && statusCode <= 199) return HttpStatusCodeCategory.Informational;
        if (statusCode >= 200 && statusCode <= 299) return HttpStatusCodeCategory.Successful;
        if (statusCode >= 300 && statusCode <= 399) return HttpStatusCodeCategory.Redirection;
        if (statusCode >= 400 && statusCode <= 499) return HttpStatusCodeCategory.ClientError;
        if (statusCode >= 500 && statusCode <= 599) return HttpStatusCodeCategory.ServerError;
        throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, $"Unexpected HTTP status code: {statusCode}");
    }
}
