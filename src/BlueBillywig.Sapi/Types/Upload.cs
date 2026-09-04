using System.Text.Json.Serialization;

namespace BlueBillywig.Sapi.Types;

/// <summary>A presigned S3 URL for one upload part.</summary>
public sealed class PresignedUrl
{
    /// <summary>The presigned PUT URL.</summary>
    [JsonPropertyName("presignedUrl")] public string PresignedUrlValue { get; set; } = string.Empty;
    /// <summary>Byte offset of this part in the file (defaults to 0).</summary>
    [JsonPropertyName("offset")] public long? Offset { get; set; }
    /// <summary>Size of this part in bytes (defaults to the whole file).</summary>
    [JsonPropertyName("chunkSize")] public long? ChunkSize { get; set; }
}

/// <summary>Upload configuration as returned by <c>MediaClip.InitializeUploadAsync</c>.</summary>
public sealed class UploadData
{
    /// <summary>Number of parts.</summary>
    [JsonPropertyName("chunks")] public int? Chunks { get; set; }
    /// <summary>One presigned URL per part.</summary>
    [JsonPropertyName("presignedUrls")] public List<PresignedUrl>? PresignedUrls { get; set; }
    /// <summary>S3 object key (multi-part only).</summary>
    [JsonPropertyName("key")] public string? Key { get; set; }
    /// <summary>S3 multi-part upload ID (multi-part only).</summary>
    [JsonPropertyName("uploadId")] public string? UploadId { get; set; }
    /// <summary>S3 URL to list uploaded parts (for progress polling).</summary>
    [JsonPropertyName("listPartsUrl")] public string? ListPartsUrl { get; set; }
    /// <summary>S3 URL to check whether the object exists (upload complete).</summary>
    [JsonPropertyName("headObjectUrl")] public string? HeadObjectUrl { get; set; }
    /// <summary>Any additional properties returned by the SAPI.</summary>
    [JsonExtensionData] public Dictionary<string, object>? AdditionalProperties { get; set; }
}

/// <summary>An uploaded S3 part, as sent to <c>completeUpload</c>.</summary>
public sealed class S3Part
{
    /// <summary>The part's ETag (without quotes).</summary>
    [JsonPropertyName("ETag")] public string ETag { get; set; } = string.Empty;
    /// <summary>The part number.</summary>
    [JsonPropertyName("PartNumber")] public string PartNumber { get; set; } = string.Empty;

    /// <summary>Creates an empty part.</summary>
    public S3Part() { }

    /// <summary>Creates a part.</summary>
    public S3Part(string eTag, string partNumber)
    {
        ETag = eTag;
        PartNumber = partNumber;
    }
}
