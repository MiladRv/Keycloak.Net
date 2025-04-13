using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public class TokenManagement(AsyncRetryPolicy<HttpResponseMessage> retryPolicy,
    HttpClient httpClient,
    IOptions<KeycloakConfiguration> keyCloakConfiguration) : ITokenManagement
{
    private readonly KeycloakConfiguration _keyCloakConfiguration = keyCloakConfiguration.Value;

    public async Task<KeycloakBaseResponse<SigninResponseDto>> GetClientTokenAsync()
    {
        var uri = $"realms/{_keyCloakConfiguration.RealmName}/protocol/openid-connect/token";

        var requestData = new Dictionary<string, string>
        {
            { "client_id", _keyCloakConfiguration.ClientId },
            { "client_secret", _keyCloakConfiguration.ClientSecret },
            { "grant_type", "client_credentials" }
        };

        var requestContent = new FormUrlEncodedContent(requestData);

        var response = await retryPolicy.ExecuteAsync(async () => await httpClient.PostAsync(uri, requestContent));

        return await response.HandleResponseAsync<SigninResponseDto>();
    }
    
    public async Task<KeycloakBaseResponse<SigninResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"realms/{_keyCloakConfiguration.RealmName}/protocol/openid-connect/token", UriKind.Relative);

        var requestData = new Dictionary<string, string>
        {
            { "client_id", _keyCloakConfiguration.ClientId },
            { "client_secret", _keyCloakConfiguration.ClientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        var requestContent = new FormUrlEncodedContent(requestData);

        var response = await retryPolicy.ExecuteAsync(async () => await httpClient.PostAsync(uri, requestContent, cancellationToken));

        return await response.HandleResponseAsync<SigninResponseDto>();
    }
    
    public async Task<KeycloakBaseResponse> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"realms/{_keyCloakConfiguration.RealmName}/protocol/openid-connect/revoke", UriKind.Relative);

        var formData = new Dictionary<string, string>
        {
            ["client_id"] = _keyCloakConfiguration.ClientId,
            ["client_secret"] = _keyCloakConfiguration.ClientSecret,
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new FormUrlEncodedContent(formData)
        };

        var response = await retryPolicy.ExecuteAsync(() => httpClient.SendAsync(request, cancellationToken));
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return !response.IsSuccessStatusCode
            ? new KeycloakFailureResponse(response.StatusCode, content)
            : new KeycloakBaseResponse(true, response.StatusCode);
    }
}