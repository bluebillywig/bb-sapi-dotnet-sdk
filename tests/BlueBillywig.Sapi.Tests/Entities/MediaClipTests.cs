using System.Text.Json.Nodes;
using BlueBillywig.Sapi.Exceptions;
using BlueBillywig.Sapi.Tests.Helpers;
using BlueBillywig.Sapi.Types;
using static BlueBillywig.Sapi.Tests.Helpers.TestSdk;

namespace BlueBillywig.Sapi.Tests.Entities;

public class MediaClipTests
{
    [Fact]
    public async Task ListWithCorrectParams()
    {
        var (sdk, h) = Create(Ok());
        await sdk.MediaClip.ListAsync(15, 1, "createddate asc");
        Assert.Equal("15", Query(h.Calls[0].Url, "limit"));
        Assert.Equal("1", Query(h.Calls[0].Url, "offset"));
        Assert.Equal("createddate asc", Query(h.Calls[0].Url, "sort"));
        Assert.Equal("/sapi/mediaclip", PathOf(h.Calls[0].Url));
        Assert.Equal("GET", h.Calls[0].Method);
    }

    [Fact]
    public async Task Delete()
    {
        var (sdk, h) = Create(Ok());
        await sdk.MediaClip.DeleteAsync(1);
        Assert.Equal("https://my-publication.bbvms.com/sapi/mediaclip/1", h.Calls[0].Url);
        Assert.Equal("DELETE", h.Calls[0].Method);
    }

    [Fact]
    public async Task DeleteWithPurge()
    {
        var (sdk, h) = Create(Ok());
        await sdk.MediaClip.DeleteAsync(1, true);
        Assert.Equal("true", Query(h.Calls[0].Url, "purge"));
        Assert.Equal("/sapi/mediaclip/1", PathOf(h.Calls[0].Url));
    }

    [Fact]
    public async Task InitializeUploadThrowsForMissingFile()
    {
        var (sdk, _) = Create();
        const string path = "./path/to/a/non/existing/mediaclip/file";
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => sdk.MediaClip.InitializeUploadAsync(path));
        Assert.Contains($"File {path} is not a file or does not exist.", ex.Message);
    }

    [Fact]
    public async Task InitializeUploadWithCorrectParams()
    {
        var (sdk, h) = Create(Ok());
        var (path, cleanup) = TempFile(10, 0x41);
        try
        {
            await sdk.MediaClip.InitializeUploadAsync(path);
            Assert.Equal("/sapi/mediaclip/0/upload", PathOf(h.Calls[0].Url));
            Assert.Equal(Path.GetFileName(path), Query(h.Calls[0].Url, "filename"));
            Assert.Equal("10", Query(h.Calls[0].Url, "filesize"));
            Assert.Equal("application/octet-stream", Query(h.Calls[0].Url, "contenttype"));
            Assert.Null(Query(h.Calls[0].Url, "clipid"));
            Assert.Equal("GET", h.Calls[0].Method);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task InitializeUploadWithMediaClipId()
    {
        var (sdk, h) = Create(Ok());
        var (path, cleanup) = TempFile(10, 0x41);
        try
        {
            await sdk.MediaClip.InitializeUploadAsync(path, 1);
            Assert.Equal("1", Query(h.Calls[0].Url, "clipid"));
        }
        finally { cleanup(); }
    }

    [Theory]
    [InlineData("clip.mp4", "video/mp4")]
    [InlineData("CLIP.MOV", "video/quicktime")]
    [InlineData("a.jpg", "image/jpeg")]
    [InlineData("a.unknown", "application/octet-stream")]
    public void MimeTypeByExtension(string file, string expected) => Assert.Equal(expected, Sapi.Entities.MediaClip.GetMimeType(file));

    [Fact]
    public async Task AbortUpload()
    {
        var (sdk, h) = Create(Ok());
        await sdk.MediaClip.AbortUploadAsync("/prefix/my-video.mp4", "12345");
        Assert.Equal("/prefix/my-video.mp4", Query(h.Calls[0].Url, "s3filekey"));
        Assert.Equal("12345", Query(h.Calls[0].Url, "s3uploadid"));
        Assert.Equal("/sapi/mediaclip/0/abortUpload", PathOf(h.Calls[0].Url));
        Assert.Equal("PUT", h.Calls[0].Method);
    }

    [Fact]
    public async Task CompleteUpload()
    {
        var (sdk, h) = Create(Ok());
        var parts = new[] { new S3Part("12345", "1"), new S3Part("12346", "2"), new S3Part("12347", "3") };

        await sdk.MediaClip.CompleteUploadAsync("/prefix/my-video.mp4", "12345", parts);

        Assert.Equal("https://my-publication.bbvms.com/sapi/mediaclip/0/completeUpload", h.Calls[0].Url);
        Assert.Equal("PUT", h.Calls[0].Method);
        var expected = JsonNode.Parse("{\"s3FileKey\":\"/prefix/my-video.mp4\",\"s3UploadId\":\"12345\",\"s3Parts\":[{\"ETag\":\"12345\",\"PartNumber\":\"1\"},{\"ETag\":\"12346\",\"PartNumber\":\"2\"},{\"ETag\":\"12347\",\"PartNumber\":\"3\"}]}");
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(h.Calls[0].BodyText!)), h.Calls[0].BodyText);
    }

    [Fact]
    public async Task Get()
    {
        var (sdk, h) = Create(Ok());
        await sdk.MediaClip.GetAsync(1);
        Assert.Equal("true", Query(h.Calls[0].Url, "includejobs"));
        Assert.Null(Query(h.Calls[0].Url, "lang"));
        Assert.Equal("/sapi/mediaclip/1", PathOf(h.Calls[0].Url));
        Assert.Equal("GET", h.Calls[0].Method);
    }

    [Fact]
    public async Task GetWithLangAndNoJobs()
    {
        var (sdk, h) = Create(Ok());
        await sdk.MediaClip.GetAsync(1, "en", false);
        Assert.Equal("false", Query(h.Calls[0].Url, "includejobs"));
        Assert.Equal("en", Query(h.Calls[0].Url, "lang"));
    }

    [Fact]
    public async Task CreateAndUpdate()
    {
        var (sdk, h) = Create(Ok(), Ok(), Ok(), Ok());
        var props = new MediaClipProps { Title = "My Mediaclip" };

        await sdk.MediaClip.CreateAsync(props);
        await sdk.MediaClip.CreateAsync(props, true, "en");
        await sdk.MediaClip.UpdateAsync(1, props);
        await sdk.MediaClip.UpdateAsync(1, props, true, "en");

        Assert.Equal("false", Query(h.Calls[0].Url, "softsave"));
        Assert.Equal("/sapi/mediaclip", PathOf(h.Calls[0].Url));
        Assert.Equal("PUT", h.Calls[0].Method);
        Assert.Equal("{\"title\":\"My Mediaclip\"}", h.Calls[0].BodyText);
        Assert.Equal("true", Query(h.Calls[1].Url, "softsave"));
        Assert.Equal("en", Query(h.Calls[1].Url, "lang"));
        Assert.Equal("/sapi/mediaclip/1", PathOf(h.Calls[2].Url));
        Assert.Equal("false", Query(h.Calls[2].Url, "softsave"));
        Assert.Equal("{\"title\":\"My Mediaclip\"}", h.Calls[2].BodyText);
        Assert.Equal("true", Query(h.Calls[3].Url, "softsave"));
        Assert.Equal("en", Query(h.Calls[3].Url, "lang"));
    }

    [Fact]
    public async Task ContractInterfacesRouteToDefaults()
    {
        var (sdk, h) = Create(Ok(), Ok(), Ok(), Ok());
        Sapi.Contracts.IGettable gettable = sdk.MediaClip;
        Sapi.Contracts.ICreatable<MediaClipProps> creatable = sdk.MediaClip;
        Sapi.Contracts.IUpdatable<MediaClipProps> updatable = sdk.MediaClip;
        Sapi.Contracts.IDeletable deletable = sdk.MediaClip;

        await gettable.GetAsync("1");
        await creatable.CreateAsync(new MediaClipProps { Title = "t" });
        await updatable.UpdateAsync("1", new MediaClipProps { Title = "t" });
        await deletable.DeleteAsync("1");

        Assert.Equal("true", Query(h.Calls[0].Url, "includejobs"));
        Assert.Equal("false", Query(h.Calls[1].Url, "softsave"));
        Assert.Equal("false", Query(h.Calls[2].Url, "softsave"));
        Assert.Null(Query(h.Calls[3].Url, "purge"));
    }

    private static UploadData ThreePartUpload() => new()
    {
        Key = "/prefix/blank.mp4",
        UploadId = "12345",
        Chunks = 3,
        PresignedUrls = new List<PresignedUrl>
        {
            new() { PresignedUrlValue = "https://s3.example.com/presigned-url/1?partNumber=1", ChunkSize = 10, Offset = 0 },
            new() { PresignedUrlValue = "https://s3.example.com/presigned-url/2?partNumber=2", ChunkSize = 10, Offset = 10 },
            new() { PresignedUrlValue = "https://s3.example.com/presigned-url/3?partNumber=3", ChunkSize = 10, Offset = 20 },
        },
    };

    [Fact]
    public async Task ExecuteSingleChunkUpload()
    {
        var (sdk, h) = Create(Ok());
        var (path, cleanup) = TempFile(10, 0x41);
        try
        {
            var result = await sdk.MediaClip.ExecuteUploadAsync(path, new UploadData
            {
                Chunks = 1,
                PresignedUrls = new List<PresignedUrl> { new() { PresignedUrlValue = "https://s3.example.com/presigned-url", ChunkSize = 10 } },
            });

            Assert.True(result);
            Assert.Single(h.Calls);
            Assert.Equal("PUT", h.Calls[0].Method);
            Assert.Equal("https://s3.example.com/presigned-url", h.Calls[0].Url);
            // A sized body so S3 gets a Content-Length and no chunked Transfer-Encoding.
            Assert.Equal("10", h.Calls[0].Header("Content-Length"));
            Assert.Null(h.Calls[0].Header("Transfer-Encoding"));
            Assert.Equal(new string('A', 10), h.Calls[0].BodyText);
            Assert.Null(h.Calls[0].Header("rpctoken"));
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task ChunkSizeDefaultsToFileSize()
    {
        var (sdk, h) = Create(Ok());
        var (path, cleanup) = TempFile(10, 0x41);
        try
        {
            var result = await sdk.MediaClip.ExecuteUploadAsync(path, new UploadData
            {
                Chunks = 1,
                PresignedUrls = new List<PresignedUrl> { new() { PresignedUrlValue = "https://s3.example.com/presigned-url" } },
            });
            Assert.True(result);
            Assert.Equal(10, h.Calls[0].Body!.Length);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task ExecuteMultiChunkUpload()
    {
        var (sdk, h) = Create(
            new MockResponse { Headers = new() { ["ETag"] = "\"some-etag-1\"" } },
            new MockResponse { Headers = new() { ["ETag"] = "\"some-etag-2\"" } },
            new MockResponse { Headers = new() { ["ETag"] = "\"some-etag-3\"" } },
            Ok());
        var (path, cleanup) = TempFile(30, 0x42);
        try
        {
            var result = await sdk.MediaClip.ExecuteUploadAsync(path, ThreePartUpload());

            Assert.True(result);
            Assert.Equal(4, h.Calls.Count);
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal("PUT", h.Calls[i].Method);
                Assert.Equal(10, h.Calls[i].Body!.Length);
            }

            var complete = h.Calls[3];
            Assert.Equal("PUT", complete.Method);
            Assert.Equal("/sapi/mediaclip/0/completeUpload", PathOf(complete.Url));
            var body = JsonNode.Parse(complete.BodyText!)!.AsObject();
            Assert.Equal("/prefix/blank.mp4", body["s3FileKey"]!.ToString());
            Assert.Equal("12345", body["s3UploadId"]!.ToString());
            var parts = body["s3Parts"]!.AsArray();
            Assert.Equal(3, parts.Count);
            Assert.Equal(new[] { "1", "2", "3" }, parts.Select(p => p!["PartNumber"]!.ToString()).OrderBy(x => x));
            Assert.All(parts, p => Assert.Matches("^some-etag-\\d$", p!["ETag"]!.ToString()));
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task HandlesMissingETagAndPartNumber()
    {
        var (sdk, h) = Create(Ok(), Ok(), Ok());
        var (path, cleanup) = TempFile(20, 0x44);
        try
        {
            var result = await sdk.MediaClip.ExecuteUploadAsync(path, new UploadData
            {
                Key = "/prefix/blank.mp4",
                UploadId = "12345",
                Chunks = 2,
                PresignedUrls = new List<PresignedUrl>
                {
                    new() { PresignedUrlValue = "https://s3.example.com/presigned-url/1", ChunkSize = 10, Offset = 0 },
                    new() { PresignedUrlValue = "https://s3.example.com/presigned-url/2?partNumber=2", ChunkSize = 10, Offset = 10 },
                },
            });

            Assert.True(result);
            var parts = JsonNode.Parse(h.Calls[2].BodyText!)!["s3Parts"]!.AsArray();
            Assert.Equal("", parts[0]!["ETag"]!.ToString());
            Assert.Equal("", parts[1]!["ETag"]!.ToString());
            var numbers = parts.Select(p => p!["PartNumber"]!.ToString()).ToList();
            Assert.Contains("", numbers);
            Assert.Contains("2", numbers);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task AbortsUploadOnChunkFailure()
    {
        var (sdk, h) = Create(
            new MockResponse { Headers = new() { ["ETag"] = "\"some-etag-1\"" } },
            new MockResponse { Headers = new() { ["ETag"] = "\"some-etag-2\"" } },
            new MockResponse { Status = 500, StatusText = "Internal Server Error" },
            Ok());
        var (path, cleanup) = TempFile(30, 0x43);
        try
        {
            await Assert.ThrowsAsync<SapiServerErrorException>(() => sdk.MediaClip.ExecuteUploadAsync(path, ThreePartUpload()));

            Assert.Equal(4, h.Calls.Count);
            var abort = h.Calls[3];
            Assert.Equal("PUT", abort.Method);
            Assert.Equal("/sapi/mediaclip/0/abortUpload", PathOf(abort.Url));
            Assert.Equal("/prefix/blank.mp4", Query(abort.Url, "s3filekey"));
            Assert.Equal("12345", Query(abort.Url, "s3uploadid"));
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task StillThrowsOriginalErrorWhenAbortFails()
    {
        var (sdk, _) = Create(
            new MockResponse { Headers = new() { ["ETag"] = "\"some-etag-1\"" } },
            new MockResponse { Headers = new() { ["ETag"] = "\"some-etag-2\"" } },
            new MockResponse { Status = 500, StatusText = "Internal Server Error" },
            new MockResponse { Status = 500, StatusText = "Internal Server Error" });
        var (path, cleanup) = TempFile(30, 0x43);
        try
        {
            await Assert.ThrowsAsync<SapiServerErrorException>(() => sdk.MediaClip.ExecuteUploadAsync(path, ThreePartUpload()));
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task ExecuteUploadThrowsForInvalidPath()
    {
        var (sdk, _) = Create();
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => sdk.MediaClip.ExecuteUploadAsync("./nonexistent", new UploadData
        {
            Chunks = 1,
            PresignedUrls = new List<PresignedUrl> { new() { PresignedUrlValue = "https://s3.example.com/url" } },
        }));
        Assert.Contains("is not a file or does not exist", ex.Message);
    }

    [Fact]
    public async Task ExecuteUploadThrowsForMissingChunksOrUrls()
    {
        var (sdk, _) = Create();
        var (path, cleanup) = TempFile(10, 0x41);
        try
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sdk.MediaClip.ExecuteUploadAsync(path, new UploadData()));
            Assert.Contains("uploadData must contain 'chunks' and 'presignedUrls' keys.", ex.Message);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task ExecuteUploadThrowsForMultiPartMissingKeyOrUploadId()
    {
        var (sdk, _) = Create();
        var (path, cleanup) = TempFile(10, 0x41);
        try
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sdk.MediaClip.ExecuteUploadAsync(path, new UploadData
            {
                Chunks = 3,
                PresignedUrls = new List<PresignedUrl>
                {
                    new() { PresignedUrlValue = "https://s3.example.com/url/1" },
                    new() { PresignedUrlValue = "https://s3.example.com/url/2" },
                    new() { PresignedUrlValue = "https://s3.example.com/url/3" },
                },
            }));
            Assert.Contains("uploadData for multi-part uploads must contain 'key' and 'uploadId' keys.", ex.Message);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task UploadDataDeserializesFromSapiJson()
    {
        var (sdk, _) = Create(Ok("{\"chunks\":2,\"key\":\"k\",\"uploadId\":\"u\",\"listPartsUrl\":\"https://s3/l\",\"headObjectUrl\":\"https://s3/h\",\"presignedUrls\":[{\"presignedUrl\":\"https://s3/1\",\"offset\":0,\"chunkSize\":5},{\"presignedUrl\":\"https://s3/2\",\"offset\":5,\"chunkSize\":5}]}"));
        var response = await sdk.SendRequestAsync("GET", "/sapi/mediaclip/0/upload");

        var data = response.Json<UploadData>()!;

        Assert.Equal(2, data.Chunks);
        Assert.Equal("k", data.Key);
        Assert.Equal("https://s3/l", data.ListPartsUrl);
        Assert.Equal(2, data.PresignedUrls!.Count);
        Assert.Equal(5, data.PresignedUrls[1].Offset);
        Assert.Equal("https://s3/2", data.PresignedUrls[1].PresignedUrlValue);
    }

    private const string ListParts = "https://s3.example.com/list-part";
    private const string HeadObject = "https://s3.example.com/head-object";

    [Fact]
    public async Task Progress0WhenNotStarted()
    {
        var (sdk, _) = Create(new MockResponse { Status = 404 }, new MockResponse { Status = 404 });
        Assert.Equal(0, await sdk.MediaClip.GetUploadProgressAsync(ListParts, HeadObject, 5));
    }

    [Fact]
    public async Task Progress100WhenCompleted()
    {
        var (sdk, h) = Create(new MockResponse { Status = 404 }, Ok());
        Assert.Equal(100, await sdk.MediaClip.GetUploadProgressAsync(ListParts, HeadObject, 5));
        Assert.Equal("HEAD", h.Calls[1].Method);
    }

    [Fact]
    public async Task ProgressForSinglePartObject()
    {
        var (sdk, _) = Create(Ok("{\"Part\":{\"PartNumber\":1}}"));
        Assert.Equal(20, await sdk.MediaClip.GetUploadProgressAsync(ListParts, HeadObject, 5));
    }

    [Fact]
    public async Task ProgressForMultipleParts()
    {
        var (sdk, _) = Create(Ok("{\"Part\":[{},{},{}]}"), Ok("{\"Part\":[{},{},{},{},{}]}"), Ok("{}"));
        Assert.Equal(60, await sdk.MediaClip.GetUploadProgressAsync(ListParts, HeadObject, 5));
        Assert.Equal(100, await sdk.MediaClip.GetUploadProgressAsync(ListParts, HeadObject, 5));
        Assert.Equal(0, await sdk.MediaClip.GetUploadProgressAsync(ListParts, HeadObject, 5));
    }

    [Fact]
    public async Task ProgressThrowsOnUnexpectedStatus()
    {
        var (sdk, _) = Create(new MockResponse { Status = 500, StatusText = "Internal Server Error" });
        await Assert.ThrowsAsync<SapiServerErrorException>(() => sdk.MediaClip.GetUploadProgressAsync(ListParts, HeadObject, 5));
    }

    [Fact]
    public async Task ProgressStreamYieldsUntil100()
    {
        var (sdk, _) = Create(
            new MockResponse { Status = 404 }, new MockResponse { Status = 404 },
            Ok("{\"Part\":[{}]}"), Ok("{\"Part\":[{},{}]}"), Ok("{\"Part\":[{},{},{}]}"), Ok("{\"Part\":[{},{},{},{}]}"),
            new MockResponse { Status = 404 }, Ok());

        var progress = new List<double>();
        await foreach (var p in sdk.MediaClip.UploadProgressAsync(ListParts, HeadObject, 5, 10))
        {
            progress.Add(p);
        }

        Assert.Equal(new double[] { 0, 20, 40, 60, 80, 100 }, progress);
    }

    [Fact]
    public async Task ProgressStreamThrowsWhenMaxIterationsExceeded()
    {
        var (sdk, _) = Create(new MockResponse { Status = 404 }, new MockResponse { Status = 404 }, new MockResponse { Status = 404 }, new MockResponse { Status = 404 });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in sdk.MediaClip.UploadProgressAsync(ListParts, HeadObject, 5, 10, 2)) { }
        });
        Assert.Equal("Upload progress polling exceeded the maximum of 2 iterations.", ex.Message);
    }

    [Fact]
    public async Task SourcePathThrowsWithoutSrc()
    {
        var (sdk, _) = Create(Ok("{\"title\":\"No src here\"}"));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sdk.MediaClip.GetSourcePathAsync(1));
        Assert.Equal("MediaClip response does not contain a 'src' field.", ex.Message);
    }

    [Fact]
    public async Task RelativeSourcePath()
    {
        var (sdk, _) = Create(Ok("{\"src\":\"/some/source/of/mediaclip.mp4\"}"));
        Assert.Equal("/some/source/of/mediaclip.mp4", await sdk.MediaClip.GetSourcePathAsync(1, false));
    }

    [Fact]
    public async Task AbsoluteSourcePath()
    {
        var (sdk, _) = Create(Ok("{\"src\":\"/some/source/of/mediaclip.mp4\"}"), Ok("{\"defaultMediaAssetPath\":\"https://my-cfn.bluebillywig.com\"}"));
        Assert.Equal("https://my-cfn.bluebillywig.com/some/source/of/mediaclip.mp4", await sdk.MediaClip.GetSourcePathAsync(1, true));
    }

    [Fact]
    public async Task AbsoluteSourcePathThrowsWithoutDefaultMediaAssetPath()
    {
        var (sdk, _) = Create(Ok("{\"src\":\"/x.mp4\"}"), Ok("{}"));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sdk.MediaClip.GetSourcePathAsync(1, true));
        Assert.Equal("Publication data missing 'defaultMediaAssetPath'.", ex.Message);
    }
}
