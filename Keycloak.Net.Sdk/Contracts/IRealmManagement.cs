namespace Keycloak.Net.Sdk.Contracts;

public interface IRealmManagement
{
     Task<KeycloakBaseResponse> CreateRealmAsync(string realmName);
}