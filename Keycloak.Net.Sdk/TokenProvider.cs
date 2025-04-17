using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk;

public sealed class TokenProvider(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keycloakConfiguration)
    : ITokenProvider
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Keycloak");
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _token;
    private DateTime _expiresAt;

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
            _expiresAt = DateTime.UtcNow.AddSeconds(response.Response.ExpiresIn - 30); // make sure that token will be got before it be expired

            return _token!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<KeycloakBaseResponse<SigninResponseDto>> GetAdminTokenAsync()
    {
        var uri = $"realms/{keycloakConfiguration.Value.RealmName}/protocol/openid-connect/token";

        var requestData = new Dictionary<string, string>
        {
            { "client_id", keycloakConfiguration.Value.ClientId },
            { "client_secret", keycloakConfiguration.Value.ClientSecret },
            { "grant_type", "client_credentials" }
        };

        var requestContent = new FormUrlEncodedContent(requestData);

        var response = await _httpClient.PostAsync(uri, requestContent);

        return await response.HandleResponseAsync<SigninResponseDto>();
    }
}