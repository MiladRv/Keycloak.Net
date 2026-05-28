using Keycloak.Net.Sdk.Athentications.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Athentications;

public sealed class TokenProvider : ITokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _token;
    private DateTime _expiresAt;
    private readonly IOptions<KeycloakConfiguration> _keycloakConfiguration;

    public TokenProvider(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keycloakConfiguration)
    {
        _httpClientFactory = httpClientFactory;
        _keycloakConfiguration = keycloakConfiguration;
    }

    public async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt)
            return _token!;

        await _semaphore.WaitAsync();

        try
        {
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt)
                return _token!;

            var response = await GetAdminTokenAsync();

            _token = response.Response.AccessToken;
            _expiresAt = DateTime.UtcNow.AddSeconds(response.Response.ExpiresIn - 30);

            return _token!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<KeycloakBaseResponse<SigninResponseDto>> GetAdminTokenAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("keycloak-token");
        var uri = $"realms/{_keycloakConfiguration.Value.RealmName}/protocol/openid-connect/token";

        var requestData = new Dictionary<string, string>
        {
            { "client_id", _keycloakConfiguration.Value.ClientId },
            { "client_secret", _keycloakConfiguration.Value.ClientSecret },
            { "grant_type", "client_credentials" }
        };

        var requestContent = new FormUrlEncodedContent(requestData);
        var response = await httpClient.PostAsync(uri, requestContent);

        return await response.HandleResponseAsync<SigninResponseDto>();
    }
}
