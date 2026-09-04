using System.Text.Json;
using System.Text.Json.Nodes;
using BlueBillywig.Sapi.Search;
using BlueBillywig.Sapi.Tests.Helpers;
using BlueBillywig.Sapi.Util;
using static BlueBillywig.Sapi.Tests.Helpers.TestSdk;

namespace BlueBillywig.Sapi.Tests.Search;

/// <summary>
/// The filterset wire format. These pin the SHAPE, because the shape is the contract; the
/// semantics are SAPI's to own. The Node SDK verified equivalence against a live publication:
/// sending a filterset and a hand-compiled fq return identical counts.
/// </summary>
public class FilterSetTests
{
    [Fact]
    public void OneConditionIsAGroupOfOne()
    {
        var groups = FilterSet.Create().Where("status", FilterOperator.Is, "published").ToArray();

        var filter = Assert.Single(Assert.Single(groups).Filters);
        Assert.Equal("status", filter.Field);
        Assert.Equal(FilterOperator.Is, filter.Operator);
        Assert.Equal("published", filter.Value);
        Assert.Null(filter.Type);
    }

    [Fact]
    public void EachWhereIsItsOwnGroup()
    {
        var filterSet = FilterSet.Create().Where("status", FilterOperator.Is, "published").Where("mediatype", FilterOperator.Is, "video");
        Assert.Equal(2, filterSet.ToArray().Count);
    }

    [Fact]
    public void FiltersPassedTogetherShareAGroup()
    {
        var filterSet = FilterSet.Create().AndGroup(
            new Filter("status", FilterOperator.Is, "published"),
            new Filter("status", FilterOperator.Is, "draft"));

        var groups = filterSet.ToArray();
        Assert.Single(groups);
        Assert.Equal(2, groups[0].Filters.Count);
    }

    [Fact]
    public void CarriesEntityTypeButOmitsItWhenAbsent()
    {
        Assert.Equal("mediaclip", FilterSet.Create().Where("status", FilterOperator.Is, "published", "mediaclip").ToArray()[0].Filters[0].Type);
        Assert.Null(FilterSet.Create().Where("status", FilterOperator.Is, "published").ToArray()[0].Filters[0].Type);
        Assert.DoesNotContain("type", FilterSet.Create().Where("status", FilterOperator.Is, "published").ToJson());
    }

    [Fact]
    public void CarriesSeveralValuesAsAList()
    {
        var value = FilterSet.Create().Where("status", FilterOperator.IsAnyOf, new[] { "published", "draft" }).ToArray()[0].Filters[0].Value;
        Assert.Equal(new[] { "published", "draft" }, Assert.IsAssignableFrom<IReadOnlyList<string>>(value));
    }

    [Fact]
    public void DropsAFilterWithNothingToMatchOn()
    {
        Assert.Empty(FilterSet.Create().Where("status", FilterOperator.Is, "   ").ToArray());
        Assert.True(FilterSet.Create().Where("status", FilterOperator.Is).IsEmpty());
        Assert.True(FilterSet.Create().Where("status", FilterOperator.Is, Array.Empty<string>()).IsEmpty());
    }

    [Fact]
    public void SendsThePlaceholderForPresenceOperators()
    {
        // The backend's compiler skips ANY filter whose value is empty, presence tests included,
        // so a bare isEmpty silently never fires. OVP6 sends '*'; the SDK must do the same.
        var filterSet = FilterSet.Create().Where("author", FilterOperator.IsEmpty);

        Assert.False(filterSet.IsEmpty());
        Assert.Equal("*", filterSet.ToArray()[0].Filters[0].Value);
    }

    [Fact]
    public void PlaceholderOverridesSuppliedValue()
        => Assert.Equal("*", FilterSet.Create().Where("author", FilterOperator.IsNotEmpty, "anything").ToArray()[0].Filters[0].Value);

    [Fact]
    public void NormalisesNumbersAndBooleansToStrings()
    {
        // A JSON number works, but a JSON boolean gets mangled into "1" by the backend and matches
        // nothing; a boolean must go out as the string 'true'/'false'.
        Assert.Equal("100", FilterSet.Create().Where("views", FilterOperator.IsGreaterThan, 100).ToArray()[0].Filters[0].Value);
        Assert.Equal("true", FilterSet.Create().Where("hasInteractivity", FilterOperator.Is, true).ToArray()[0].Filters[0].Value);
        Assert.Equal("false", FilterSet.Create().Where("isImported", FilterOperator.Is, false).ToArray()[0].Filters[0].Value);
        Assert.Equal("2.5", FilterSet.Create().Where("ratio", FilterOperator.Is, 2.5).ToArray()[0].Filters[0].Value);
        var list = FilterSet.Create().Where("views", FilterOperator.IsAnyOf, new object[] { 1, 2.5, true }).ToArray()[0].Filters[0].Value;
        Assert.Equal(new[] { "1", "2.5", "true" }, Assert.IsAssignableFrom<IReadOnlyList<string>>(list));
    }

    [Fact]
    public void DropsNonScalarArrayMembers()
    {
        var value = FilterSet.From("[{\"filters\":[{\"field\":\"status\",\"operator\":\"is\",\"value\":[\"published\",{\"nested\":true}]}]}]").ToArray()[0].Filters[0].Value;
        Assert.Equal(new[] { "published" }, Assert.IsAssignableFrom<IReadOnlyList<string>>(value));
    }

    [Fact]
    public void SkipsAGroupWithoutAFiltersArray()
    {
        var filterSet = FilterSet.From("[{\"notFilters\":true},{\"filters\":[{\"field\":\"status\",\"operator\":\"is\",\"value\":\"published\"}]}]");
        Assert.Equal("[{\"filters\":[{\"field\":\"status\",\"operator\":\"is\",\"value\":\"published\"}]}]", filterSet.ToJson());
    }

    [Fact]
    public void ToleratesJunkShapesWithoutCrashing()
    {
        Assert.True(FilterSet.From("{\"type\":\"SearchRequest\",\"filterSet\":\"junk\"}").IsEmpty());
        Assert.True(FilterSet.From("[{\"filters\":[null]}]").IsEmpty());
        Assert.True(FilterSet.From("null").IsEmpty());
        Assert.True(FilterSet.From("[{\"filters\":[{\"operator\":\"is\",\"value\":\"x\"}]}]").IsEmpty());
    }

    [Fact]
    public void UnknownOperatorIsADescriptiveError()
    {
        var ex = Assert.Throws<ArgumentException>(() => FilterSet.From("[{\"filters\":[{\"field\":\"status\",\"operator\":\"Is\",\"value\":\"x\"}]}]"));
        Assert.Contains("Unknown filter operator 'Is'", ex.Message);
        Assert.Contains("isNot", ex.Message);
    }

    [Fact]
    public void DropsAGroupLeftWithNoFilters()
    {
        var groups = FilterSet.Create()
            .AndGroup(new Filter("status", FilterOperator.Is, ""))
            .Where("mediatype", FilterOperator.Is, "video")
            .ToArray();

        Assert.Single(groups);
        Assert.Equal("mediatype", groups[0].Filters[0].Field);
    }

    [Fact]
    public void SerialisesToTheJsonSapiExpects()
        => Assert.Equal("[{\"filters\":[{\"field\":\"status\",\"operator\":\"is\",\"value\":\"published\"}]}]", FilterSet.Create().Where("status", FilterOperator.Is, "published").ToString());

    [Fact]
    public void RoundTripsFromEnvelopeAndBareList()
    {
        const string groups = "[{\"filters\":[{\"field\":\"status\",\"operator\":\"is\",\"value\":\"published\"}]}]";
        Assert.Equal(groups, FilterSet.From("{\"type\":\"SearchRequest\",\"filterSet\":" + groups + "}").ToJson());
        Assert.Equal(groups, FilterSet.From(groups).ToJson());
        Assert.Equal(groups, FilterSet.From(JsonNode.Parse(groups)).ToJson());
    }

    [Fact]
    public void IngestedNumbersKeepTheirLiteral()
        => Assert.Equal("[{\"filters\":[{\"field\":\"views\",\"operator\":\"isGreaterThan\",\"value\":\"2.50\"}]}]", FilterSet.From("[{\"filters\":[{\"field\":\"views\",\"operator\":\"isGreaterThan\",\"value\":2.50}]}]").ToJson());

    [Fact]
    public void SerialisesThroughJsonSerializerAsTheWireFormat()
    {
        var filterSet = FilterSet.Create().Where("status", FilterOperator.Is, "published");
        Assert.Equal("{\"filterset\":[{\"filters\":[{\"field\":\"status\",\"operator\":\"is\",\"value\":\"published\"}]}]}", JsonSerializer.Serialize(new { filterset = filterSet }, SapiJson.Options));
        Assert.Equal(filterSet.ToJson(), JsonSerializer.Deserialize<FilterSet>(filterSet.ToJson(), SapiJson.Options)!.ToJson());
    }

    [Fact]
    public void EnvelopeWithNoGroupsIsEmpty() => Assert.True(FilterSet.From("{\"type\":\"SearchRequest\"}").IsEmpty());

    [Fact]
    public void IsImmutable()
    {
        var a = FilterSet.Create();
        var b = a.Where("status", FilterOperator.Is, "published");
        Assert.True(a.IsEmpty());
        Assert.False(b.IsEmpty());
    }

    [Fact]
    public void WireNamesRoundTrip()
    {
        foreach (var op in Enum.GetValues<FilterOperator>())
        {
            Assert.Equal(op, FilterOperators.Parse(op.ToWire()));
        }
        Assert.Equal(17, FilterOperators.WireNames.Count);
    }
}

public class MediaClipSearchTests
{
    [Fact]
    public async Task SendsTheFiltersetAsJsonForSapiToCompile()
    {
        var (sdk, h) = Create(Ok());

        await sdk.MediaClip.SearchAsync(FilterSet.Create().Where("status", FilterOperator.Is, "published"), 25, 50, "title asc", "holiday");

        var url = h.Calls[0].Url;
        Assert.Equal("/sapi/mediaclip", PathOf(url));
        Assert.Equal("holiday", Query(url, "q"));
        Assert.Equal("25", Query(url, "limit"));
        Assert.Equal("50", Query(url, "offset"));
        Assert.Equal("title asc", Query(url, "sort"));
        Assert.Equal("[{\"filters\":[{\"field\":\"status\",\"operator\":\"is\",\"value\":\"published\"}]}]", Query(url, "filterset"));
    }

    [Fact]
    public async Task SendsNoFilterParametersWhenNothingIsFiltered()
    {
        var (sdk, h) = Create(Ok());
        await sdk.MediaClip.SearchAsync(FilterSet.Create());
        Assert.Null(Query(h.Calls[0].Url, "filterset"));
        Assert.Null(Query(h.Calls[0].Url, "fq[0]"));
        Assert.Equal("*", Query(h.Calls[0].Url, "q"));
        Assert.Equal("15", Query(h.Calls[0].Url, "limit"));
    }

    [Fact]
    public async Task EncodesRawFilterQueriesAsIndexedParameters()
    {
        var (sdk, h) = Create(Ok());

        await sdk.MediaClip.SearchAsync(FilterSet.Create().Where("status", FilterOperator.Is, "published"), filterQueries: new[] { "statusSort:\"published\"", "mediatype:video" });

        // SAPI accepts fq[0]= (percent-encoded fq%5B0%5D on the wire); it ignores a nested
        // fq[][0]= and a plain fq=, in both cases silently, so this encoding is pinned.
        var url = h.Calls[0].Url;
        Assert.Contains("fq%5B0%5D=", url);
        Assert.Equal("statusSort:\"published\"", Query(url, "fq[0]"));
        Assert.Equal("mediatype:video", Query(url, "fq[1]"));
        Assert.Null(Query(url, "fq"));
        Assert.NotNull(Query(url, "filterset"));
    }
}
