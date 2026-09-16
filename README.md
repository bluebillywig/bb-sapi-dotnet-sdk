# Blue Billywig SAPI .NET SDK

This .NET SDK provides abstractions to interact with the Blue Billywig Server API (SAPI).
It is a port of the [Node.js SDK](https://github.com/bluebillywig/bb-sapi-node-sdk) and
mirrors its design: the same entities, the same `SapiResponse` shape, the same HOTP RPC-token
authentication and the same upload flow.

## Requirements

- .NET 8.0 or .NET 10.0
- Zero runtime dependencies (`System.Net.Http` and `System.Text.Json` only)

## Installation

```bash
dotnet add package BlueBillywig.Sapi
```

## Quick Start

```csharp
using BlueBillywig.Sapi;

using var sdk = Sdk.WithRpcTokenAuthentication(
    "my-publication",
    1,               // token ID
    "shared-secret"  // shared secret
);

// List media clips
var response = await sdk.MediaClip.ListAsync();
var data = response.Json();
Console.WriteLine(data);
```

## Authentication

The SDK uses HOTP-based RPC token authentication. You need a **token ID** and **shared secret**
from your Blue Billywig publication settings.

```csharp
// Recommended: use the convenience factory
var sdk = Sdk.WithRpcTokenAuthentication("my-publication", tokenId, sharedSecret);

// Or provide a custom authenticator
using BlueBillywig.Sapi.Authentication;

var authenticator = new RpcTokenAuthenticator(tokenId, sharedSecret);
var sdk = new Sdk("my-publication", authenticator);
```

**Clock synchronization**: The RPC token is time-based (HOTP). Both client and server clocks
must be reasonably synchronized (within the token expiration window, default 120 seconds).
Significant clock drift will cause authentication failures.

## Entities

All entities support standard CRUD operations where applicable. IDs are accepted as `long` or
`string`.

| Entity | List | Get | Create | Update | Delete | Search |
|---|---|---|---|---|---|---|
| `sdk.MediaClip` | Yes | Yes | Yes | Yes | Yes | Yes |
| `sdk.Playlist` | Yes | Yes | Yes | Yes | Yes | - |
| `sdk.Channel` | Yes | Yes | Yes | Yes | Yes | - |
| `sdk.Playout` | Yes | Yes | Yes | Yes | Yes | - |
| `sdk.Subtitle` | Yes | Yes | Yes | Yes | Yes | - |

`sdk.MediaClipList` is the legacy alias for `sdk.Playlist`. `sdk.Thumbnail` is not a CRUD
entity: SAPI has no thumbnail records, only image routes, so it provides URL helpers (see
[Thumbnails](#thumbnails)).

### Media Clips

```csharp
using BlueBillywig.Sapi.Types;

// List
var response = await sdk.MediaClip.ListAsync(15, 0, "createddate desc");

// Get (with optional language and job inclusion)
var response = await sdk.MediaClip.GetAsync(123);
var response = await sdk.MediaClip.GetAsync(123, "en", includeJobs: false);

// Create
var response = await sdk.MediaClip.CreateAsync(new MediaClipProps { Title = "My Video" });

// Update
var response = await sdk.MediaClip.UpdateAsync(123, new MediaClipProps { Title = "Updated Title" });

// Delete (with optional purge)
var response = await sdk.MediaClip.DeleteAsync(123);
var response = await sdk.MediaClip.DeleteAsync(123, purge: true);
```

Every `*Props` class has an `AdditionalProperties` dictionary for fields the SDK does not
model; they are sent verbatim.

### Search (filtersets)

`ListAsync` can only page and sort. `SearchAsync` filters, using the same filterset structure
the OVP builds in its filter UI, so a filterset moves between the OVP, the API and this SDK
unchanged. Groups are AND-ed, filters within a group OR-ed.

```csharp
using BlueBillywig.Sapi.Search;

var filterSet = FilterSet.Create()
    .Where("status", FilterOperator.Is, "published")
    .Where("title", FilterOperator.Contains, "koert");

var response = await sdk.MediaClip.SearchAsync(filterSet);

// OR within a group
var either = FilterSet.Create().AndGroup(
    new Filter("status", FilterOperator.Is, "published"),
    new Filter("status", FilterOperator.Is, "draft"));

// Ingest a filterset saved by the OVP (bare list or SearchRequest envelope)
var fromOvp = FilterSet.From(json);
```

The filterset is sent as JSON and compiled by SAPI; it is deliberately not compiled
client-side. Numbers and booleans are normalised to strings on the wire, presence operators
(`IsEmpty` / `IsNotEmpty`) carry the `*` placeholder the backend requires, and filters with a
blank value are dropped. Raw Solr filters can be passed through `filterQueries`; they go out as
indexed `fq[0]` parameters, the only form SAPI reads.

### Playlists, Channels, Playouts, Subtitles

```csharp
// All follow the same pattern
var response = await sdk.Playlist.ListAsync();
var response = await sdk.Playlist.GetAsync(1);
var response = await sdk.Playlist.CreateAsync(new PlaylistProps { Title = "My Playlist" });
var response = await sdk.Playlist.UpdateAsync(1, new PlaylistProps { Title = "Updated" });
var response = await sdk.Playlist.DeleteAsync(1);
```

## File Uploads

The SDK supports single-chunk and multi-part uploads to S3 via presigned URLs. Each part is
sent as a sized body (`Content-Length`, no chunked transfer encoding), one part in memory at a
time.

```csharp
// 1. Initialize the upload
var initResponse = await sdk.MediaClip.InitializeUploadAsync("/path/to/video.mp4");
initResponse.AssertOk();
var uploadData = initResponse.Json<UploadData>()!;

// 2. Execute the upload
var success = await sdk.MediaClip.ExecuteUploadAsync("/path/to/video.mp4", uploadData);

// 3. (Optional) Track upload progress
await foreach (var progress in sdk.MediaClip.UploadProgressAsync(
    uploadData.ListPartsUrl!, uploadData.HeadObjectUrl!, uploadData.Chunks!.Value))
{
    Console.WriteLine($"Upload progress: {progress}%");
}
```

## Thumbnails

```csharp
// Absolute thumbnail URL with dimensions
var url = sdk.Thumbnail.GetAbsoluteImagePath("/path/to/image.jpg", 640, 360);
// => https://my-publication.bbvms.com/image/640/360/path/to/image.jpg

// A media clip's poster (never build this from clip.src: that is the source media file)
var poster = sdk.Thumbnail.GetMediaClipPosterPath(1234, 320, 180);
// => https://my-publication.bbvms.com/mediaclip/1234/spthumbnail/320/180.webp

// Draft clips need a READ-ONLY RPC token (the URL ends up in page source, and is short-lived)
var draftPoster = sdk.Thumbnail.GetMediaClipPosterPath(1234, rpcToken: "12-…");
```

## Response Handling

All entity methods return a `SapiResponse`:

```csharp
var response = await sdk.MediaClip.GetAsync(123);

// Check status
if (response.Ok)
{
    var node = response.Json();          // JsonNode
    var typed = response.Json<MyType>(); // deserialized with System.Text.Json
}

// Or assert (throws on non-2xx)
response.AssertOk();

// Access response details
response.StatusCode;            // e.g. 200
response.Body;                  // raw body string
response.Header("Content-Type");
response.QueryParam("limit");   // query param from the request URL
response.StatusCategory;        // HttpStatusCodeCategory enum

// Batch response utilities
SapiResponse.AllOk(responses);
SapiResponse.AssertAllOk(responses);
SapiResponse.FailedResponses(responses);
```

## Error Handling

The SDK throws typed exceptions. The Node SDK's `HTTP*Exception` names are prefixed `Sapi` here
to avoid clashing with `System.Net.Http.HttpRequestException`.

| Node SDK | .NET SDK |
|---|---|
| `HTTPRequestException` | `SapiRequestException` |
| `HTTPClientErrorException` (4xx) | `SapiClientErrorException` |
| `HTTPServerErrorException` (5xx) | `SapiServerErrorException` |
| `HTTPConnectionException` (transport / timeout) | `SapiConnectionException` |

```csharp
using BlueBillywig.Sapi.Exceptions;

try
{
    var response = await sdk.MediaClip.GetAsync(999);
    response.AssertOk();
}
catch (SapiClientErrorException e)
{
    Console.Error.WriteLine($"Client error {e.StatusCode}: {e.Message}");
    Console.Error.WriteLine($"Response body: {e.ResponseBody}");
}
catch (SapiServerErrorException e)
{
    Console.Error.WriteLine($"Server error {e.StatusCode}: {e.Message}");
}
catch (SapiConnectionException e)
{
    // No response received: network failure or timeout. StatusCode is 0, InnerException has the cause.
}
```

## Configuration

```csharp
var sdk = Sdk.WithRpcTokenAuthentication("my-publication", tokenId, sharedSecret, new SdkOptions
{
    // Override the base URI (useful for testing or custom deployments)
    BaseUri = "https://custom-host.example.com",

    // Inject an HttpClient (testing, or an IHttpClientFactory-managed client)
    HttpClient = myHttpClient,

    // Default per-request timeout (TimeSpan.Zero disables). Default 30 seconds.
    Timeout = TimeSpan.FromSeconds(30),
});
```

Authentication headers are attached only to same-origin (SAPI) requests; cross-origin URLs
such as presigned S3 upload and progress URLs never receive the RPC token.

## Development

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack src/BlueBillywig.Sapi
```
