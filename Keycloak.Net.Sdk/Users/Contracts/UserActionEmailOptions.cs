namespace Keycloak.Net.Sdk.Users.Contracts;

/// <summary>Optional settings for emails sent by Keycloak.</summary>
public sealed record UserActionEmailOptions
{
    public string? ClientId { get; init; }
    public string? RedirectUri { get; init; }

    /// <summary>Link lifetime in seconds. Null uses the realm default.</summary>
    public int? LifespanSeconds { get; init; }
}
