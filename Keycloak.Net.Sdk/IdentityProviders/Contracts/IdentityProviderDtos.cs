using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.IdentityProviders.Contracts;

/// <summary>An external login (another OIDC/SAML server, Google, Microsoft, ...) brokered by the realm.</summary>
public sealed record IdentityProviderResponseDto
{
    [JsonPropertyName("alias")] public string Alias { get; set; } = string.Empty;
    [JsonPropertyName("internalId")] public string? InternalId { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    /// <summary>For example <c>oidc</c>, <c>saml</c>, <c>google</c>, <c>microsoft</c>.</summary>
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = string.Empty;
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("trustEmail")] public bool TrustEmail { get; set; }
    [JsonPropertyName("storeToken")] public bool StoreToken { get; set; }
    [JsonPropertyName("linkOnly")] public bool LinkOnly { get; set; }
    [JsonPropertyName("hideOnLogin")] public bool HideOnLogin { get; set; }
    [JsonPropertyName("firstBrokerLoginFlowAlias")] public string? FirstBrokerLoginFlowAlias { get; set; }
    [JsonPropertyName("postBrokerLoginFlowAlias")] public string? PostBrokerLoginFlowAlias { get; set; }
    /// <summary>Provider settings, for example <c>clientId</c>, <c>authorizationUrl</c>, <c>tokenUrl</c>. Secrets come back masked.</summary>
    [JsonPropertyName("config")] public Dictionary<string, string>? Config { get; set; }
}

public sealed record CreateIdentityProviderRequestDto
{
    /// <summary>Unique name used in URLs, for example <c>corporate-ad</c>.</summary>
    [JsonPropertyName("alias")] public required string Alias { get; init; }
    /// <summary>For example <c>oidc</c>, <c>keycloak-oidc</c>, <c>saml</c>, <c>google</c>.</summary>
    [JsonPropertyName("providerId")] public required string ProviderId { get; init; }

    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; init; }

    [JsonPropertyName("enabled")] public bool Enabled { get; init; } = true;
    /// <summary>Treat emails from this provider as verified.</summary>
    [JsonPropertyName("trustEmail")] public bool TrustEmail { get; init; }
    /// <summary>Store the provider's tokens for the user.</summary>
    [JsonPropertyName("storeToken")] public bool StoreToken { get; init; }
    /// <summary>Only allow linking to existing accounts, not logging in or registering.</summary>
    [JsonPropertyName("linkOnly")] public bool LinkOnly { get; init; }
    [JsonPropertyName("hideOnLogin")] public bool HideOnLogin { get; init; }

    [JsonPropertyName("firstBrokerLoginFlowAlias")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirstBrokerLoginFlowAlias { get; init; }

    [JsonPropertyName("config")] public Dictionary<string, string> Config { get; init; } = [];
}

/// <summary>Fields left null are not changed. <c>Config</c> entries are merged into the existing config.</summary>
public sealed record UpdateIdentityProviderRequestDto
{
    public string? DisplayName { get; init; }
    public bool? Enabled { get; init; }
    public bool? TrustEmail { get; init; }
    public bool? StoreToken { get; init; }
    public bool? LinkOnly { get; init; }
    public bool? HideOnLogin { get; init; }
    public string? FirstBrokerLoginFlowAlias { get; init; }
    public Dictionary<string, string>? Config { get; init; }
}

/// <summary>Maps data from the external provider to the Keycloak user (attributes, roles, ...).</summary>
public sealed record IdentityProviderMapperResponseDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("identityProviderAlias")] public string? IdentityProviderAlias { get; set; }
    [JsonPropertyName("identityProviderMapper")] public string IdentityProviderMapper { get; set; } = string.Empty;
    [JsonPropertyName("config")] public Dictionary<string, string>? Config { get; set; }
}

public sealed record CreateIdentityProviderMapperRequestDto
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    /// <summary>For example <c>oidc-user-attribute-idp-mapper</c>, <c>oidc-hardcoded-role-idp-mapper</c>, <c>hardcoded-attribute-idp-mapper</c>.</summary>
    [JsonPropertyName("identityProviderMapper")] public required string IdentityProviderMapper { get; init; }
    [JsonPropertyName("config")] public Dictionary<string, string> Config { get; init; } = [];
}

/// <summary>A link between a Keycloak user and their account at an identity provider.</summary>
public sealed record FederatedIdentityDto
{
    [JsonPropertyName("identityProvider")] public string IdentityProvider { get; set; } = string.Empty;
    /// <summary>The user's id at the identity provider.</summary>
    [JsonPropertyName("userId")] public string UserId { get; set; } = string.Empty;
    /// <summary>The user's name at the identity provider.</summary>
    [JsonPropertyName("userName")] public string UserName { get; set; } = string.Empty;
}
