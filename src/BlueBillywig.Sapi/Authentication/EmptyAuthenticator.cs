namespace BlueBillywig.Sapi.Authentication;

/// <summary>An authenticator that performs no authentication (pass-through). Useful for testing.</summary>
public sealed class EmptyAuthenticator : IAuthenticator
{
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Authenticate() => Empty;
}
