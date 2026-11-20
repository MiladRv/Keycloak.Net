using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Authorization.Contracts;

/// <summary>
/// Keycloak Authorization Services: fine-grained permissions on a resource-server client.
/// <c>clientUuid</c> is the resource server's internal client id (<c>ClientResponseDto.Id</c>).
/// Evaluation methods take the user's own access token and the resource server's <c>clientId</c>.
/// </summary>
public interface IAuthorizationManagement
{
    // ── Resource server ───────────────────────────────────────────────────────
    /// <summary>Turns on Authorization Services for a client (it must be confidential; service accounts are enabled too).</summary>
    Task<KeycloakBaseResponse> EnableAuthorizationAsync(string clientUuid, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<ResourceServerResponseDto>> GetResourceServerAsync(string clientUuid, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateResourceServerAsync(string clientUuid, UpdateResourceServerRequestDto requestDto, CancellationToken cancellationToken = default);

    // ── Scopes ────────────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<AuthorizationScopeResponseDto>>> GetScopesAsync(string clientUuid, string? name = null, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<AuthorizationScopeResponseDto>> CreateScopeAsync(string clientUuid, CreateAuthorizationScopeRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default);

    // ── Resources ─────────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<ResourceResponseDto>>> GetResourcesAsync(string clientUuid, GetResourcesQueryDto? query = null, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<ResourceResponseDto>> GetResourceAsync(string clientUuid, string resourceId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<ResourceResponseDto>> CreateResourceAsync(string clientUuid, CreateResourceRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateResourceAsync(string clientUuid, string resourceId, UpdateResourceRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteResourceAsync(string clientUuid, string resourceId, CancellationToken cancellationToken = default);

    // ── Policies ──────────────────────────────────────────────────────────────
    /// <summary>Lists policies (not permissions).</summary>
    Task<KeycloakBaseResponse<List<PolicyResponseDto>>> GetPoliciesAsync(string clientUuid, GetPoliciesQueryDto? query = null, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<PolicyResponseDto>> GetPolicyAsync(string clientUuid, string policyId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<PolicyResponseDto>> CreateRolePolicyAsync(string clientUuid, CreateRolePolicyRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<PolicyResponseDto>> CreateGroupPolicyAsync(string clientUuid, CreateGroupPolicyRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<PolicyResponseDto>> CreateUserPolicyAsync(string clientUuid, CreateUserPolicyRequestDto requestDto, CancellationToken cancellationToken = default);
    /// <summary>Deletes a policy or a permission.</summary>
    Task<KeycloakBaseResponse> DeletePolicyAsync(string clientUuid, string policyId, CancellationToken cancellationToken = default);

    // ── Permissions ───────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<PolicyResponseDto>>> GetPermissionsAsync(string clientUuid, GetPermissionsQueryDto? query = null, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<PolicyResponseDto>> CreateResourcePermissionAsync(string clientUuid, CreateResourcePermissionRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<PolicyResponseDto>> CreateScopePermissionAsync(string clientUuid, CreateScopePermissionRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeletePermissionAsync(string clientUuid, string permissionId, CancellationToken cancellationToken = default);

    // ── Evaluation ────────────────────────────────────────────────────────────
    /// <summary>
    /// Admin-side evaluation of what a user may do on the resource server (optionally only some resources),
    /// without the user's token. Useful for "why can/can't this user ...?" screens.
    /// </summary>
    Task<KeycloakBaseResponse<PolicyEvaluationResponseDto>> EvaluateAsync(string clientUuid, string userId, IReadOnlyCollection<string>? resourceIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks Keycloak whether the user behind <paramref name="userAccessToken"/> has a permission on the
    /// resource server <paramref name="audience"/> (its clientId). A denial is a successful response with
    /// <c>Granted = false</c>.
    /// </summary>
    Task<KeycloakBaseResponse<PermissionDecisionDto>> CheckPermissionAsync(string userAccessToken, string audience, PermissionRequest permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the permissions the user is granted on <paramref name="audience"/>, optionally limited to
    /// <paramref name="permissions"/>. No granted permission is a successful, empty response.
    /// </summary>
    Task<KeycloakBaseResponse<List<GrantedPermissionDto>>> GetGrantedPermissionsAsync(string userAccessToken, string audience, IReadOnlyCollection<PermissionRequest>? permissions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges the user's access token for a requesting party token (RPT) whose <c>authorization</c>
    /// claim lists the granted permissions, for passing to downstream services.
    /// </summary>
    Task<KeycloakBaseResponse<SigninResponseDto>> GetRequestingPartyTokenAsync(string userAccessToken, string audience, IReadOnlyCollection<PermissionRequest>? permissions = null, CancellationToken cancellationToken = default);
}
