using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Contracts;

public interface IUserManagement
{
    Task<KeycloakBaseResponse> SignupAsync(string username, string password, CancellationToken cancellationToken);
    Task<KeycloakBaseResponse<SigninResponseDto>> SigninAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<UserInfoResponseDto>> GetUserAsync(string id);
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken);
}