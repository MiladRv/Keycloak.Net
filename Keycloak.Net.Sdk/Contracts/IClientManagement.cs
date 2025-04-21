using Keycloak.Net.Sdk.Contracts.Requests;
using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Contracts;

public interface IClientManagement
{
    Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes();
    Task<KeycloakBaseResponse<List<ClientResponseDto>>> GetClientsAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateClientAsync(CreateClientRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteClientAsync(string clientId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> EnableServiceAccountAsync(string clientId, CancellationToken cancellationToken = default);
}