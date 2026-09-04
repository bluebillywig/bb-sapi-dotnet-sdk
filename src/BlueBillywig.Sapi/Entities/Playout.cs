using System.Globalization;
using BlueBillywig.Sapi.Contracts;
using BlueBillywig.Sapi.Types;
using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Entities;

/// <summary>CRUD operations on <c>/sapi/playout</c>.</summary>
public sealed class Playout : Entity, IListable, IGettable, ICreatable<PlayoutProps>, IUpdatable<PlayoutProps>, IDeletable
{
    private const string Path = "/sapi/playout";

    /// <summary>Creates the entity.</summary>
    public Playout(Sdk sdk) : base(sdk) { }

    /// <inheritdoc />
    public Task<SapiResponse> ListAsync(int limit = 15, int offset = 0, string sort = "createddate desc", CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("GET", Path, new RequestOptions { Query = QueryParams.Build(("limit", limit), ("offset", offset), ("sort", sort)) }, cancellationToken);

    /// <inheritdoc />
    public Task<SapiResponse> GetAsync(string id, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("GET", $"{Path}/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <inheritdoc cref="GetAsync(string, CancellationToken)" />
    public Task<SapiResponse> GetAsync(long id, CancellationToken cancellationToken = default)
        => GetAsync(id.ToString(CultureInfo.InvariantCulture), cancellationToken);

    /// <inheritdoc />
    public Task<SapiResponse> CreateAsync(PlayoutProps props, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("PUT", Path, new RequestOptions { Json = props }, cancellationToken);

    /// <inheritdoc />
    public Task<SapiResponse> UpdateAsync(string id, PlayoutProps props, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("PUT", $"{Path}/{Uri.EscapeDataString(id)}", new RequestOptions { Json = props }, cancellationToken);

    /// <inheritdoc cref="UpdateAsync(string, PlayoutProps, CancellationToken)" />
    public Task<SapiResponse> UpdateAsync(long id, PlayoutProps props, CancellationToken cancellationToken = default)
        => UpdateAsync(id.ToString(CultureInfo.InvariantCulture), props, cancellationToken);

    /// <inheritdoc />
    public Task<SapiResponse> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("DELETE", $"{Path}/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <inheritdoc cref="DeleteAsync(string, CancellationToken)" />
    public Task<SapiResponse> DeleteAsync(long id, CancellationToken cancellationToken = default)
        => DeleteAsync(id.ToString(CultureInfo.InvariantCulture), cancellationToken);
}
