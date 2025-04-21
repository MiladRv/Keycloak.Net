using Keycloak.Net.Sdk.Athentications.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Users.Contracts;

public interface IUserManagement
{
    Task<KeycloakBaseResponse> SignupAsync(SignupRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<SigninResponseDto>> SigninAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<UserInfoResponseDto>> GetUserAsync(string id, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task EnableUserAsync(string userId);
    Task DisableUserAsync(string userId);
    Task SetUserPasswordAsync(string userId, string password, bool temporary = false, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string userId, CancellationToken cancellation = default);
}