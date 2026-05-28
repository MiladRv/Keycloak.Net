namespace Keycloak.Net.Sdk.Athentications.Contracts;

public interface ITokenProvider
{
    Task<string> GetTokenAsync();
}