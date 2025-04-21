namespace Keycloak.Net.Sdk.Contracts;

public interface ITokenProvider
{
    Task<string> GetTokenAsync();
}