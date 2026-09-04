namespace BlueBillywig.Sapi.Contracts;

/// <summary>An entity that can be deleted by ID.</summary>
public interface IDeletable
{
    /// <summary>Deletes an entity.</summary>
    Task<SapiResponse> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
