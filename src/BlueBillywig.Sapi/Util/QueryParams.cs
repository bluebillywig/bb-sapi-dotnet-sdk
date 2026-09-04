using System.Globalization;
using System.Text;

namespace BlueBillywig.Sapi.Util;

/// <summary>Query-string helpers.</summary>
public static class QueryParams
{
    /// <summary>
    /// Builds a query parameter map, omitting entries whose value is <c>null</c>. Booleans become
    /// <c>"true"</c>/<c>"false"</c>; numbers are formatted with the invariant culture; everything
    /// else uses <see cref="object.ToString"/>.
    /// </summary>
    public static Dictionary<string, string> Build(params (string Key, object? Value)[] entries)
    {
        var query = new Dictionary<string, string>();
        foreach (var (key, value) in entries)
        {
            var formatted = Format(value);
            if (formatted is not null)
            {
                query[key] = formatted;
            }
        }
        return query;
    }

    /// <summary>Formats a single query value, or returns <c>null</c> when the value is <c>null</c>.</summary>
    public static string? Format(object? value) => value switch
    {
        null => null,
        bool b => b ? "true" : "false",
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    /// <summary>
    /// Appends (or replaces, by key) query parameters on a URL. Existing parameters are kept
    /// byte-for-byte; only a same-named parameter is replaced. Mirrors <c>URLSearchParams.set</c>.
    /// </summary>
    public static string Append(string url, IEnumerable<KeyValuePair<string, string>> query)
    {
        var hashIndex = url.IndexOf('#');
        var fragment = hashIndex >= 0 ? url[hashIndex..] : string.Empty;
        var withoutFragment = hashIndex >= 0 ? url[..hashIndex] : url;

        var questionIndex = withoutFragment.IndexOf('?');
        var baseUrl = questionIndex >= 0 ? withoutFragment[..questionIndex] : withoutFragment;
        var existing = questionIndex >= 0 ? withoutFragment[(questionIndex + 1)..] : string.Empty;

        var pairs = new List<(string DecodedKey, string Raw)>();
        foreach (var raw in existing.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = raw.IndexOf('=');
            var rawKey = eq >= 0 ? raw[..eq] : raw;
            pairs.Add((Decode(rawKey), raw));
        }

        foreach (var (key, value) in query)
        {
            var encoded = Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
            var index = pairs.FindIndex(p => p.DecodedKey == key);
            if (index >= 0)
            {
                pairs[index] = (key, encoded);
                pairs.RemoveAll(p => p.DecodedKey == key && p.Raw != encoded);
            }
            else
            {
                pairs.Add((key, encoded));
            }
        }

        if (pairs.Count == 0)
        {
            return baseUrl + fragment;
        }

        var sb = new StringBuilder(baseUrl).Append('?');
        for (var i = 0; i < pairs.Count; i++)
        {
            if (i > 0) sb.Append('&');
            sb.Append(pairs[i].Raw);
        }
        return sb.Append(fragment).ToString();
    }

    /// <summary>Returns the first value of a query parameter in a URL, or <c>null</c>.</summary>
    public static string? Get(string url, string name)
    {
        var hashIndex = url.IndexOf('#');
        var withoutFragment = hashIndex >= 0 ? url[..hashIndex] : url;
        var questionIndex = withoutFragment.IndexOf('?');
        if (questionIndex < 0) return null;

        foreach (var raw in withoutFragment[(questionIndex + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = raw.IndexOf('=');
            var key = Decode(eq >= 0 ? raw[..eq] : raw);
            if (key == name)
            {
                return eq >= 0 ? Decode(raw[(eq + 1)..]) : string.Empty;
            }
        }
        return null;
    }

    /// <summary>Decodes a query component, treating <c>+</c> as a space like <c>URLSearchParams</c>.</summary>
    public static string Decode(string component) => Uri.UnescapeDataString(component.Replace('+', ' '));
}
