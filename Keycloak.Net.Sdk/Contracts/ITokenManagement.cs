using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Contracts;

public interface ITokenManagement
{
    Task<KeycloakBaseResponse<SigninResponseDto>> GetClientTokenAsync();
    Task<KeycloakBaseResponse<SigninResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}