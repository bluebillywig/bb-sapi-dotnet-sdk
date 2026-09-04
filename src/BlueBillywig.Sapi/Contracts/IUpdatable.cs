namespace BlueBillywig.Sapi.Contracts;

/// <summary>An entity that can be updated by ID.</summary>
/// <typeparam name="TProps">The property bag type sent as the JSON body.</typeparam>
public interface IUpdatable<in TProps>
{
    /// <summary>Updates an existing entity.</summary>
    Task<SapiResponse> UpdateAsync(string id, TProps props, CancellationToken cancellationToken = default);
}
