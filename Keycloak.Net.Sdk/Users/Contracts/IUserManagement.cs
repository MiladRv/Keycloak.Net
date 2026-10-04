using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Users.Contracts;

public interface IUserManagement
{
    Task<KeycloakBaseResponse> SignupAsync(SignupRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<SigninResponseDto>> SigninAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<UserInfoResponseDto>> GetUserAsync(string id, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUsersAsync(GetUsersQueryDto? query = null, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> EnableUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DisableUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> SetUserPasswordAsync(string userId, string password, bool temporary = false, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateUserAsync(string userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<Dictionary<string, List<string>>>> GetUserAttributesAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> SetUserAttributeAsync(string userId, string key, string value, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUsersByEmailAsync(string email, CancellationToken cancellationToken = default);

    // ── Credentials ───────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<CredentialResponseDto>>> GetUserCredentialsAsync(string userId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteUserCredentialAsync(string userId, string credentialId, CancellationToken cancellationToken = default);
}
