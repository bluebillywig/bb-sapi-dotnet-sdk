namespace BlueBillywig.Sapi.Contracts;

/// <summary>An entity that can be created.</summary>
/// <typeparam name="TProps">The property bag type sent as the JSON body.</typeparam>
public interface ICreatable<in TProps>
{
    /// <summary>Creates a new entity.</summary>
    Task<SapiResponse> CreateAsync(TProps props, CancellationToken cancellationToken = default);
}
