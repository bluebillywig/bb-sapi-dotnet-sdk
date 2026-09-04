namespace BlueBillywig.Sapi;

/// <summary>Abstract base class for all SAPI entities. Provides access to the SDK instance.</summary>
public abstract class Entity
{
    /// <summary>Creates an entity bound to an SDK instance.</summary>
    protected Entity(Sdk sdk)
    {
        Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
    }

    /// <summary>The SDK instance this entity belongs to.</summary>
    public Sdk Sdk { get; }
}
