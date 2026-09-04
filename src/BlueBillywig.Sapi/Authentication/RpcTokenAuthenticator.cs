using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Authentication;

/// <summary>
/// Authenticates requests with a time-based HOTP RPC token, sent as the <c>rpctoken</c> header
/// in the form <c>{tokenId}-{token}</c>.
/// </summary>
public sealed class RpcTokenAuthenticator : IAuthenticator
{
    /// <summary>
    /// Default token expiration window in seconds. Tokens are valid within this time window.
    /// Both client and server must have reasonably synchronized clocks (within this window)
    /// for authentication to succeed.
    /// </summary>
    public const int DefaultTokenExpirationSeconds = 120;

    private readonly string _sharedSecret;

    /// <summary>The RPC token ID.</summary>
    public int TokenId { get; }

    /// <summary>The token expiration window in seconds.</summary>
    public int TokenExpiration { get; }

    /// <summary>Creates an authenticator for the given token ID and shared secret.</summary>
    public RpcTokenAuthenticator(int tokenId, string sharedSecret, int tokenExpiration = DefaultTokenExpirationSeconds)
    {
        TokenId = tokenId;
        _sharedSecret = sharedSecret ?? throw new ArgumentNullException(nameof(sharedSecret));
        TokenExpiration = tokenExpiration;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Authenticate()
    {
        var token = Hotp.GenerateByTime(_sharedSecret, TokenExpiration);
        return new Dictionary<string, string> { ["rpctoken"] = $"{TokenId}-{token}" };
    }
}
