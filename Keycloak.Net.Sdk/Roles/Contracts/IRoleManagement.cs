using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Roles.Contracts;

public interface IRoleManagement
{
    // ── Client Roles ──────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignClientRoleToUser(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveClientRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);

    // ── Realm Roles ───────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetRealmRolesAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<RealmRoleResponseDto>> GetRealmRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateRealmRoleAsync(CreateRealmRoleRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteRealmRoleAsync(string roleName, CancellationToken cancellationToken = default);

    // ── Realm Role ↔ User ─────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetUserRealmRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignRealmRoleToUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveRealmRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);

    // ── Realm Role ↔ Group ────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetGroupRealmRolesAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignRealmRoleToGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveRealmRoleFromGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default);
}
