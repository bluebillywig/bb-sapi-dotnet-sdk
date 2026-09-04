namespace BlueBillywig.Sapi.Contracts;

/// <summary>An entity that can be fetched by ID.</summary>
public interface IGettable
{
    /// <summary>Gets a single entity by ID.</summary>
    Task<SapiResponse> GetAsync(string id, CancellationToken cancellationToken = default);
}
