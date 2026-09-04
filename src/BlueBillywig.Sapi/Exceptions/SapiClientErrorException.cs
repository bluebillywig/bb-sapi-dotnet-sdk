namespace BlueBillywig.Sapi.Exceptions;

/// <summary>Exception for 4xx (client error) HTTP responses.</summary>
public class SapiClientErrorException : SapiRequestException
{
    /// <summary>Creates a new client-error exception.</summary>
    public SapiClientErrorException(string message, int statusCode, string? responseBody = null)
        : base(message, statusCode, responseBody)
    {
    }
}
