using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Contracts;

public interface IRoleManagement
{
     Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles();
     Task AssignRoleToUser(string userId, string roleId, string roleName);
}