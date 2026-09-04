using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BlueBillywig.Sapi.Types;

/// <summary>Channel detail page configuration.</summary>
public sealed class ChannelDetailPageConfig
{
    /// <summary>Player alignment.</summary>
    [JsonPropertyName("playerAlignment")] public string? PlayerAlignment { get; set; }
    /// <summary>Within border.</summary>
    [JsonPropertyName("withinBorder")] public bool? WithinBorder { get; set; }
    /// <summary>Background color.</summary>
    [JsonPropertyName("backgroundColor")] public string? BackgroundColor { get; set; }
    /// <summary>Show thumbnail as background.</summary>
    [JsonPropertyName("showThumbnailAsBackground")] public bool? ShowThumbnailAsBackground { get; set; }
    /// <summary>Enable background blur.</summary>
    [JsonPropertyName("enableBackgroundBlur")] public bool? EnableBackgroundBlur { get; set; }
    /// <summary>Show related items.</summary>
    [JsonPropertyName("showRelatedItems")] public bool? ShowRelatedItems { get; set; }
    /// <summary>Related items layout.</summary>
    [JsonPropertyName("relatedItemsLayout")] public string? RelatedItemsLayout { get; set; }
    /// <summary>Any additional properties to send verbatim.</summary>
    [JsonExtensionData] public Dictionary<string, object>? AdditionalProperties { get; set; }
}

/// <summary>Channel configuration.</summary>
public sealed class ChannelConfig
{
    /// <summary>Play-in mode.</summary>
    [JsonPropertyName("playIn")] public string? PlayIn { get; set; }
    /// <summary>Detail page configuration.</summary>
    [JsonPropertyName("detailPageConfig")] public ChannelDetailPageConfig? DetailPageConfig { get; set; }
    /// <summary>Blocks.</summary>
    [JsonPropertyName("blocks")] public List<JsonNode?>? Blocks { get; set; }
    /// <summary>Any additional properties to send verbatim.</summary>
    [JsonExtensionData] public Dictionary<string, object>? AdditionalProperties { get; set; }
}

/// <summary>Writable channel properties.</summary>
public sealed class ChannelProps
{
    /// <summary>Configuration.</summary>
    [JsonPropertyName("config")] public ChannelConfig? Config { get; set; }
    /// <summary>Any additional properties to send verbatim.</summary>
    [JsonExtensionData] public Dictionary<string, object>? AdditionalProperties { get; set; }
}
