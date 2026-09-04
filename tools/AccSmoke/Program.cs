// Smoke run of the SDK against a real publication. Read-only unless the key carries write roles.
//
//   SAPI_AUTH_KEY=<tokenId>-<secret> SAPI_BASE_URL=https://testsuite.acc.bbvms.com dotnet run
//
// Set SMOKE_UPLOAD_FILE=/path/to/video.mp4 to upload a real media file instead of the bundled
// 179-byte PNG: a file larger than one part exercises the multi-part path, progress polling and
// the wait for SAPI to finish processing the clip.
//
// SAPI_AUTH_KEY follows the bb-sapi CLI convention; SAPI_BASE_URL defaults to the publication's
// production host, so ALWAYS set it when targeting ACC.
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueBillywig.Sapi;
using BlueBillywig.Sapi.Authentication;
using BlueBillywig.Sapi.Exceptions;
using BlueBillywig.Sapi.Search;
using BlueBillywig.Sapi.Types;

var key = Environment.GetEnvironmentVariable("SAPI_AUTH_KEY") ?? throw new InvalidOperationException("Set SAPI_AUTH_KEY=<tokenId>-<secret>");
var publication = Environment.GetEnvironmentVariable("SAPI_PUBLICATION") ?? "testsuite";
var baseUrl = Environment.GetEnvironmentVariable("SAPI_BASE_URL");
var dash = key.IndexOf('-');
var tokenId = int.Parse(key[..dash]);
var secret = key[(dash + 1)..];
var uploadPath = Environment.GetEnvironmentVariable("SMOKE_UPLOAD_FILE") ?? Path.Combine(AppContext.BaseDirectory, "smoke.png");

using var sdk = new Sdk(publication, new RpcTokenAuthenticator(tokenId, secret), new SdkOptions { BaseUri = baseUrl });
Console.WriteLine($"BaseUri {sdk.BaseUri}, token id {tokenId}");

static string Step(string name) { Console.WriteLine($"\n== {name}"); return name; }
static JsonObject Obj(SapiResponse r) { r.AssertOk(); return (JsonObject)r.Json()!; }

long? createdId = null;
var failures = 0;
try
{
    Step("publication data");
    var pub = await sdk.GetPublicationDataAsync();
    Console.WriteLine($"name={pub["name"]} defaultMediaAssetPath={pub["defaultMediaAssetPath"]}");

    Step("list 3 clips");
    var list = Obj(await sdk.MediaClip.ListAsync(3));
    Console.WriteLine($"numfound={list["numfound"]} count={list["count"]}");
    var first = list["items"]!.AsArray()[0]!;
    var firstId = long.Parse(first["id"]!.ToString());
    Console.WriteLine($"first id={firstId} title={first["title"]} status={first["status"]}");

    Step("get first clip (lang=en, no jobs)");
    var got = Obj(await sdk.MediaClip.GetAsync(firstId, "en", false));
    Console.WriteLine($"id={got["id"]} title={got["title"]} src={got["src"]}");

    Step("search: status is published AND title contains 'a'");
    var fs = FilterSet.Create().Where("status", FilterOperator.Is, "published").Where("title", FilterOperator.Contains, "a");
    var search = Obj(await sdk.MediaClip.SearchAsync(fs, 2));
    Console.WriteLine($"filterset={fs} -> numfound={search["numfound"]} count={search["count"]}");

    Step("search: hasInteractivity is true (bool normalised) + isEmpty placeholder");
    var s2 = Obj(await sdk.MediaClip.SearchAsync(FilterSet.Create().Where("hasInteractivity", FilterOperator.Is, true), 1));
    var s3 = Obj(await sdk.MediaClip.SearchAsync(FilterSet.Create().Where("author", FilterOperator.IsEmpty), 1));
    var s4 = Obj(await sdk.MediaClip.SearchAsync(FilterSet.Create().Where("author", FilterOperator.IsNotEmpty), 1));
    Console.WriteLine($"hasInteractivity=true numfound={s2["numfound"]}; author isEmpty={s3["numfound"]} isNotEmpty={s4["numfound"]} (sum vs total {list["numfound"]})");

    Step("search: raw fq escape hatch fq[0]=status:published");
    var s5 = Obj(await sdk.MediaClip.SearchAsync(FilterSet.Create(), 1, filterQueries: new[] { "status:published" }));
    Console.WriteLine($"fq numfound={s5["numfound"]} (filterset published numfound={Obj(await sdk.MediaClip.SearchAsync(FilterSet.Create().Where("status", FilterOperator.Is, "published"), 1))["numfound"]})");

    Step("poster URL fetch");
    var poster = sdk.Thumbnail.GetMediaClipPosterPath(firstId, 320, 180);
    using (var http = new HttpClient())
    {
        var pr = await http.GetAsync(poster);
        Console.WriteLine($"{poster} -> {(int)pr.StatusCode} {pr.Content.Headers.ContentType} {pr.Content.Headers.ContentLength} bytes");
    }

    Step("source path (absolute)");
    try { Console.WriteLine(await sdk.MediaClip.GetSourcePathAsync(firstId)); }
    catch (InvalidOperationException e) { Console.WriteLine("no src: " + e.Message); }

    Step("404 -> SapiClientErrorException");
    try { (await sdk.MediaClip.GetAsync(999999999)).AssertOk(); Console.WriteLine("UNEXPECTED: no throw"); failures++; }
    catch (SapiClientErrorException e) { Console.WriteLine($"ok: {e.StatusCode} {e.Message}"); }

    Step("create clip");
    var created = Obj(await sdk.MediaClip.CreateAsync(new MediaClipProps { Title = $"dotnet-sdk smoke {DateTime.UtcNow:O}", Status = "draft" }));
    createdId = long.Parse(created["id"]!.ToString());
    Console.WriteLine($"created id={createdId} title={created["title"]}");

    Step("update clip");
    var updated = Obj(await sdk.MediaClip.UpdateAsync(createdId.Value, new MediaClipProps { Description = "updated by bb-sapi-dotnet-sdk smoke run" }));
    Console.WriteLine($"description={updated["description"]}");

    Step($"initialize + execute upload ({Path.GetFileName(uploadPath)}, {new FileInfo(uploadPath).Length} bytes)");
    var init = await sdk.MediaClip.InitializeUploadAsync(uploadPath, createdId);
    Console.WriteLine($"init status={init.StatusCode} body={init.Body[..Math.Min(300, init.Body.Length)]}");
    init.AssertOk();
    var upload = init.Json<UploadData>()!;
    Console.WriteLine($"chunks={upload.Chunks} urls={upload.PresignedUrls?.Count} chunkSize={upload.PresignedUrls?[0].ChunkSize} key={upload.Key} uploadId={(string.IsNullOrEmpty(upload.UploadId) ? "(none)" : "set")} listPartsUrl={(upload.ListPartsUrl is null ? "null" : "set")}");
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var ok = await sdk.MediaClip.ExecuteUploadAsync(uploadPath, upload);
    Console.WriteLine($"executeUpload -> {ok} in {sw.Elapsed.TotalSeconds:F1}s");
    if (upload.ListPartsUrl is not null && upload.HeadObjectUrl is not null)
    {
        await foreach (var p in sdk.MediaClip.UploadProgressAsync(upload.ListPartsUrl, upload.HeadObjectUrl, upload.Chunks!.Value, 500, 20))
            Console.WriteLine($"progress {p}%");
    }

    Step("wait for SAPI to process the clip (up to 3 minutes)");
    var deadline = DateTime.UtcNow.AddMinutes(3);
    JsonObject after;
    do
    {
        await Task.Delay(5000);
        after = Obj(await sdk.MediaClip.GetAsync(createdId.Value));
        var jobs = after["jobs"] as JsonArray;
        var jobSummary = jobs is null ? "n/a" : string.Join(",", jobs.Select(j => $"{j?["type"]}:{j?["status"]}"));
        Console.WriteLine($"  src={after["src"]} originalfilename={after["originalfilename"]} length={after["length"]} status={after["status"]} jobs=[{jobSummary}]");
        if (!string.IsNullOrEmpty(after["src"]?.ToString()) && !string.IsNullOrEmpty(after["length"]?.ToString())) break;
    } while (DateTime.UtcNow < deadline);
    if (string.IsNullOrEmpty(after["src"]?.ToString())) { Console.WriteLine("clip not processed within the wait window"); failures++; }
    else
    {
        Console.WriteLine($"source path: {await sdk.MediaClip.GetSourcePathAsync(createdId.Value)}");
        using var http2 = new HttpClient();
        var posterResp = await http2.GetAsync(sdk.Thumbnail.GetMediaClipPosterPath(createdId.Value, 320, 180, new RpcTokenAuthenticator(tokenId, secret).Authenticate()["rpctoken"]));
        Console.WriteLine($"poster of new clip -> {(int)posterResp.StatusCode} {posterResp.Content.Headers.ContentType}");
    }
}
catch (Exception e)
{
    failures++;
    Console.WriteLine($"FAILED: {e.GetType().Name}: {e.Message}");
    if (e is SapiRequestException sre) Console.WriteLine(sre.ResponseBody);
}
finally
{
    if (createdId is not null)
    {
        Step("delete created clip (purge)");
        var del = await sdk.MediaClip.DeleteAsync(createdId.Value, purge: true);
        Console.WriteLine($"delete -> {del.StatusCode} {del.Body[..Math.Min(200, del.Body.Length)]}");
        var gone = await sdk.MediaClip.GetAsync(createdId.Value);
        Console.WriteLine($"get after delete -> {gone.StatusCode}");
    }
}
Console.WriteLine($"\nfailures={failures}");
return failures;
