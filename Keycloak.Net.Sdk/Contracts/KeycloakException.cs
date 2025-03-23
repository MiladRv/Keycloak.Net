namespace Keycloak.Net.Sdk.Contracts;

public sealed class KeycloakException(
    string realmName,
    string clientId,
    string message) : Exception(message)
{
    public string ClientId { get; set; } = clientId;
    public string RealmName { get; set; } = realmName;
}