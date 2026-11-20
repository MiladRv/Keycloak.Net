using System.Globalization;

namespace Keycloak.Net.Sdk.AspNetCore;

/// <summary>
/// Which values of one data dimension (for example branch ids) a user may see.
/// Either unrestricted (administrators) or a fixed set of values, which may be empty.
/// </summary>
public sealed class DataScope
{
    private DataScope(bool isUnrestricted, IReadOnlyCollection<string> values)
    {
        IsUnrestricted = isUnrestricted;
        Values = values;
    }

    /// <summary>Every value is allowed.</summary>
    public static DataScope Unrestricted { get; } = new(true, []);

    /// <summary>No value is allowed.</summary>
    public static DataScope None { get; } = new(false, []);

    /// <summary>Only these values are allowed.</summary>
    public static DataScope Of(IEnumerable<string> values) =>
        new(false, values.Where(v => v is not null).Distinct(StringComparer.Ordinal).ToList());

    public bool IsUnrestricted { get; }

    /// <summary>The allowed values. Empty when <see cref="IsUnrestricted"/> is true.</summary>
    public IReadOnlyCollection<string> Values { get; }

    public bool Allows(string value) => IsUnrestricted || Values.Contains(value, StringComparer.Ordinal);

    /// <summary>
    /// The allowed values parsed as <typeparamref name="T"/> with the invariant culture.
    /// Values that do not parse are left out, so a malformed value grants nothing.
    /// </summary>
    public IReadOnlyList<T> ValuesAs<T>() where T : IParsable<T>
    {
        var result = new List<T>(Values.Count);
        foreach (var value in Values)
            if (T.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
                result.Add(parsed);
        return result;
    }

    public override string ToString() => IsUnrestricted ? "Unrestricted" : $"[{string.Join(", ", Values)}]";
}
