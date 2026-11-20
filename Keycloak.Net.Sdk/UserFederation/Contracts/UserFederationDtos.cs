using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.UserFederation.Contracts;

/// <summary>A user storage provider (LDAP / Active Directory, Kerberos, or a custom provider).</summary>
public sealed record UserStorageProviderResponseDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    /// <summary>For example <c>ldap</c> or <c>kerberos</c>.</summary>
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = string.Empty;
    [JsonPropertyName("providerType")] public string? ProviderType { get; set; }
    [JsonPropertyName("parentId")] public string? ParentId { get; set; }
    /// <summary>Provider settings; every value is a list, as Keycloak stores them. Secrets come back masked.</summary>
    [JsonPropertyName("config")] public Dictionary<string, List<string>>? Config { get; set; }
}

public sealed record CreateUserStorageProviderRequestDto
{
    /// <summary>Display name, for example <c>corporate-ad</c>.</summary>
    public required string Name { get; init; }
    /// <summary>For example <c>ldap</c> or <c>kerberos</c>.</summary>
    public required string ProviderId { get; init; }
    /// <summary>
    /// Provider settings. For LDAP for example <c>connectionUrl</c>, <c>bindDn</c>, <c>bindCredential</c>,
    /// <c>usersDn</c>, <c>vendor</c> (<c>ad</c>, <c>other</c>, ...), <c>editMode</c> (<c>READ_ONLY</c>, <c>WRITABLE</c>, <c>UNSYNCED</c>),
    /// <c>usernameLDAPAttribute</c>, <c>rdnLDAPAttribute</c>, <c>uuidLDAPAttribute</c>, <c>userObjectClasses</c>.
    /// </summary>
    public Dictionary<string, List<string>> Config { get; init; } = [];
}

/// <summary>Fields left null are not changed. <c>Config</c> entries are merged into the existing config.</summary>
public sealed record UpdateUserStorageProviderRequestDto
{
    public string? Name { get; init; }
    public Dictionary<string, List<string>>? Config { get; init; }
}

/// <summary>The outcome of synchronizing users from a user storage provider.</summary>
public sealed record SynchronizationResultDto
{
    [JsonPropertyName("ignored")] public bool Ignored { get; set; }
    [JsonPropertyName("added")] public int Added { get; set; }
    [JsonPropertyName("updated")] public int Updated { get; set; }
    [JsonPropertyName("removed")] public int Removed { get; set; }
    [JsonPropertyName("failed")] public int Failed { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
}
