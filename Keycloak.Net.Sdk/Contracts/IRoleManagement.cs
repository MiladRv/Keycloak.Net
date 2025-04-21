using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Contracts;

public interface IRoleManagement
{
     Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles();
     Task<KeycloakBaseResponse> AssignClientRoleToUser(string userId, string roleId, string roleName);
     Task<KeycloakBaseResponse> RemoveClientRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default);
}