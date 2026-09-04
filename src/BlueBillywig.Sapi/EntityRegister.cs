namespace BlueBillywig.Sapi;

/// <summary>A named entity factory registration.</summary>
/// <param name="Name">The entity name, e.g. <c>mediaclip</c>.</param>
/// <param name="Factory">Creates the entity for an SDK instance.</param>
public sealed record EntityRegistration(string Name, Func<Sdk, Entity> Factory);

/// <summary>
/// A register that lazily instantiates entities by name and caches them. This is the .NET
/// counterpart of the Node SDK's Proxy-based entity register.
/// </summary>
public sealed class EntityRegister
{
    private readonly Sdk _sdk;
    private readonly Dictionary<string, Func<Sdk, Entity>> _factories = new();
    private readonly Dictionary<string, Entity> _cache = new();
    private readonly object _lock = new();

    /// <summary>Creates a register for the given SDK and registrations.</summary>
    public EntityRegister(Sdk sdk, IEnumerable<EntityRegistration> registrations)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        foreach (var registration in registrations)
        {
            _factories[registration.Name] = registration.Factory;
        }
    }

    /// <summary>The registered entity names.</summary>
    public IReadOnlyCollection<string> Names => _factories.Keys;

    /// <summary>Whether an entity with this name is registered.</summary>
    public bool Contains(string name) => _factories.ContainsKey(name);

    /// <summary>Returns the (cached) entity for a name, or <c>null</c> when unknown.</summary>
    public Entity? Get(string name)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(name, out var cached)) return cached;
            if (!_factories.TryGetValue(name, out var factory)) return null;
            var instance = factory(_sdk);
            _cache[name] = instance;
            return instance;
        }
    }

    /// <summary>Returns the (cached) entity for a name as <typeparamref name="T"/>, or <c>null</c>.</summary>
    public T? Get<T>(string name) where T : Entity => Get(name) as T;

    /// <summary>Indexer alias for <see cref="Get(string)"/>.</summary>
    public Entity? this[string name] => Get(name);
}
