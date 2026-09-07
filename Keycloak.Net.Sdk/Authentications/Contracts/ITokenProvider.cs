namespace Keycloak.Net.Sdk.Authentications.Contracts;

public interface ITokenProvider
{
    Task<string> GetTokenAsync();
}