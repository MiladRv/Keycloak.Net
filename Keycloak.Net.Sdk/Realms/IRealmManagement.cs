using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Realms;

public interface IRealmManagement
{
     Task<KeycloakBaseResponse> CreateRealmAsync(string realmName);
}