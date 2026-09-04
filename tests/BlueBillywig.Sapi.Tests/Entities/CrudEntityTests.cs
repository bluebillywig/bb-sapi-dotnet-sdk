using System.Text.Json.Nodes;
using BlueBillywig.Sapi.Tests.Helpers;
using BlueBillywig.Sapi.Types;
using static BlueBillywig.Sapi.Tests.Helpers.TestSdk;

namespace BlueBillywig.Sapi.Tests.Entities;

public class ChannelTests
{
    [Fact]
    public async Task List()
    {
        var (sdk, h) = Create(Ok());
        await sdk.Channel.ListAsync(15, 1, "createddate asc");
        Assert.Equal("15", Query(h.Calls[0].Url, "limit"));
        Assert.Equal("1", Query(h.Calls[0].Url, "offset"));
        Assert.Equal("createddate asc", Query(h.Calls[0].Url, "sort"));
        Assert.Equal("/sapi/channel", PathOf(h.Calls[0].Url));
        Assert.Equal("GET", h.Calls[0].Method);
    }

    [Fact]
    public async Task Get()
    {
        var (sdk, h) = Create(Ok());
        await sdk.Channel.GetAsync(1);
        Assert.Equal("https://my-publication.bbvms.com/sapi/channel/1", h.Calls[0].Url);
        Assert.Equal("GET", h.Calls[0].Method);
    }

    [Fact]
    public async Task CreateChannel()
    {
        var (sdk, h) = TestSdk.Create(Ok());
        await sdk.Channel.CreateAsync(new ChannelProps { Config = new ChannelConfig { PlayIn = "inline" } });
        Assert.Equal("https://my-publication.bbvms.com/sapi/channel", h.Calls[0].Url);
        Assert.Equal("PUT", h.Calls[0].Method);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"config\":{\"playIn\":\"inline\"}}"), JsonNode.Parse(h.Calls[0].BodyText!)));
    }

    [Fact]
    public async Task UpdateChannel()
    {
        var (sdk, h) = TestSdk.Create(Ok());
        await sdk.Channel.UpdateAsync(1, new ChannelProps { Config = new ChannelConfig { PlayIn = "overlay" } });
        Assert.Equal("https://my-publication.bbvms.com/sapi/channel/1", h.Calls[0].Url);
        Assert.Equal("PUT", h.Calls[0].Method);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"config\":{\"playIn\":\"overlay\"}}"), JsonNode.Parse(h.Calls[0].BodyText!)));
    }

    [Fact]
    public async Task DeleteChannel()
    {
        var (sdk, h) = TestSdk.Create(Ok());
        await sdk.Channel.DeleteAsync(1);
        Assert.Equal("https://my-publication.bbvms.com/sapi/channel/1", h.Calls[0].Url);
        Assert.Equal("DELETE", h.Calls[0].Method);
    }
}

public class PlaylistTests
{
    [Fact]
    public async Task List()
    {
        var (sdk, h) = Create(Ok());
        await sdk.Playlist.ListAsync(10, 5, "createddate asc");
        Assert.Equal("10", Query(h.Calls[0].Url, "limit"));
        Assert.Equal("5", Query(h.Calls[0].Url, "offset"));
        Assert.Equal("/sapi/playlist", PathOf(h.Calls[0].Url));
    }

    [Fact]
    public async Task GetCreateUpdateDelete()
    {
        var (sdk, h) = Create(Ok(), Ok(), Ok(), Ok());
        await sdk.Playlist.GetAsync(1);
        await sdk.Playlist.CreateAsync(new PlaylistProps { Title = "My Playlist" });
        await sdk.Playlist.UpdateAsync(1, new PlaylistProps { Title = "My Updated Playlist" });
        await sdk.Playlist.DeleteAsync(1);

        Assert.Equal("https://my-publication.bbvms.com/sapi/playlist/1", h.Calls[0].Url);
        Assert.Equal("GET", h.Calls[0].Method);
        Assert.Equal("https://my-publication.bbvms.com/sapi/playlist", h.Calls[1].Url);
        Assert.Equal("PUT", h.Calls[1].Method);
        Assert.Equal("{\"title\":\"My Playlist\"}", h.Calls[1].BodyText);
        Assert.Equal("https://my-publication.bbvms.com/sapi/playlist/1", h.Calls[2].Url);
        Assert.Equal("{\"title\":\"My Updated Playlist\"}", h.Calls[2].BodyText);
        Assert.Equal("DELETE", h.Calls[3].Method);
    }

    [Fact]
    public async Task MediaClipListAlias()
    {
        var (sdk, h) = Create(Ok());
        await sdk.MediaClipList.GetAsync(1);
        Assert.Equal("https://my-publication.bbvms.com/sapi/playlist/1", h.Calls[0].Url);
    }

    [Fact]
    public async Task StringIdIsEscaped()
    {
        var (sdk, h) = Create(Ok());
        await sdk.Playlist.GetAsync("a b/c");
        Assert.Equal("https://my-publication.bbvms.com/sapi/playlist/a%20b%2Fc", h.Calls[0].Url);
    }
}

public class PlayoutTests
{
    [Fact]
    public async Task ListUsesSapiPlayout()
    {
        var (sdk, h) = Create(Ok());
        await sdk.Playout.ListAsync(10, 5, "createddate asc");
        Assert.Equal("/sapi/playout", PathOf(h.Calls[0].Url));
        Assert.Equal("createddate asc", Query(h.Calls[0].Url, "sort"));
    }

    [Fact]
    public async Task GetCreateUpdateDelete()
    {
        var (sdk, h) = Create(Ok(), Ok(), Ok(), Ok());
        await sdk.Playout.GetAsync(1);
        await sdk.Playout.CreateAsync(new PlayoutProps { Name = "My Playout" });
        await sdk.Playout.UpdateAsync(1, new PlayoutProps { Name = "My Updated Playout" });
        await sdk.Playout.DeleteAsync(1);

        Assert.Equal("https://my-publication.bbvms.com/sapi/playout/1", h.Calls[0].Url);
        Assert.Equal("{\"name\":\"My Playout\"}", h.Calls[1].BodyText);
        Assert.Equal("https://my-publication.bbvms.com/sapi/playout/1", h.Calls[2].Url);
        Assert.Equal("{\"name\":\"My Updated Playout\"}", h.Calls[2].BodyText);
        Assert.Equal("DELETE", h.Calls[3].Method);
    }
}

public class SubtitleTests
{
    [Fact]
    public async Task List()
    {
        var (sdk, h) = Create(Ok());
        await sdk.Subtitle.ListAsync(10, 5, "createddate asc");
        Assert.Equal("/sapi/subtitle", PathOf(h.Calls[0].Url));
    }

    [Fact]
    public async Task GetCreateUpdateDelete()
    {
        var (sdk, h) = Create(Ok(), Ok(), Ok(), Ok());
        await sdk.Subtitle.GetAsync(1);
        await sdk.Subtitle.CreateAsync(new SubtitleProps { MediaclipId = 1, Isocode = "en" });
        await sdk.Subtitle.UpdateAsync(1, new SubtitleProps { Status = "published" });
        await sdk.Subtitle.DeleteAsync(1);

        Assert.Equal("https://my-publication.bbvms.com/sapi/subtitle/1", h.Calls[0].Url);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"mediaclipId\":1,\"isocode\":\"en\"}"), JsonNode.Parse(h.Calls[1].BodyText!)));
        Assert.Equal("{\"status\":\"published\"}", h.Calls[2].BodyText);
        Assert.Equal("DELETE", h.Calls[3].Method);
    }

    [Fact]
    public async Task AdditionalPropertiesAreSentVerbatim()
    {
        var (sdk, h) = Create(Ok());
        await sdk.Subtitle.CreateAsync(new SubtitleProps { Status = "draft", AdditionalProperties = new Dictionary<string, object> { ["custom"] = 42 } });
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"status\":\"draft\",\"custom\":42}"), JsonNode.Parse(h.Calls[0].BodyText!)));
    }
}

public class ThumbnailTests
{
    [Fact]
    public void AbsoluteImagePathWithDimensions()
    {
        var (sdk, _) = Create();
        Assert.Equal("https://my-publication.bbvms.com/image/0/200/some/path/to/an/image", sdk.Thumbnail.GetAbsoluteImagePath("/some/path/to/an/image", 0, 200));
        Assert.Equal("https://my-publication.bbvms.com/image/300/0/some/path/to/an/image", sdk.Thumbnail.GetAbsoluteImagePath("some/path/to/an/image", 300, 0));
    }

    [Fact]
    public void ThrowsForNegativeDimensions()
    {
        var (sdk, _) = Create();
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => sdk.Thumbnail.GetAbsoluteImagePath("some/path", -1, 0));
        Assert.Contains("Given width is lower than 0.", ex.Message);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => sdk.Thumbnail.GetAbsoluteImagePath("some/path", 0, -1));
        Assert.Contains("Given height is lower than 0.", ex.Message);
    }

    [Fact]
    public void PosterUrlFromOvpThumbnailRoute()
    {
        var (sdk, _) = Create();
        Assert.Equal("https://my-publication.bbvms.com/mediaclip/1234/spthumbnail/320/180.webp", sdk.Thumbnail.GetMediaClipPosterPath(1234, 320, 180));
    }

    [Fact]
    public void PosterUrlLetsServiceChooseByDefault()
    {
        var (sdk, _) = Create();
        Assert.Equal("https://my-publication.bbvms.com/mediaclip/1234/spthumbnail/default/default.webp", sdk.Thumbnail.GetMediaClipPosterPath(1234));
    }

    [Fact]
    public void PosterUrlCarriesRpcToken()
    {
        var (sdk, _) = Create();
        Assert.Equal(
            "https://my-publication.bbvms.com/mediaclip/1234/spthumbnail/default/default.webp?useSession=true&rpctoken=12-345678",
            sdk.Thumbnail.GetMediaClipPosterPath(1234, null, null, "12-345678"));
        Assert.DoesNotContain("?", sdk.Thumbnail.GetMediaClipPosterPath(1234, null, null, ""));
    }

    [Theory]
    [InlineData(-1, "default")]
    [InlineData(0, "0")]
    [InlineData(99999, "99999")]
    [InlineData(100000, "default")]
    public void PosterDimensionGuard(int width, string expected)
    {
        var (sdk, _) = Create();
        Assert.Equal($"https://my-publication.bbvms.com/mediaclip/1/spthumbnail/{expected}/default.webp", sdk.Thumbnail.GetMediaClipPosterPath(1, width));
    }

    [Fact]
    public void PosterUrlEscapesStringId()
    {
        var (sdk, _) = Create();
        Assert.Equal("https://my-publication.bbvms.com/mediaclip/a%20b%2Fc/spthumbnail/default/default.webp", sdk.Thumbnail.GetMediaClipPosterPath("a b/c"));
    }
}
