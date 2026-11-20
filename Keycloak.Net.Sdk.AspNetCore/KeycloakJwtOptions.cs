namespace Keycloak.Net.Sdk.AspNetCore;

/// <summary>
/// Settings for validating Keycloak access tokens in an ASP.NET Core API. The property names match
/// the SDK's <c>KeycloakConfiguration</c>, so the same configuration section can be bound to both.
/// </summary>
public sealed class KeycloakJwtOptions
{
    /// <summary>Keycloak base address, for example <c>https://sso.example.com/</c>.</summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>The realm that issues the tokens.</summary>
    public string RealmName { get; set; } = string.Empty;

    /// <summary>
    /// The API's client id. Its client roles are added as role claims, and it is the expected
    /// audience when <see cref="Audience"/> is not set.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The audience tokens must contain. Defaults to <see cref="ClientId"/>. Keycloak only puts a
    /// client in <c>aud</c> when an audience mapper (or a client role of that client) adds it.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>Reject tokens whose <c>aud</c> does not contain the audience. Default: true.</summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>Require HTTPS for the metadata endpoint. Turn off only for local development. Default: true.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Add the user's realm roles (<c>realm_access.roles</c>) as role claims. Default: true.</summary>
    public bool MapRealmRoles { get; set; } = true;

    /// <summary>Add the roles of <see cref="ClientId"/> and <see cref="RoleClientIds"/> as role claims. Default: true.</summary>
    public bool MapClientRoles { get; set; } = true;

    /// <summary>Other clients whose roles (<c>resource_access.{client}.roles</c>) are added as role claims.</summary>
    public List<string> RoleClientIds { get; set; } = [];

    internal string Authority => $"{ServerUrl.TrimEnd('/')}/realms/{Uri.EscapeDataString(RealmName)}";

    internal string? EffectiveAudience => string.IsNullOrWhiteSpace(Audience) ? NullIfEmpty(ClientId) : Audience;

    internal IEnumerable<string> ClientsWithMappedRoles =>
        new[] { ClientId }.Concat(RoleClientIds).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal);

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
            throw new InvalidOperationException($"{nameof(KeycloakJwtOptions)}.{nameof(ServerUrl)} is required.");
        if (!Uri.TryCreate(ServerUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException($"{nameof(KeycloakJwtOptions)}.{nameof(ServerUrl)} must be an absolute URL.");
        if (string.IsNullOrWhiteSpace(RealmName))
            throw new InvalidOperationException($"{nameof(KeycloakJwtOptions)}.{nameof(RealmName)} is required.");
        if (ValidateAudience && EffectiveAudience is null)
            throw new InvalidOperationException(
                $"Set {nameof(KeycloakJwtOptions)}.{nameof(Audience)} or {nameof(ClientId)}, or turn off {nameof(ValidateAudience)}.");
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
