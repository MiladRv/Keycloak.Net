using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Realms.Contracts;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Realms;

public sealed class RealmAdminTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakConfiguration> keycloakConfiguration) : IRealmAdminTokenProvider
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak-admin");
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _token;
    private DateTime _expiresAt;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt)
            return _token;

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt)
                return _token;

            var parameters = new Dictionary<string, string>
            {
                { "client_id", "admin-cli" },
                { "grant_type", "password" },
                { "username", keycloakConfiguration.Value.AdminUsername },
                { "password", keycloakConfiguration.Value.AdminPassword }
            };

            var httpResponse = await _httpClient.PostAsync(
                "realms/master/protocol/openid-connect/token",
                new FormUrlEncodedContent(parameters),
                cancellationToken);

            var response = await httpResponse.HandleResponseAsync<SigninResponseDto>();

            if (!response.IsSuccessful)
                throw new KeycloakException(keycloakConfiguration.Value.RealmName, keycloakConfiguration.Value.ClientId, "could not get keycloak's admin token");

            _token = response.Response.AccessToken!;
            _expiresAt = DateTime.UtcNow.AddSeconds(response.Response.ExpiresIn - 30);

            return _token;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
