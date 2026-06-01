using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.UserSessions.Contracts;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.UserSessions;

public sealed class UserSessionManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IUserSessionManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse<List<UserSessionResponseDto>>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var uri = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/sessions";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<List<UserSessionResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/sessions/{sessionId}", UriKind.Relative);
        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> LogoutUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/logout", UriKind.Relative);
        var request = new HttpRequestMessage(HttpMethod.Post, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }
}
