using System.Text.Json.Serialization;

namespace BlueBillywig.Sapi.Types;

/// <summary>Writable subtitle properties. Unset (null) properties are omitted from the request.</summary>
public sealed class SubtitleProps
{
    /// <summary>Status.</summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    /// <summary>Media clip ID.</summary>
    [JsonPropertyName("mediaclipId")] public long? MediaclipId { get; set; }
    /// <summary>Language ID.</summary>
    [JsonPropertyName("languageId")] public long? LanguageId { get; set; }
    /// <summary>ISO code.</summary>
    [JsonPropertyName("isocode")] public string? Isocode { get; set; }
    /// <summary>Original file name.</summary>
    [JsonPropertyName("originalfilename")] public string? Originalfilename { get; set; }
    /// <summary>Upload identifier.</summary>
    [JsonPropertyName("uploadIdentifier")] public string? UploadIdentifier { get; set; }
    /// <summary>Any additional properties to send verbatim.</summary>
    [JsonExtensionData] public Dictionary<string, object>? AdditionalProperties { get; set; }
}
