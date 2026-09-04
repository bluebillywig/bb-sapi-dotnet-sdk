namespace BlueBillywig.Sapi.Contracts;

/// <summary>An entity that can be listed (paged and sorted).</summary>
public interface IListable
{
    /// <summary>Lists entities.</summary>
    Task<SapiResponse> ListAsync(int limit = 15, int offset = 0, string sort = "createddate desc", CancellationToken cancellationToken = default);
}
