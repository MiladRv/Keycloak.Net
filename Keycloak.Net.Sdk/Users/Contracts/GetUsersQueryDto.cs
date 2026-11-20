namespace Keycloak.Net.Sdk.Users.Contracts;

public sealed record GetUsersQueryDto
{
    /// <summary>Pagination offset (0-based).</summary>
    public int? First { get; init; }

    /// <summary>Maximum number of results to return.</summary>
    public int? Max { get; init; }

    /// <summary>Free-text search across username, email, first name, and last name.</summary>
    public string? Search { get; init; }

    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool? Enabled { get; init; }
    public bool? EmailVerified { get; init; }

    /// <summary>
    /// When true, <see cref="Username"/>, <see cref="Email"/>, <see cref="FirstName"/> and
    /// <see cref="LastName"/> must match exactly instead of as substrings.
    /// Not applied by <c>GetUsersCountAsync</c> (Keycloak's count endpoint does not support it).
    /// </summary>
    public bool? Exact { get; init; }

    /// <summary>
    /// Only return users that have every one of these attributes with exactly this value
    /// (sent as Keycloak's <c>q</c> parameter), for example <c>{ ["branch_id"] = "12" }</c>.
    /// Keys and values must not contain double quotes.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
