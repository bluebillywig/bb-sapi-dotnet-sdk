using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Tests.Util;

public class QueryParamsTests
{
    [Fact]
    public void StringsAsIs() => Assert.Equal(new Dictionary<string, string> { ["key"] = "value" }, QueryParams.Build(("key", "value")));

    [Fact]
    public void NumbersToStrings() => Assert.Equal(new Dictionary<string, string> { ["limit"] = "15", ["offset"] = "0" }, QueryParams.Build(("limit", 15), ("offset", 0)));

    [Fact]
    public void BooleansToTrueFalse() => Assert.Equal(new Dictionary<string, string> { ["active"] = "true", ["deleted"] = "false" }, QueryParams.Build(("active", true), ("deleted", false)));

    [Fact]
    public void OmitsNulls() => Assert.Equal(new Dictionary<string, string> { ["key"] = "value" }, QueryParams.Build(("key", "value"), ("empty", null)));

    [Fact]
    public void MixedTypes()
    {
        var query = QueryParams.Build(("name", "test"), ("count", 42), ("active", true), ("missing", null), ("ratio", 2.5));
        Assert.Equal(new Dictionary<string, string> { ["name"] = "test", ["count"] = "42", ["active"] = "true", ["ratio"] = "2.5" }, query);
    }

    [Fact]
    public void EmptyForAllNull() => Assert.Empty(QueryParams.Build(("a", null), ("b", null)));

    [Fact]
    public void AppendKeepsExistingParamsVerbatimAndReplacesSameKey()
    {
        var url = QueryParams.Append("https://s3.example.com/u?X-Amz-Credential=a%2Fb&partNumber=1", new Dictionary<string, string> { ["partNumber"] = "2", ["sort"] = "createddate desc" });

        Assert.Equal("https://s3.example.com/u?X-Amz-Credential=a%2Fb&partNumber=2&sort=createddate%20desc", url);
    }

    [Fact]
    public void GetDecodesPlusAndPercent()
    {
        Assert.Equal("createddate desc", QueryParams.Get("https://x/?sort=createddate+desc", "sort"));
        Assert.Equal("a/b", QueryParams.Get("https://x/?k=a%2Fb", "k"));
        Assert.Null(QueryParams.Get("https://x/?k=1", "missing"));
    }
}
