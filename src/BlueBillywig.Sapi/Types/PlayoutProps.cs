using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BlueBillywig.Sapi.Types;

/// <summary>Writable playout properties. Unset (null) properties are omitted from the request.</summary>
public sealed class PlayoutProps
{
    /// <summary>Name.</summary>
    [JsonPropertyName("name")] public string? Name { get; set; }
    /// <summary>Label.</summary>
    [JsonPropertyName("label")] public string? Label { get; set; }
    /// <summary>Status.</summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    /// <summary>Center button type.</summary>
    [JsonPropertyName("centerButtonType")] public string? CenterButtonType { get; set; }
    /// <summary>Corner radius.</summary>
    [JsonPropertyName("cornerRadius")] public int? CornerRadius { get; set; }
    /// <summary>Responsive sizing.</summary>
    [JsonPropertyName("responsiveSizing")] public bool? ResponsiveSizing { get; set; }
    /// <summary>Width.</summary>
    [JsonPropertyName("width")] public int? Width { get; set; }
    /// <summary>Height.</summary>
    [JsonPropertyName("height")] public int? Height { get; set; }
    /// <summary>Max width.</summary>
    [JsonPropertyName("maxWidth")] public int? MaxWidth { get; set; }
    /// <summary>Auto height.</summary>
    [JsonPropertyName("autoHeight")] public bool? AutoHeight { get; set; }
    /// <summary>Aspect ratio.</summary>
    [JsonPropertyName("aspectRatio")] public string? AspectRatio { get; set; }
    /// <summary>Background color.</summary>
    [JsonPropertyName("backgroundColor")] public string? BackgroundColor { get; set; }
    /// <summary>Foreground color.</summary>
    [JsonPropertyName("foregroundColor")] public string? ForegroundColor { get; set; }
    /// <summary>Widget color.</summary>
    [JsonPropertyName("widgetColor")] public string? WidgetColor { get; set; }
    /// <summary>Bg color.</summary>
    [JsonPropertyName("bgColor")] public string? BgColor { get; set; }
    /// <summary>Logo ID.</summary>
    [JsonPropertyName("logoId")] public long? LogoId { get; set; }
    /// <summary>Logo alignment.</summary>
    [JsonPropertyName("logoAlign")] public string? LogoAlign { get; set; }
    /// <summary>Logo click URL.</summary>
    [JsonPropertyName("logoClickUrl")] public string? LogoClickUrl { get; set; }
    /// <summary>Control bar.</summary>
    [JsonPropertyName("controlBar")] public bool? ControlBar { get; set; }
    /// <summary>Time display.</summary>
    [JsonPropertyName("timeDisplay")] public bool? TimeDisplay { get; set; }
    /// <summary>Time line.</summary>
    [JsonPropertyName("timeLine")] public bool? TimeLine { get; set; }
    /// <summary>Mute button.</summary>
    [JsonPropertyName("muteButton")] public bool? MuteButton { get; set; }
    /// <summary>Volume.</summary>
    [JsonPropertyName("volume")] public bool? Volume { get; set; }
    /// <summary>Full screen.</summary>
    [JsonPropertyName("fullScreen")] public bool? FullScreen { get; set; }
    /// <summary>Auto play.</summary>
    [JsonPropertyName("autoPlay")] public bool? AutoPlay { get; set; }
    /// <summary>Auto loop.</summary>
    [JsonPropertyName("autoLoop")] public bool? AutoLoop { get; set; }
    /// <summary>Auto mute.</summary>
    [JsonPropertyName("autoMute")] public bool? AutoMute { get; set; }
    /// <summary>Title.</summary>
    [JsonPropertyName("title")] public bool? Title { get; set; }
    /// <summary>Fit mode.</summary>
    [JsonPropertyName("fitmode")] public string? Fitmode { get; set; }
    /// <summary>Commercials.</summary>
    [JsonPropertyName("commercials")] public List<JsonNode?>? Commercials { get; set; }
    /// <summary>Any additional properties to send verbatim.</summary>
    [JsonExtensionData] public Dictionary<string, object>? AdditionalProperties { get; set; }
}
