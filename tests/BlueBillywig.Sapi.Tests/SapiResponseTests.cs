using System.Text.Json;
using System.Text.Json.Nodes;
using BlueBillywig.Sapi.Exceptions;
using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Tests;

public class SapiResponseTests
{
    private static SapiResponse Make(int status, string body = "", Dictionary<string, string>? headers = null)
        => new("https://www.bluebillywig.com/", "GET", status, "", headers ?? new Dictionary<string, string>(), body);

    [Fact]
    public void OkForAll2xx()
    {
        for (var code = 200; code <= 299; code++) Assert.True(Make(code).Ok);
    }

    [Theory]
    [InlineData(100, 199)]
    [InlineData(300, 399)]
    [InlineData(400, 499)]
    [InlineData(500, 599)]
    public void NotOkOutside2xx(int start, int end)
    {
        for (var code = start; code <= end; code++) Assert.False(Make(code).Ok);
    }

    [Fact]
    public void AssertOkDoesNotThrowFor2xx()
    {
        for (var code = 200; code <= 299; code++) Make(code).AssertOk();
    }

    [Theory]
    [InlineData(100, 199, typeof(SapiRequestException))]
    [InlineData(300, 399, typeof(SapiRequestException))]
    [InlineData(400, 499, typeof(SapiClientErrorException))]
    [InlineData(500, 599, typeof(SapiServerErrorException))]
    public void AssertOkThrowsTypedExceptions(int start, int end, Type expected)
    {
        for (var code = start; code <= end; code++)
        {
            var ex = Assert.ThrowsAny<SapiRequestException>(() => Make(code).AssertOk());
            Assert.IsType(expected, ex);
            Assert.Equal(code, ex.StatusCode);
        }
    }

    [Fact]
    public void ExceptionCarriesBodyWhenNonEmpty()
    {
        var ex = Assert.Throws<SapiClientErrorException>(() => Make(400, "error details").AssertOk());
        Assert.Equal("error details", ex.ResponseBody);
    }

    [Fact]
    public void ExceptionBodyNullWhenEmpty()
    {
        var ex = Assert.Throws<SapiClientErrorException>(() => Make(400).AssertOk());
        Assert.Null(ex.ResponseBody);
    }

    [Theory]
    [InlineData(100, 199, HttpStatusCodeCategory.Informational)]
    [InlineData(200, 299, HttpStatusCodeCategory.Successful)]
    [InlineData(300, 399, HttpStatusCodeCategory.Redirection)]
    [InlineData(400, 499, HttpStatusCodeCategory.ClientError)]
    [InlineData(500, 599, HttpStatusCodeCategory.ServerError)]
    public void StatusCategory(int start, int end, HttpStatusCodeCategory expected)
    {
        for (var code = start; code <= end; code++) Assert.Equal(expected, Make(code).StatusCategory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(600)]
    public void OutOfRangeStatusThrows(int code)
        => Assert.Throws<ArgumentOutOfRangeException>(() => HttpStatusCodeCategoryExtensions.GetStatusCodeCategory(code));

    [Fact]
    public void AllOk()
    {
        Assert.True(SapiResponse.AllOk(new[] { Make(200), Make(201), Make(204) }));
        Assert.False(SapiResponse.AllOk(new[] { Make(200), Make(200), Make(404), Make(200) }));
    }

    [Fact]
    public void AssertAllOk()
    {
        SapiResponse.AssertAllOk(new[] { Make(200), Make(201) });
        Assert.Throws<SapiClientErrorException>(() => SapiResponse.AssertAllOk(new[] { Make(200), Make(404), Make(200) }));
    }

    [Fact]
    public void FailedResponsesYieldsOnlyFailures()
    {
        var failed = SapiResponse.FailedResponses(new[] { Make(200), Make(200), Make(404), Make(200), Make(500), Make(200) }).ToList();
        Assert.Equal(2, failed.Count);
    }

    [Fact]
    public void ExposesRawBody() => Assert.Equal("hello world", Make(200, "hello world").Body);

    [Fact]
    public void ParsesJsonBody()
    {
        var data = new JsonObject
        {
            ["object1"] = new JsonObject { ["field1"] = "value1", ["field2"] = "value2" },
            ["object2"] = new JsonObject
            {
                ["object3"] = new JsonObject { ["field3"] = "value3" },
                ["list1"] = new JsonArray("listValue1", "listValue2"),
            },
        };
        var response = Make(200, data.ToJsonString());

        Assert.True(JsonNode.DeepEquals(data, response.Json()));
        Assert.Equal("value1", response.Json<Dictionary<string, JsonElement>>()!["object1"].GetProperty("field1").GetString());
    }

    [Fact]
    public void JsonNullForEmptyBody()
    {
        Assert.Null(Make(200).Json());
        Assert.Null(Make(200).Json<Dictionary<string, string>>());
    }

    [Fact]
    public void JsonThrowsForNonJson() => Assert.ThrowsAny<JsonException>(() => Make(200, "some incorrect value").Json());

    [Fact]
    public void HeaderIsCaseInsensitive()
    {
        var response = Make(200, "", new Dictionary<string, string> { ["Content-Type"] = "application/json" });
        Assert.Equal("application/json", response.Header("content-type"));
        Assert.Equal("application/json", response.Header("Content-Type"));
        Assert.Null(response.Header("X-Missing"));
    }

    [Fact]
    public void QueryParamFromUrl()
    {
        var response = new SapiResponse("https://example.com/path?foo=bar&baz=qux", "GET", 200, "", new Dictionary<string, string>(), "");
        Assert.Equal("bar", response.QueryParam("foo"));
        Assert.Equal("qux", response.QueryParam("baz"));
        Assert.Null(response.QueryParam("missing"));
    }
}
