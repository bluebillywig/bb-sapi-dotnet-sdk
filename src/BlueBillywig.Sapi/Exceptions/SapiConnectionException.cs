namespace BlueBillywig.Sapi.Exceptions;

/// <summary>
/// Exception for transport-level failures (network error, DNS failure, connection reset, or
/// request timeout) where no HTTP response was received.
/// <see cref="SapiRequestException.StatusCode"/> is 0 to signal "no response reached us"; the
/// underlying error is preserved on <see cref="Exception.InnerException"/>.
/// </summary>
public class SapiConnectionException : SapiRequestException
{
    /// <summary>Creates a new connection exception.</summary>
    public SapiConnectionException(string message, Exception? cause = null)
        : base(message, 0, null, cause)
    {
    }
}
