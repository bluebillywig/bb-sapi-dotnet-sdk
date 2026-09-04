using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueBillywig.Sapi.Util;

/// <summary>Shared <see cref="JsonSerializerOptions"/> for SAPI request and response bodies.</summary>
public static class SapiJson
{
    /// <summary>
    /// camelCase property names, nulls omitted when writing, case-insensitive when reading,
    /// and relaxed escaping so bodies stay readable on the wire.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
