namespace BlueBillywig.Sapi.Authentication;

/// <summary>Strategy interface for authenticating outgoing SAPI requests.</summary>
public interface IAuthenticator
{
    /// <summary>Returns headers to add to the outgoing request for authentication.</summary>
    IReadOnlyDictionary<string, string> Authenticate();
}
