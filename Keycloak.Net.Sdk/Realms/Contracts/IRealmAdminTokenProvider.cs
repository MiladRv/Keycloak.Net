namespace Keycloak.Net.Sdk.Realms.Contracts;

public interface IRealmAdminTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
