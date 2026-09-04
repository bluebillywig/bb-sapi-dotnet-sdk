namespace BlueBillywig.Sapi.Exceptions;

/// <summary>Exception for 5xx (server error) HTTP responses.</summary>
public class SapiServerErrorException : SapiRequestException
{
    /// <summary>Creates a new server-error exception.</summary>
    public SapiServerErrorException(string message, int statusCode, string? responseBody = null)
        : base(message, statusCode, responseBody)
    {
    }
}
