using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using BlueBillywig.Sapi.Contracts;
using BlueBillywig.Sapi.Search;
using BlueBillywig.Sapi.Types;
using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Entities;

/// <summary>CRUD, search and upload operations on <c>/sapi/mediaclip</c>.</summary>
public sealed class MediaClip : Entity, IListable, IGettable, ICreatable<MediaClipProps>, IUpdatable<MediaClipProps>, IDeletable
{
    private const string Path = "/sapi/mediaclip";

    /// <summary>Default delay (in ms) between upload progress polling requests.</summary>
    public const int DefaultUploadProgressPollInterval = 2000;

    /// <summary>Maps file extensions to MIME types for media upload content-type detection.</summary>
    private static readonly Dictionary<string, string> MimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".ogg"] = "video/ogg",
        [".ogv"] = "video/ogg",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".wmv"] = "video/x-ms-wmv",
        [".flv"] = "video/x-flv",
        [".mkv"] = "video/x-matroska",
        [".m4v"] = "video/x-m4v",
        [".3gp"] = "video/3gpp",
        [".3g2"] = "video/3gpp2",
        [".ts"] = "video/mp2t",
        [".mts"] = "video/mp2t",
        [".m2ts"] = "video/mp2t",
        [".m3u8"] = "application/vnd.apple.mpegurl",
        [".mpd"] = "application/dash+xml",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".aac"] = "audio/aac",
        [".m4a"] = "audio/mp4",
        [".flac"] = "audio/flac",
        [".wma"] = "audio/x-ms-wma",
        [".opus"] = "audio/opus",
        [".oga"] = "audio/ogg",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".svg"] = "image/svg+xml",
        [".webp"] = "image/webp",
        [".avif"] = "image/avif",
        [".heif"] = "image/heif",
        [".heic"] = "image/heic",
        [".mxf"] = "application/mxf",
    };

    /// <summary>Creates the entity.</summary>
    public MediaClip(Sdk sdk) : base(sdk) { }

    /// <summary>Returns the MIME type for a file path by extension, or <c>application/octet-stream</c>.</summary>
    public static string GetMimeType(string filePath)
    {
        var ext = System.IO.Path.GetExtension(filePath);
        return MimeMap.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
    }

    private static string Id(long id) => id.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Search media clips using a filterset: the filtered counterpart to <see cref="ListAsync"/>,
    /// which can only page and sort. The filterset goes over the wire as JSON and SAPI compiles it,
    /// exactly as the OVP does. It is deliberately not compiled here.
    /// </summary>
    /// <param name="filterSet">Groups are AND-ed, filters within a group OR-ed.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="offset">Page offset.</param>
    /// <param name="sort">Sort expression.</param>
    /// <param name="query">Free-text query; <c>*</c> for everything.</param>
    /// <param name="filterQueries">
    /// Raw Solr filters, for the rare thing a filterset cannot express. These go out as indexed
    /// parameters (<c>fq[0]</c>, percent-encoded as <c>fq%5B0%5D</c> on the wire); SAPI ignores a
    /// repeated <c>fq=</c> and a nested <c>fq[][0]=</c>, in both cases without an error.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task<SapiResponse> SearchAsync(
        FilterSet filterSet,
        int limit = 15,
        int offset = 0,
        string sort = "createddate desc",
        string query = "*",
        IEnumerable<string>? filterQueries = null,
        CancellationToken cancellationToken = default)
    {
        if (filterSet is null) throw new ArgumentNullException(nameof(filterSet));

        var parameters = QueryParams.Build(("q", query), ("limit", limit), ("offset", offset), ("sort", sort));

        var wire = filterSet.ToJsonNode();
        if (wire.Count > 0)
        {
            parameters["filterset"] = wire.ToJsonString(SapiJson.Options);
        }

        var index = 0;
        foreach (var filterQuery in filterQueries ?? Array.Empty<string>())
        {
            parameters[$"fq[{index++}]"] = filterQuery;
        }

        return Sdk.SendRequestAsync("GET", Path, new RequestOptions { Query = parameters }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SapiResponse> ListAsync(int limit = 15, int offset = 0, string sort = "createddate desc", CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("GET", Path, new RequestOptions { Query = QueryParams.Build(("limit", limit), ("offset", offset), ("sort", sort)) }, cancellationToken);

    /// <summary>Gets a media clip.</summary>
    /// <param name="id">The clip ID.</param>
    /// <param name="lang">Optional language.</param>
    /// <param name="includeJobs">Whether to include jobs (default <c>true</c>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task<SapiResponse> GetAsync(string id, string? lang = null, bool includeJobs = true, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("GET", $"{Path}/{Uri.EscapeDataString(id)}", new RequestOptions
        {
            Query = QueryParams.Build(("includejobs", includeJobs), ("lang", lang)),
        }, cancellationToken);

    /// <inheritdoc cref="GetAsync(string, string?, bool, CancellationToken)" />
    public Task<SapiResponse> GetAsync(long id, string? lang = null, bool includeJobs = true, CancellationToken cancellationToken = default)
        => GetAsync(Id(id), lang, includeJobs, cancellationToken);

    Task<SapiResponse> IGettable.GetAsync(string id, CancellationToken cancellationToken) => GetAsync(id, null, true, cancellationToken);

    /// <summary>Creates a media clip.</summary>
    public Task<SapiResponse> CreateAsync(MediaClipProps props, bool softSave = false, string? lang = null, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("PUT", Path, new RequestOptions
        {
            Query = QueryParams.Build(("softsave", softSave), ("lang", lang)),
            Json = props,
        }, cancellationToken);

    Task<SapiResponse> ICreatable<MediaClipProps>.CreateAsync(MediaClipProps props, CancellationToken cancellationToken) => CreateAsync(props, false, null, cancellationToken);

    /// <summary>Updates a media clip.</summary>
    public Task<SapiResponse> UpdateAsync(string id, MediaClipProps props, bool softSave = false, string? lang = null, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("PUT", $"{Path}/{Uri.EscapeDataString(id)}", new RequestOptions
        {
            Query = QueryParams.Build(("softsave", softSave), ("lang", lang)),
            Json = props,
        }, cancellationToken);

    /// <inheritdoc cref="UpdateAsync(string, MediaClipProps, bool, string?, CancellationToken)" />
    public Task<SapiResponse> UpdateAsync(long id, MediaClipProps props, bool softSave = false, string? lang = null, CancellationToken cancellationToken = default)
        => UpdateAsync(Id(id), props, softSave, lang, cancellationToken);

    Task<SapiResponse> IUpdatable<MediaClipProps>.UpdateAsync(string id, MediaClipProps props, CancellationToken cancellationToken) => UpdateAsync(id, props, false, null, cancellationToken);

    /// <summary>Deletes a media clip, optionally purging it.</summary>
    public Task<SapiResponse> DeleteAsync(string id, bool purge = false, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("DELETE", $"{Path}/{Uri.EscapeDataString(id)}", new RequestOptions
        {
            Query = purge ? QueryParams.Build(("purge", true)) : null,
        }, cancellationToken);

    /// <inheritdoc cref="DeleteAsync(string, bool, CancellationToken)" />
    public Task<SapiResponse> DeleteAsync(long id, bool purge = false, CancellationToken cancellationToken = default)
        => DeleteAsync(Id(id), purge, cancellationToken);

    Task<SapiResponse> IDeletable.DeleteAsync(string id, CancellationToken cancellationToken) => DeleteAsync(id, false, cancellationToken);

    private static FileInfo RequireFile(string mediaClipPath)
    {
        var info = new FileInfo(mediaClipPath);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"File {mediaClipPath} is not a file or does not exist.", mediaClipPath);
        }
        return info;
    }

    /// <summary>Asks SAPI for presigned upload URLs for a local file.</summary>
    /// <param name="mediaClipPath">Path to the local file.</param>
    /// <param name="mediaClipId">Optional existing clip ID to attach the upload to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="FileNotFoundException">if the path is not an existing file.</exception>
    public Task<SapiResponse> InitializeUploadAsync(string mediaClipPath, long? mediaClipId = null, CancellationToken cancellationToken = default)
    {
        var file = RequireFile(mediaClipPath);
        return Sdk.SendRequestAsync("GET", $"{Path}/0/upload", new RequestOptions
        {
            Query = QueryParams.Build(
                ("filename", file.Name),
                ("filesize", file.Length),
                ("contenttype", GetMimeType(mediaClipPath)),
                ("clipid", mediaClipId)),
        }, cancellationToken);
    }

    /// <summary>Aborts a multi-part upload.</summary>
    public Task<SapiResponse> AbortUploadAsync(string s3FileKey, string s3UploadId, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("PUT", $"{Path}/0/abortUpload", new RequestOptions
        {
            Query = QueryParams.Build(("s3filekey", s3FileKey), ("s3uploadid", s3UploadId)),
        }, cancellationToken);

    /// <summary>Completes a multi-part upload.</summary>
    public Task<SapiResponse> CompleteUploadAsync(string s3FileKey, string s3UploadId, IEnumerable<S3Part> s3Parts, CancellationToken cancellationToken = default)
        => Sdk.SendRequestAsync("PUT", $"{Path}/0/completeUpload", new RequestOptions
        {
            Json = new CompleteUploadBody(s3FileKey, s3UploadId, s3Parts.ToList()),
        }, cancellationToken);

    private sealed record CompleteUploadBody(string S3FileKey, string S3UploadId, List<S3Part> S3Parts);

    /// <summary>
    /// Executes a file upload using presigned URLs from the SAPI. Handles both single-chunk and
    /// multi-part uploads. On multi-part failure, automatically aborts the upload before re-throwing.
    /// </summary>
    /// <param name="mediaClipPath">Path to the local file.</param>
    /// <param name="uploadData">Upload configuration returned by <see cref="InitializeUploadAsync"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    public async Task<bool> ExecuteUploadAsync(string mediaClipPath, UploadData uploadData, CancellationToken cancellationToken = default)
    {
        RequireFile(mediaClipPath);
        if (uploadData is null) throw new ArgumentNullException(nameof(uploadData));

        if (uploadData.Chunks is null || uploadData.PresignedUrls is null)
        {
            throw new ArgumentException("uploadData must contain 'chunks' and 'presignedUrls' keys.", nameof(uploadData));
        }

        if (uploadData.Chunks == 1)
        {
            var response = await PerformUploadAsync(mediaClipPath, uploadData.PresignedUrls[0], cancellationToken).ConfigureAwait(false);
            response.AssertOk();
            return true;
        }

        if (string.IsNullOrEmpty(uploadData.Key) || string.IsNullOrEmpty(uploadData.UploadId))
        {
            throw new ArgumentException("uploadData for multi-part uploads must contain 'key' and 'uploadId' keys.", nameof(uploadData));
        }

        SapiResponse[] responses;
        try
        {
            // Every part is attempted (Task.WhenAll waits for all of them, even when one throws),
            // then the responses are asserted, and only then is a failed upload aborted.
            var uploads = uploadData.PresignedUrls.Select(url => PerformUploadAsync(mediaClipPath, url, cancellationToken)).ToList();
            responses = await Task.WhenAll(uploads).ConfigureAwait(false);
            SapiResponse.AssertAllOk(responses);
        }
        catch
        {
            // ANY failure (non-2xx part, transport error, IO error, cancellation) aborts the
            // multi-part upload so S3 does not keep billing for orphaned parts. CancellationToken.None
            // so a cancelled caller still aborts; abort failures are swallowed to preserve the original error.
            try
            {
                var abortResponse = await AbortUploadAsync(uploadData.Key!, uploadData.UploadId!, CancellationToken.None).ConfigureAwait(false);
                abortResponse.AssertOk();
            }
            catch
            {
                // Abort failed; swallow to preserve the original error.
            }
            throw;
        }

        var parts = responses.Select(response => new S3Part(
            response.Header("ETag")?.Replace("\"", string.Empty) ?? string.Empty,
            response.QueryParam("partNumber") ?? string.Empty)).ToList();

        var completeResponse = await CompleteUploadAsync(uploadData.Key!, uploadData.UploadId!, parts, cancellationToken).ConfigureAwait(false);
        completeResponse.AssertOk();
        return true;
    }

    /// <summary>S3 caps a single part (and a single PUT object) at 5 GiB.</summary>
    internal const long MaxPartSize = 5L * 1024 * 1024 * 1024;

    /// <summary>Length in bytes of the slice a presigned URL covers: computed in <c>long</c>, clamped to the file.</summary>
    internal static long ComputePartLength(long fileLength, long? chunkSize, long? offset)
    {
        var start = offset ?? 0;
        var size = chunkSize ?? fileLength;
        return Math.Max(0, Math.Min(size, fileLength - start));
    }

    private async Task<SapiResponse> PerformUploadAsync(string mediaClipPath, PresignedUrl presignedUrl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fileLength = new FileInfo(mediaClipPath).Length;
        var offset = presignedUrl.Offset ?? 0;
        var length = ComputePartLength(fileLength, presignedUrl.ChunkSize, offset);
        if (length > MaxPartSize)
        {
            throw new ArgumentException($"Upload part of {length} bytes exceeds the S3 maximum part size of {MaxPartSize} bytes.", nameof(presignedUrl));
        }

        // Stream the slice instead of buffering it. ContentLength is set explicitly so HttpClient
        // sends Content-Length and not chunked Transfer-Encoding, which the S3 presigned PUT rejects
        // (501 NotImplemented). The stream is owned (and disposed) by the content.
        var slice = new FileSliceStream(mediaClipPath, offset, length);
        var content = new StreamContent(slice, 81920);
        content.Headers.ContentLength = length;

        // Timeout zero: an upload's duration scales with file size, so the default request timeout
        // would abort large uploads. The presigned URL is cross-origin, so SendRequestAsync already
        // omits the rpctoken (no auth leak to S3).
        return await Sdk.SendRequestAsync("PUT", presignedUrl.PresignedUrlValue, new RequestOptions
        {
            Body = content,
            Timeout = TimeSpan.Zero,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Read-only, bounded view of <c>length</c> bytes of a file starting at <c>offset</c>.</summary>
    private sealed class FileSliceStream : Stream
    {
        private readonly FileStream _file;
        private readonly long _start;
        private readonly long _length;
        private long _position;

        public FileSliceStream(string path, long offset, long length)
        {
            _file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            _start = offset;
            _length = length;
            try { _file.Seek(offset, SeekOrigin.Begin); }
            catch { _file.Dispose(); throw; }
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var remaining = _length - _position;
            if (remaining <= 0) return 0;
            if (buffer.Length > remaining) buffer = buffer[..(int)remaining];
            var read = _file.Read(buffer);
            _position += read;
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var remaining = _length - _position;
            if (remaining <= 0) return 0;
            if (buffer.Length > remaining) buffer = buffer[..(int)remaining];
            var read = await _file.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                _ => _length + offset,
            };
            if (target < 0) throw new IOException("Attempted to seek before the start of the stream.");
            _position = Math.Min(target, _length);
            _file.Seek(_start + _position, SeekOrigin.Begin);
            return _position;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _file.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>Polls S3 to determine upload progress as a percentage (0-100).</summary>
    /// <param name="listPartsUrl">S3 URL to list uploaded parts.</param>
    /// <param name="headObjectUrl">S3 URL to check if the object exists (upload complete).</param>
    /// <param name="partsCount">Total number of expected parts.</param>
    /// <param name="requestDelay">Delay in ms before making the request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="ArgumentOutOfRangeException">when <paramref name="partsCount"/> is zero or negative.</exception>
    public async Task<double> GetUploadProgressAsync(string listPartsUrl, string headObjectUrl, int partsCount, int requestDelay = 0, CancellationToken cancellationToken = default)
    {
        RequirePositivePartsCount(partsCount);
        if (requestDelay > 0)
        {
            await Task.Delay(requestDelay, cancellationToken).ConfigureAwait(false);
        }

        var response = await Sdk.SendRequestAsync("GET", listPartsUrl, null, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == 404)
        {
            var headResponse = await Sdk.SendRequestAsync("HEAD", headObjectUrl, null, cancellationToken).ConfigureAwait(false);
            return headResponse.StatusCode == 404 ? 0 : 100;
        }

        if (response.StatusCode == 200)
        {
            var uploadedPartsCount = CountListedParts(response);
            return Math.Clamp((double)uploadedPartsCount / partsCount * 100, 0, 100);
        }

        response.AssertOk();
        return 0; // unreachable, AssertOk throws
    }

    private static void RequirePositivePartsCount(int partsCount)
    {
        if (partsCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partsCount), partsCount, "partsCount must be greater than zero.");
        }
    }

    /// <summary>
    /// Counts the uploaded parts in a ListParts body. The presigned S3 ListParts URL returns XML
    /// (<c>ListPartsResult</c> with <c>Part</c> children, namespace ignored); a JSON body (legacy
    /// proxy shape) is still accepted.
    /// </summary>
    private static int CountListedParts(SapiResponse response)
    {
        var body = response.Body.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (body.Length == 0) return 0;

        var contentType = response.Header("Content-Type") ?? string.Empty;
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase) || body.StartsWith('{'))
        {
            var parts = (response.Json() as JsonObject)?["Part"];
            return parts switch
            {
                JsonArray array => array.Count,
                // A single object instead of a 1-element array for one part
                JsonObject => 1,
                _ => 0,
            };
        }

        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var stringReader = new StringReader(body);
        using var xmlReader = XmlReader.Create(stringReader, settings);
        var root = XDocument.Load(xmlReader).Root;
        return root?.Elements().Count(e => e.Name.LocalName == "Part") ?? 0;
    }

    /// <summary>Yields upload progress percentages until complete.</summary>
    /// <param name="listPartsUrl">S3 URL to list uploaded parts.</param>
    /// <param name="headObjectUrl">S3 URL to check if the object exists.</param>
    /// <param name="partsCount">Total number of expected parts.</param>
    /// <param name="pollInterval">Minimum interval (ms) between polls.</param>
    /// <param name="maxIterations">Maximum number of polls (0 = unlimited).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// when <paramref name="partsCount"/> is zero or negative; thrown eagerly, at the call, not on first MoveNext.
    /// </exception>
    /// <exception cref="InvalidOperationException">when <paramref name="maxIterations"/> is exceeded.</exception>
    public IAsyncEnumerable<double> UploadProgressAsync(
        string listPartsUrl,
        string headObjectUrl,
        int partsCount,
        int pollInterval = DefaultUploadProgressPollInterval,
        int maxIterations = 0,
        CancellationToken cancellationToken = default)
    {
        RequirePositivePartsCount(partsCount);
        return PollUploadProgressAsync(listPartsUrl, headObjectUrl, partsCount, pollInterval, maxIterations, cancellationToken);
    }

    private async IAsyncEnumerable<double> PollUploadProgressAsync(
        string listPartsUrl,
        string headObjectUrl,
        int partsCount,
        int pollInterval,
        int maxIterations,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        double uploadProgress = 0;
        long timePrevStart = 0;
        var iterations = 0;

        while (uploadProgress < 100)
        {
            if (maxIterations > 0 && iterations >= maxIterations)
            {
                throw new InvalidOperationException($"Upload progress polling exceeded the maximum of {maxIterations} iterations.");
            }

            var timeStart = Environment.TickCount64;
            var delayTime = (int)Math.Max(0, pollInterval - (timeStart - timePrevStart));

            uploadProgress = await GetUploadProgressAsync(listPartsUrl, headObjectUrl, partsCount, delayTime, cancellationToken).ConfigureAwait(false);
            yield return uploadProgress;
            timePrevStart = timeStart;
            iterations++;
        }
    }

    /// <summary>Retrieves the source video path for a media clip.</summary>
    /// <param name="mediaClipId">The media clip ID.</param>
    /// <param name="absolute">If <c>true</c>, returns an absolute URL using the publication's default media asset path.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<string> GetSourcePathAsync(string mediaClipId, bool absolute = true, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync(mediaClipId, null, true, cancellationToken).ConfigureAwait(false);
        response.AssertOk();
        var data = response.Json() as JsonObject;
        if (data is null || !data.ContainsKey("src") || data["src"] is not JsonValue srcValue)
        {
            throw new InvalidOperationException("MediaClip response does not contain a 'src' field.");
        }
        var src = srcValue.ToString();
        return absolute ? await GetAbsoluteVideoPathAsync(src, cancellationToken).ConfigureAwait(false) : src;
    }

    /// <inheritdoc cref="GetSourcePathAsync(string, bool, CancellationToken)" />
    public Task<string> GetSourcePathAsync(long mediaClipId, bool absolute = true, CancellationToken cancellationToken = default)
        => GetSourcePathAsync(Id(mediaClipId), absolute, cancellationToken);

    /// <summary>Converts a relative video path to an absolute URL using publication data.</summary>
    /// <param name="relativeVideoPath">The relative path (with or without leading slash).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<string> GetAbsoluteVideoPathAsync(string relativeVideoPath, CancellationToken cancellationToken = default)
    {
        var publicationData = await Sdk.GetPublicationDataAsync(cancellationToken).ConfigureAwait(false);
        var dmap = publicationData["defaultMediaAssetPath"]?.ToString();
        if (string.IsNullOrEmpty(dmap))
        {
            throw new InvalidOperationException("Publication data missing 'defaultMediaAssetPath'.");
        }
        return $"{dmap}/{relativeVideoPath.TrimStart('/')}";
    }
}
