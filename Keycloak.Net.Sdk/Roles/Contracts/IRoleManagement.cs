using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Users.Contracts;

namespace Keycloak.Net.Sdk.Roles.Contracts;

public interface IRoleManagement
{
    // Every client-role operation has two overloads:
    //   - without clientUuid: targets the client configured in KeycloakConfiguration.ClientUuid
    //   - with clientUuid as the first parameter: targets any client in the realm (its internal id, not its clientId)

    // ── Client Roles ──────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRolesAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRolesAsync(string clientUuid, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<ClientRoleResponseDto>> GetClientRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<ClientRoleResponseDto>> GetClientRoleAsync(string clientUuid, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateClientRoleAsync(CreateClientRoleRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateClientRoleAsync(string clientUuid, CreateClientRoleRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateClientRoleAsync(string roleName, UpdateClientRoleRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateClientRoleAsync(string clientUuid, string roleName, UpdateClientRoleRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteClientRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteClientRoleAsync(string clientUuid, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetClientRoleUsersAsync(string roleName, int? first = null, int? max = null, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetClientRoleUsersAsync(string clientUuid, string roleName, int? first = null, int? max = null, CancellationToken cancellationToken = default);

    // ── Client Role ↔ User ────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetUserClientRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetUserClientRolesAsync(string clientUuid, string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignClientRoleToUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignClientRoleToUserAsync(string clientUuid, string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveClientRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveClientRoleFromUserAsync(string clientUuid, string userId, string roleId, string roleName, CancellationToken cancellationToken = default);

    // ── Client Role Composites ───────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoleCompositesAsync(string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoleCompositesAsync(string clientUuid, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AddClientRoleCompositesAsync(string roleName, List<CompositeRoleRequestDto> composites, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AddClientRoleCompositesAsync(string clientUuid, string roleName, List<CompositeRoleRequestDto> composites, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveClientRoleCompositesAsync(string roleName, List<CompositeRoleRequestDto> composites, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveClientRoleCompositesAsync(string clientUuid, string roleName, List<CompositeRoleRequestDto> composites, CancellationToken cancellationToken = default);

    // ── Realm Roles ───────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetRealmRolesAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<RealmRoleResponseDto>> GetRealmRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateRealmRoleAsync(CreateRealmRoleRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateRealmRoleAsync(string roleName, UpdateRealmRoleRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteRealmRoleAsync(string roleName, CancellationToken cancellationToken = default);

    // ── Realm Role ↔ User ─────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetUserRealmRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignRealmRoleToUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveRealmRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);

    // ── Realm Role ↔ Group ────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetGroupRealmRolesAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignRealmRoleToGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveRealmRoleFromGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default);

    // ── Client Role ↔ Group ───────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetGroupClientRolesAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetGroupClientRolesAsync(string clientUuid, string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignClientRoleToGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignClientRoleToGroupAsync(string clientUuid, string groupId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveClientRoleFromGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveClientRoleFromGroupAsync(string clientUuid, string groupId, string roleId, string roleName, CancellationToken cancellationToken = default);

    // ── Effective roles ───────────────────────────────────────────────────────
    // A user's effective roles include roles granted directly, through group membership (including
    // parent groups) and through composite roles - i.e. what ends up in the user's token.
    // A group's effective realm roles include its parent groups' roles; a group's effective client
    // roles do not (Keycloak behaviour) - they cover the group's own mappings and their composites.
    Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetUserEffectiveRealmRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetUserEffectiveClientRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetUserEffectiveClientRolesAsync(string clientUuid, string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetGroupEffectiveRealmRolesAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetGroupEffectiveClientRolesAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetGroupEffectiveClientRolesAsync(string clientUuid, string groupId, CancellationToken cancellationToken = default);
}
