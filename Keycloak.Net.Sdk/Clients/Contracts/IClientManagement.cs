using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Clients.Contracts;

public interface IClientManagement
{
    Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<ClientScopeResponseDto>> GetClientScopeAsync(string scopeId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateClientScopeAsync(CreateClientScopeRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateClientScopeAsync(string scopeId, UpdateClientScopeRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteClientScopeAsync(string scopeId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ClientResponseDto>>> GetClientsAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateClientAsync(CreateClientRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteClientAsync(string clientId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> EnableServiceAccountAsync(string clientId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<ProtocolMapperResponseDto>>> GetProtocolMappersAsync(string clientUuid, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<ProtocolMapperResponseDto>> GetProtocolMapperAsync(string clientUuid, string mapperId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateProtocolMapperAsync(string clientUuid, CreateProtocolMapperRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateProtocolMapperAsync(string clientUuid, string mapperId, UpdateProtocolMapperRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteProtocolMapperAsync(string clientUuid, string mapperId, CancellationToken cancellationToken = default);

    // ── Default Client Scopes ─────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetDefaultClientScopesAsync(string clientUuid, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AddDefaultClientScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveDefaultClientScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default);

    // ── Optional Client Scopes ────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetOptionalClientScopesAsync(string clientUuid, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AddOptionalClientScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveOptionalClientScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default);
}