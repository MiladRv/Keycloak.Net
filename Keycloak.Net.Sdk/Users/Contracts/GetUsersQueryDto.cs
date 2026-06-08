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
}
