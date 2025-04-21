using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Athentications.Contracts;

public interface ITokenManagement
{
    Task<KeycloakBaseResponse<SigninResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}