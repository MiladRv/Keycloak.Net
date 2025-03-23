using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Contracts;

public interface IKeycloakAdminClient
{
    Task<KeycloakBaseResponse> SignupAsync(string username, string password, CancellationToken cancellationToken);
    Task<KeycloakBaseResponse<SigninResponseDto>> SigninAsync(string username, 
        string password,
        CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<SigninResponseDto>> RefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken);
    Task<KeycloakBaseResponse<UserInfoResponseDto>> GetUserAsync(string id);
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUserByUsernameAsync(string username,
        CancellationToken cancellationToken);
    Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes();
    Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles();
    Task AssignRoleToUser(string userId, string roleId, string roleName);
    Task<KeycloakBaseResponse> CreateRealmAsync(string realmName);
}