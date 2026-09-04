namespace BlueBillywig.Sapi.Search;

/// <summary>
/// One condition in a filterset. <see cref="Value"/> may be a scalar (string, number, bool) or a
/// sequence of scalars; numbers and booleans are normalised to strings on the wire by
/// <see cref="FilterSet.ToArray"/>: the backend's compiler mangles a JSON <c>true</c> into
/// <c>"1"</c> (which matches nothing, silently) and its empty-value guard drops <c>false</c>
/// outright, while numbers work but only ever appear as strings in what OVP6 sends.
/// </summary>
public sealed class Filter
{
    /// <summary>The field to filter on.</summary>
    public string Field { get; }

    /// <summary>The operator.</summary>
    public FilterOperator Operator { get; }

    /// <summary>The value: a scalar, a sequence of scalars, or <c>null</c>.</summary>
    public object? Value { get; }

    /// <summary>Constrain to an entity type: mediaclip, project, search.</summary>
    public string? Type { get; }

    /// <summary>Creates a filter.</summary>
    public Filter(string field, FilterOperator @operator, object? value = null, string? type = null)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        Operator = @operator;
        Value = value;
        Type = type;
    }

    /// <summary>The value as a single string, when it is a scalar; otherwise <c>null</c>.</summary>
    public string? ScalarValue => Value is string s ? s : null;

    /// <summary>The value as a list of strings, when it is a list; otherwise <c>null</c>.</summary>
    public IReadOnlyList<string>? ListValue => Value as IReadOnlyList<string>;
}

/// <summary>A group of filters. Groups are AND-ed with each other, filters within a group are OR-ed.</summary>
public sealed class FilterGroup
{
    /// <summary>The filters in this group.</summary>
    public IReadOnlyList<Filter> Filters { get; }

    /// <summary>Creates a group.</summary>
    public FilterGroup(IEnumerable<Filter> filters)
    {
        Filters = (filters ?? throw new ArgumentNullException(nameof(filters))).ToList();
    }

    /// <summary>Creates a group.</summary>
    public FilterGroup(params Filter[] filters) : this((IEnumerable<Filter>)filters) { }
}
