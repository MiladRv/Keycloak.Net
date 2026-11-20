using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Authorization.Contracts;

/// <summary>Values for <c>DecisionStrategy</c> on resource servers and permissions.</summary>
public static class DecisionStrategies
{
    /// <summary>Every policy must grant.</summary>
    public const string Unanimous = "UNANIMOUS";
    /// <summary>At least one policy must grant.</summary>
    public const string Affirmative = "AFFIRMATIVE";
    /// <summary>More policies must grant than deny.</summary>
    public const string Consensus = "CONSENSUS";
}

/// <summary>Values for a policy's <c>Logic</c>.</summary>
public static class PolicyLogic
{
    public const string Positive = "POSITIVE";
    /// <summary>Inverts the policy's result.</summary>
    public const string Negative = "NEGATIVE";
}

/// <summary>Values for a resource server's <c>PolicyEnforcementMode</c>.</summary>
public static class PolicyEnforcementModes
{
    /// <summary>Requests are denied when no policy is associated with a resource.</summary>
    public const string Enforcing = "ENFORCING";
    /// <summary>Requests are allowed when no policy is associated with a resource.</summary>
    public const string Permissive = "PERMISSIVE";
    public const string Disabled = "DISABLED";
}

// ── Resource server ───────────────────────────────────────────────────────────

public sealed record ResourceServerResponseDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("clientId")] public string ClientId { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("policyEnforcementMode")] public string? PolicyEnforcementMode { get; set; }
    [JsonPropertyName("decisionStrategy")] public string? DecisionStrategy { get; set; }
    [JsonPropertyName("allowRemoteResourceManagement")] public bool AllowRemoteResourceManagement { get; set; }
}

/// <summary>Fields left null are not changed.</summary>
public sealed record UpdateResourceServerRequestDto
{
    public string? PolicyEnforcementMode { get; init; }
    public string? DecisionStrategy { get; init; }
    public bool? AllowRemoteResourceManagement { get; init; }
}

// ── Scopes ────────────────────────────────────────────────────────────────────

public sealed record AuthorizationScopeResponseDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("iconUri")] public string? IconUri { get; set; }
}

public sealed record CreateAuthorizationScopeRequestDto
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; init; }

    [JsonPropertyName("iconUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IconUri { get; init; }
}

// ── Resources ─────────────────────────────────────────────────────────────────

public sealed record ResourceOwnerDto
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public sealed record ResourceResponseDto
{
    [JsonPropertyName("_id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("uris")] public List<string>? Uris { get; set; }
    [JsonPropertyName("scopes")] public List<AuthorizationScopeResponseDto>? Scopes { get; set; }
    [JsonPropertyName("ownerManagedAccess")] public bool OwnerManagedAccess { get; set; }
    [JsonPropertyName("owner")] public ResourceOwnerDto? Owner { get; set; }
    [JsonPropertyName("attributes")] public Dictionary<string, List<string>>? Attributes { get; set; }
}

/// <summary>A resource, for example one ERP entity type (<c>invoice</c>) or one record.</summary>
public sealed record CreateResourceRequestDto
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; init; }

    /// <summary>Groups resources, for example <c>urn:erp:resources:invoice</c>.</summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    [JsonPropertyName("uris")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Uris { get; init; }

    /// <summary>Scope names (created on the resource server if they do not exist).</summary>
    [JsonIgnore]
    public List<string>? Scopes { get; init; }

    [JsonPropertyName("scopes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<NamedReference>? ScopeReferences => Scopes?.Select(s => new NamedReference(s)).ToList();

    [JsonPropertyName("ownerManagedAccess")]
    public bool OwnerManagedAccess { get; init; }

    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<string>>? Attributes { get; init; }
}

/// <summary>Fields left null are not changed. <c>Scopes</c> and <c>Attributes</c> replace the existing ones when set.</summary>
public sealed record UpdateResourceRequestDto
{
    public string? Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Type { get; init; }
    public List<string>? Uris { get; init; }
    public List<string>? Scopes { get; init; }
    public Dictionary<string, List<string>>? Attributes { get; init; }
}

public sealed record NamedReference([property: JsonPropertyName("name")] string Name);

public sealed record GetResourcesQueryDto
{
    public string? Name { get; init; }
    /// <summary>Match <see cref="Name"/> exactly instead of as a substring.</summary>
    public bool? ExactName { get; init; }
    public string? Type { get; init; }
    public string? Uri { get; init; }
    public string? Scope { get; init; }
    public int? First { get; init; }
    public int? Max { get; init; }
}

// ── Policies and permissions ──────────────────────────────────────────────────

/// <summary>A policy or permission as Keycloak lists it. Type-specific settings are in <see cref="Config"/>.</summary>
public sealed record PolicyResponseDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    /// <summary><c>role</c>, <c>group</c>, <c>user</c>, ... for policies; <c>resource</c> or <c>scope</c> for permissions.</summary>
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("logic")] public string? Logic { get; set; }
    [JsonPropertyName("decisionStrategy")] public string? DecisionStrategy { get; set; }
    [JsonPropertyName("config")] public Dictionary<string, string>? Config { get; set; }
}

public sealed record GetPoliciesQueryDto
{
    public string? Name { get; init; }
    /// <summary>Policy type, for example <c>role</c>.</summary>
    public string? Type { get; init; }
    public int? First { get; init; }
    public int? Max { get; init; }
}

public sealed record GetPermissionsQueryDto
{
    public string? Name { get; init; }
    /// <summary>Only permissions for this resource id.</summary>
    public string? Resource { get; init; }
    /// <summary>Only permissions for this scope id.</summary>
    public string? Scope { get; init; }
    public int? First { get; init; }
    public int? Max { get; init; }
}

public abstract record CreatePolicyRequestDtoBase
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("logic")] public string Logic { get; init; } = PolicyLogic.Positive;
    [JsonPropertyName("decisionStrategy")] public string DecisionStrategy { get; init; } = DecisionStrategies.Unanimous;
}

public sealed record PolicyRole(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("required")] bool Required = false);

/// <summary>Grants users holding the roles. Role ids are role ids (realm or client).</summary>
public sealed record CreateRolePolicyRequestDto : CreatePolicyRequestDtoBase
{
    [JsonPropertyName("roles")] public required List<PolicyRole> Roles { get; init; }

    /// <summary>
    /// Check the roles the user has now instead of the roles in the token being evaluated.
    /// Null leaves Keycloak's default (off).
    /// </summary>
    [JsonPropertyName("fetchRoles")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FetchRoles { get; init; }
}

public sealed record PolicyGroup(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("extendChildren")] bool ExtendChildren = false);

/// <summary>Grants members of the groups; with <c>ExtendChildren</c> also members of their child groups.</summary>
public sealed record CreateGroupPolicyRequestDto : CreatePolicyRequestDtoBase
{
    [JsonPropertyName("groups")] public required List<PolicyGroup> Groups { get; init; }
}

/// <summary>Grants specific users by id.</summary>
public sealed record CreateUserPolicyRequestDto : CreatePolicyRequestDtoBase
{
    [JsonPropertyName("users")] public required List<string> Users { get; init; }
}

/// <summary>Grants access to whole resources when the policies grant.</summary>
public sealed record CreateResourcePermissionRequestDto : CreatePolicyRequestDtoBase
{
    /// <summary>Resource ids.</summary>
    [JsonPropertyName("resources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Resources { get; init; }

    /// <summary>Apply to every resource of this type instead of listing resources.</summary>
    [JsonPropertyName("resourceType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResourceType { get; init; }

    /// <summary>Policy ids.</summary>
    [JsonPropertyName("policies")] public required List<string> Policies { get; init; }
}

/// <summary>Grants specific scopes (for example <c>approve</c>) when the policies grant.</summary>
public sealed record CreateScopePermissionRequestDto : CreatePolicyRequestDtoBase
{
    /// <summary>Optional resource ids; without them the permission applies to the scopes on any resource.</summary>
    [JsonPropertyName("resources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Resources { get; init; }

    /// <summary>Scope ids.</summary>
    [JsonPropertyName("scopes")] public required List<string> Scopes { get; init; }

    /// <summary>Policy ids.</summary>
    [JsonPropertyName("policies")] public required List<string> Policies { get; init; }
}

// ── Evaluation ────────────────────────────────────────────────────────────────

/// <summary>A permission to ask for: a resource (id or name) and optionally scopes.</summary>
public sealed record PermissionRequest(string? Resource, params string[] Scopes)
{
    internal string ToFormValue()
    {
        var scopes = Scopes.Length > 0 ? "#" + string.Join(',', Scopes) : string.Empty;
        return (Resource ?? string.Empty) + scopes;
    }
}

public sealed record PermissionDecisionDto
{
    public bool Granted { get; init; }
}

public sealed record GrantedPermissionDto
{
    [JsonPropertyName("rsid")] public string? ResourceId { get; set; }
    [JsonPropertyName("rsname")] public string? ResourceName { get; set; }
    [JsonPropertyName("scopes")] public List<string>? Scopes { get; set; }
}

/// <summary>The result of evaluating a user's permissions with the admin API (no user token needed).</summary>
public sealed record PolicyEvaluationResponseDto
{
    /// <summary><c>PERMIT</c> or <c>DENY</c>.</summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("results")] public List<PolicyEvaluationResultDto> Results { get; set; } = [];
}

public sealed record PolicyEvaluationResultDto
{
    [JsonPropertyName("resource")] public ResourceResponseDto? Resource { get; set; }
    /// <summary><c>PERMIT</c> or <c>DENY</c>.</summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("allowedScopes")] public List<AuthorizationScopeResponseDto>? AllowedScopes { get; set; }
    [JsonPropertyName("policies")] public List<JsonElement>? Policies { get; set; }
}
