using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Roles.Contracts;

public interface IRoleManagement
{
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AssignClientRoleToUser(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveClientRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
}
