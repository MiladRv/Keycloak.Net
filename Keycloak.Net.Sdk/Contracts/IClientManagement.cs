using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Contracts;

public interface IClientManagement
{
    Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes();

}