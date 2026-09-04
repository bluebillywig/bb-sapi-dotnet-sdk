using System.Text.Json.Serialization;

namespace BlueBillywig.Sapi.Types;

/// <summary>Writable media clip properties. Unset (null) properties are omitted from the request.</summary>
public sealed class MediaClipProps
{
    /// <summary>Clip title.</summary>
    [JsonPropertyName("title")] public string? Title { get; set; }
    /// <summary>Clip description.</summary>
    [JsonPropertyName("description")] public string? Description { get; set; }
    /// <summary>Author.</summary>
    [JsonPropertyName("author")] public string? Author { get; set; }
    /// <summary>Copyright notice.</summary>
    [JsonPropertyName("copyright")] public string? Copyright { get; set; }
    /// <summary>Deeplink.</summary>
    [JsonPropertyName("deeplink")] public string? Deeplink { get; set; }
    /// <summary>Status, e.g. <c>published</c> or <c>draft</c>.</summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    /// <summary>Use type, e.g. <c>editorial</c> or <c>commercial</c>.</summary>
    [JsonPropertyName("usetype")] public string? Usetype { get; set; }
    /// <summary>Fit mode.</summary>
    [JsonPropertyName("fitmode")] public string? Fitmode { get; set; }
    /// <summary>Source type.</summary>
    [JsonPropertyName("sourcetype")] public string? Sourcetype { get; set; }
    /// <summary>Original file name.</summary>
    [JsonPropertyName("originalfilename")] public string? Originalfilename { get; set; }
    /// <summary>Length.</summary>
    [JsonPropertyName("length")] public long? Length { get; set; }
    /// <summary>Category.</summary>
    [JsonPropertyName("cat")] public string? Cat { get; set; }
    /// <summary>Playout override.</summary>
    [JsonPropertyName("playoutoverride")] public string? Playoutoverride { get; set; }
    /// <summary>Any additional properties to send verbatim.</summary>
    [JsonExtensionData] public Dictionary<string, object>? AdditionalProperties { get; set; }
}
