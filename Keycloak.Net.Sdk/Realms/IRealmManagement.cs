using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Realms.Contracts;

namespace Keycloak.Net.Sdk.Realms;

public interface IRealmManagement
{
    Task<KeycloakBaseResponse> CreateRealmAsync(string realmName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<RealmResponseDto>>> GetRealmsAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<RealmResponseDto>> GetRealmAsync(string realmName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateRealmAsync(string realmName, UpdateRealmRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteRealmAsync(string realmName, CancellationToken cancellationToken = default);
}