using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Search;

/// <summary>
/// The filterset: the structure the OVP builds in its filter UI, and the shape SAPI's
/// <c>filterset</c> parameter takes.
/// <para>
/// Deliberately NOT compiled to a Solr query here. SAPI compiles filtersets itself, using the
/// same SearchRequestHelper that serves the OVP, so compiling client-side would be a second
/// implementation of semantics the server owns — free to drift, with a failure mode that is
/// invisible: a filter SAPI cannot read is ignored, and the response is HTTP 200 with neither
/// <c>numfound</c> nor <c>items</c>, which reads exactly like an empty library.
/// </para>
/// <para>
/// Mirrors <c>app/services/filter-set.types.ts</c> in OVP6, so a filterset moves between the
/// UI, the API and any SDK unchanged.
/// </para>
/// <para>Server-side quirks a caller inherits (the compiler is formatengine's):</para>
/// <list type="bullet">
/// <item>A filter whose value is the string <c>'0'</c> is dropped by the backend's empty-value
/// guard, so "views is 0" cannot be expressed as a filterset.</item>
/// <item>In values, <c>+</c> becomes a space and <c>"</c> is stripped before compilation.</item>
/// <item>An unknown FIELD is not an error: it queries a non-existent index field and returns
/// numfound=0 — a typo'd field name looks like an empty library.</item>
/// </list>
/// <example>
/// <code>
/// var filterSet = FilterSet.Create()
///     .Where("status", FilterOperator.Is, "published")
///     .Where("title", FilterOperator.Contains, "koert");
///
/// await sdk.MediaClip.SearchAsync(filterSet);
/// </code>
/// </example>
/// </summary>
[JsonConverter(typeof(FilterSetJsonConverter))]
public sealed class FilterSet
{
    private readonly IReadOnlyList<FilterGroup> _groups;

    private FilterSet(IReadOnlyList<FilterGroup> groups)
    {
        _groups = groups;
    }

    /// <summary>Creates an empty filterset.</summary>
    public static FilterSet Create() => new(Array.Empty<FilterGroup>());

    /// <summary>Creates a filterset from groups.</summary>
    public static FilterSet From(IEnumerable<FilterGroup> groups) => new((groups ?? Array.Empty<FilterGroup>()).ToList());

    /// <summary>
    /// Ingests a filterset from JSON: either a bare list of groups or the <c>SearchRequest</c>
    /// envelope OVP6 sends (<c>{"type":"SearchRequest","filterSet":[...]}</c>). Malformed groups
    /// (no <c>filters</c> array) and filters without a string <c>field</c> are skipped as junk
    /// rather than crashing; an unknown operator throws a descriptive
    /// <see cref="ArgumentException"/>.
    /// </summary>
    public static FilterSet From(string json)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));
        return From(JsonNode.Parse(json));
    }

    /// <summary>Ingests a filterset from a parsed JSON node. See <see cref="From(string)"/>.</summary>
    public static FilterSet From(JsonNode? node)
    {
        JsonArray? groupsNode = node switch
        {
            JsonArray array => array,
            JsonObject envelope => envelope["filterSet"] as JsonArray,
            _ => null,
        };

        var groups = new List<FilterGroup>();
        if (groupsNode is null) return new FilterSet(groups);

        foreach (var groupNode in groupsNode)
        {
            // Ingested external data; a group without a filters array is junk, not a crash.
            if (groupNode is not JsonObject groupObject || groupObject["filters"] is not JsonArray filtersNode) continue;

            var filters = new List<Filter>();
            foreach (var filterNode in filtersNode)
            {
                if (filterNode is not JsonObject filterObject) continue;
                if (filterObject["field"] is not JsonValue fieldValue || fieldValue.GetValueKind() != JsonValueKind.String) continue;

                var operatorName = filterObject["operator"] is JsonValue opValue && opValue.GetValueKind() == JsonValueKind.String
                    ? opValue.GetValue<string>()
                    : null;
                var op = FilterOperators.Parse(operatorName!);

                var type = filterObject["type"] is JsonValue typeValue && typeValue.GetValueKind() == JsonValueKind.String
                    ? typeValue.GetValue<string>()
                    : null;

                filters.Add(new Filter(fieldValue.GetValue<string>(), op, JsonToValue(filterObject["value"]), type));
            }
            groups.Add(new FilterGroup(filters));
        }

        return new FilterSet(groups);
    }

    private static object? JsonToValue(JsonNode? node) => node switch
    {
        JsonArray array => array.Select(JsonToScalar).ToList(),
        _ => JsonToScalar(node),
    };

    private static object? JsonToScalar(JsonNode? node)
    {
        if (node is not JsonValue value) return null;
        return value.GetValueKind() switch
        {
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.Number => new JsonNumber(value.ToJsonString()),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    /// <summary>A number carried as its JSON literal text, so it serialises exactly as ingested.</summary>
    private sealed record JsonNumber(string Literal)
    {
        public override string ToString() => Literal;
    }

    /// <summary>Add a condition as its own group, so it is AND-ed with the rest.</summary>
    /// <param name="field">The field.</param>
    /// <param name="operator">The operator.</param>
    /// <param name="value">A string, number, bool, or a sequence of those.</param>
    /// <param name="type">Optional entity type constraint.</param>
    public FilterSet Where(string field, FilterOperator @operator, object? value = null, string? type = null)
        => AndGroup(new Filter(field, @operator, value, type));

    /// <summary>Add several conditions as one group, so they are OR-ed with each other.</summary>
    public FilterSet AndGroup(params Filter[] filters)
    {
        var groups = new List<FilterGroup>(_groups) { new FilterGroup(filters) };
        return new FilterSet(groups);
    }

    /// <summary>
    /// The wire format: what SAPI's <c>filterset</c> parameter expects. Filters with nothing to
    /// match on are dropped, values are normalised to strings, presence operators carry the
    /// <c>*</c> placeholder the backend requires, and groups left empty are removed.
    /// </summary>
    public IReadOnlyList<FilterGroup> ToArray()
    {
        var result = new List<FilterGroup>();
        foreach (var group in _groups)
        {
            var filters = group.Filters.Where(HasValue).Select(Strip).ToList();
            if (filters.Count > 0) result.Add(new FilterGroup(filters));
        }
        return result;
    }

    /// <summary>The wire format as a JSON array node.</summary>
    public JsonArray ToJsonNode()
    {
        var array = new JsonArray();
        foreach (var group in ToArray())
        {
            var filters = new JsonArray();
            foreach (var filter in group.Filters)
            {
                var obj = new JsonObject
                {
                    ["field"] = filter.Field,
                    ["operator"] = filter.Operator.ToWire(),
                };
                if (filter.Value is string scalar)
                {
                    obj["value"] = scalar;
                }
                else if (filter.Value is IReadOnlyList<string> list)
                {
                    obj["value"] = new JsonArray(list.Select(v => (JsonNode?)JsonValue.Create(v)).ToArray());
                }
                if (!string.IsNullOrEmpty(filter.Type))
                {
                    obj["type"] = filter.Type;
                }
                filters.Add(obj);
            }
            array.Add(new JsonObject { ["filters"] = filters });
        }
        return array;
    }

    /// <summary>The wire format as compact JSON.</summary>
    public string ToJson() => ToJsonNode().ToJsonString(SapiJson.Options);

    /// <inheritdoc cref="ToJson"/>
    public override string ToString() => ToJson();

    /// <summary>Whether nothing survives normalisation.</summary>
    public bool IsEmpty() => ToArray().Count == 0;

    private static bool HasValue(Filter filter)
    {
        if (filter.Operator.IsValueless()) return true;
        return EnumerateScalars(filter.Value).Any(v => v switch
        {
            bool => true,
            JsonNumber => true,
            string s => !string.IsNullOrWhiteSpace(s),
            null => false,
            _ => IsNumber(v),
        });
    }

    /// <summary>Normalise to the wire shape: what the OVP sends and the backend can read.</summary>
    private static Filter Strip(Filter filter)
    {
        object? value;
        if (filter.Operator.IsValueless())
        {
            // The backend's compiler skips ANY filter whose value is empty — presence tests
            // included — so isEmpty/isNotEmpty must carry a placeholder or they silently never
            // fire (verified live: a bare isEmpty returned the full unfiltered publication). '*'
            // is what OVP6 sends ("backend needs a value to work"), and it overrides whatever
            // the caller supplied.
            value = "*";
        }
        else if (filter.Value is null)
        {
            value = null;
        }
        else if (IsScalar(filter.Value))
        {
            value = NormalizeScalar(filter.Value);
        }
        else
        {
            value = EnumerateScalars(filter.Value).Where(IsScalar).Select(NormalizeScalar!).ToList();
        }
        return new Filter(filter.Field, filter.Operator, value, string.IsNullOrEmpty(filter.Type) ? null : filter.Type);
    }

    private static IEnumerable<object?> EnumerateScalars(object? value)
    {
        if (value is null) return new object?[] { null };
        if (value is string) return new[] { value };
        if (value is IEnumerable enumerable) return enumerable.Cast<object?>();
        return new[] { value };
    }

    private static bool IsScalar(object? value) => value is string or bool or JsonNumber || IsNumber(value);

    private static bool IsNumber(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static string NormalizeScalar(object? value) => value switch
    {
        bool b => b ? "true" : "false",
        string s => s,
        JsonNumber n => n.Literal,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value?.ToString() ?? string.Empty,
    };
}

/// <summary>Serialises a <see cref="FilterSet"/> as its wire format when embedded in a larger body.</summary>
public sealed class FilterSetJsonConverter : JsonConverter<FilterSet>
{
    /// <inheritdoc />
    public override FilterSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => FilterSet.From(JsonNode.Parse(ref reader));

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, FilterSet value, JsonSerializerOptions options)
        => value.ToJsonNode().WriteTo(writer, options);
}
