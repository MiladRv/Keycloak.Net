using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk;

public class TokenManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : ITokenManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse<SigninResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"realms/{keyCloakConfiguration.Value.RealmName}/protocol/openid-connect/token", UriKind.Relative);

        var requestData = new Dictionary<string, string>
        {
            { "client_id", keyCloakConfiguration.Value.ClientId },
            { "client_secret", keyCloakConfiguration.Value.ClientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        var requestContent = new FormUrlEncodedContent(requestData);

        var response = await _httpClient.PostAsync(uri, requestContent, cancellationToken);

        return await response.HandleResponseAsync<SigninResponseDto>();
    }

    public async Task<KeycloakBaseResponse> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"realms/{keyCloakConfiguration.Value.RealmName}/protocol/openid-connect/revoke", UriKind.Relative);

        var formData = new Dictionary<string, string>
        {
            ["client_id"] = keyCloakConfiguration.Value.ClientId,
            ["client_secret"] = keyCloakConfiguration.Value.ClientSecret,
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new FormUrlEncodedContent(formData)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return !response.IsSuccessStatusCode
            ? new KeycloakFailureResponse(response.StatusCode, content)
            : new KeycloakBaseResponse(true, response.StatusCode);
    }
}