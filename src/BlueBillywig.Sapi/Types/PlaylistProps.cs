using System.Text.Json.Serialization;

namespace BlueBillywig.Sapi.Types;

/// <summary>Writable playlist properties. Unset (null) properties are omitted from the request.</summary>
public sealed class PlaylistProps
{
    /// <summary>Title.</summary>
    [JsonPropertyName("title")] public string? Title { get; set; }
    /// <summary>Description.</summary>
    [JsonPropertyName("description")] public string? Description { get; set; }
    /// <summary>Status.</summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    /// <summary>Playlist type.</summary>
    [JsonPropertyName("type")] public string? Type { get; set; }
    /// <summary>Query.</summary>
    [JsonPropertyName("query")] public string? Query { get; set; }
    /// <summary>Media type.</summary>
    [JsonPropertyName("mediatype")] public string? Mediatype { get; set; }
    /// <summary>Use type.</summary>
    [JsonPropertyName("usetype")] public string? Usetype { get; set; }
    /// <summary>Copyright.</summary>
    [JsonPropertyName("copyright")] public string? Copyright { get; set; }
    /// <summary>Author.</summary>
    [JsonPropertyName("author")] public string? Author { get; set; }
    /// <summary>Deeplink URL.</summary>
    [JsonPropertyName("deeplinkUrl")] public string? DeeplinkUrl { get; set; }
    /// <summary>Short title.</summary>
    [JsonPropertyName("shortTitle")] public string? ShortTitle { get; set; }
    /// <summary>External URL.</summary>
    [JsonPropertyName("externalUrl")] public string? ExternalUrl { get; set; }
    /// <summary>Shuffle order.</summary>
    [JsonPropertyName("shuffleOrder")] public bool? ShuffleOrder { get; set; }
    /// <summary>Use suggest.</summary>
    [JsonPropertyName("useSuggest")] public bool? UseSuggest { get; set; }
    /// <summary>Extra suggest query.</summary>
    [JsonPropertyName("extraSuggestQuery")] public string? ExtraSuggestQuery { get; set; }
    /// <summary>Allow datasource.</summary>
    [JsonPropertyName("allowDatasource")] public bool? AllowDatasource { get; set; }
    /// <summary>Limit.</summary>
    [JsonPropertyName("limit")] public int? Limit { get; set; }
    /// <summary>Sort.</summary>
    [JsonPropertyName("sort")] public string? Sort { get; set; }
    /// <summary>Any additional properties to send verbatim.</summary>
    [JsonExtensionData] public Dictionary<string, object>? AdditionalProperties { get; set; }
}
