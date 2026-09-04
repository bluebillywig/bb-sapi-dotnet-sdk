namespace BlueBillywig.Sapi.Search;

/// <summary>Operators SAPI understands in a filterset.</summary>
public enum FilterOperator
{
    /// <summary><c>is</c></summary>
    Is,
    /// <summary><c>isNot</c></summary>
    IsNot,
    /// <summary><c>isAnyOf</c></summary>
    IsAnyOf,
    /// <summary><c>isNotAnyOf</c></summary>
    IsNotAnyOf,
    /// <summary><c>isEmpty</c> (presence test; needs no value)</summary>
    IsEmpty,
    /// <summary><c>isNotEmpty</c> (presence test; needs no value)</summary>
    IsNotEmpty,
    /// <summary><c>contains</c></summary>
    Contains,
    /// <summary><c>containsAnyOf</c></summary>
    ContainsAnyOf,
    /// <summary><c>containsAllOf</c></summary>
    ContainsAllOf,
    /// <summary><c>doesNotContain</c></summary>
    DoesNotContain,
    /// <summary><c>doesNotContainAnyOf</c></summary>
    DoesNotContainAnyOf,
    /// <summary><c>isBefore</c></summary>
    IsBefore,
    /// <summary><c>isAfter</c></summary>
    IsAfter,
    /// <summary><c>isSmallerThan</c></summary>
    IsSmallerThan,
    /// <summary><c>isGreaterThan</c></summary>
    IsGreaterThan,
    /// <summary><c>isInTheLast</c></summary>
    IsInTheLast,
    /// <summary><c>isNotInTheLast</c></summary>
    IsNotInTheLast,
}

/// <summary>Wire-name conversions for <see cref="FilterOperator"/>.</summary>
public static class FilterOperators
{
    private static readonly Dictionary<FilterOperator, string> ToWireMap = new()
    {
        [FilterOperator.Is] = "is",
        [FilterOperator.IsNot] = "isNot",
        [FilterOperator.IsAnyOf] = "isAnyOf",
        [FilterOperator.IsNotAnyOf] = "isNotAnyOf",
        [FilterOperator.IsEmpty] = "isEmpty",
        [FilterOperator.IsNotEmpty] = "isNotEmpty",
        [FilterOperator.Contains] = "contains",
        [FilterOperator.ContainsAnyOf] = "containsAnyOf",
        [FilterOperator.ContainsAllOf] = "containsAllOf",
        [FilterOperator.DoesNotContain] = "doesNotContain",
        [FilterOperator.DoesNotContainAnyOf] = "doesNotContainAnyOf",
        [FilterOperator.IsBefore] = "isBefore",
        [FilterOperator.IsAfter] = "isAfter",
        [FilterOperator.IsSmallerThan] = "isSmallerThan",
        [FilterOperator.IsGreaterThan] = "isGreaterThan",
        [FilterOperator.IsInTheLast] = "isInTheLast",
        [FilterOperator.IsNotInTheLast] = "isNotInTheLast",
    };

    private static readonly Dictionary<string, FilterOperator> FromWireMap =
        ToWireMap.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    /// <summary>All wire names, in declaration order.</summary>
    public static IReadOnlyCollection<string> WireNames => ToWireMap.Values;

    /// <summary>The wire name of an operator, e.g. <c>isNot</c>.</summary>
    public static string ToWire(this FilterOperator op) => ToWireMap[op];

    /// <summary>Parses a wire name.</summary>
    /// <exception cref="ArgumentException">for an unknown operator.</exception>
    public static FilterOperator Parse(string wireName)
    {
        if (wireName is not null && FromWireMap.TryGetValue(wireName, out var op)) return op;
        throw new ArgumentException($"Unknown filter operator '{wireName}'. Known operators: {string.Join(", ", WireNames)}.", nameof(wireName));
    }

    /// <summary>Tries to parse a wire name.</summary>
    public static bool TryParse(string? wireName, out FilterOperator op)
    {
        if (wireName is not null && FromWireMap.TryGetValue(wireName, out op)) return true;
        op = default;
        return false;
    }

    /// <summary>Whether the operator tests presence, so it is meaningful without a value.</summary>
    public static bool IsValueless(this FilterOperator op) => op is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty;
}
