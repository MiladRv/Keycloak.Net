using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Athentications.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Realms;

public sealed class RealmManagement(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakConfiguration> keyCloakConfiguration) : IRealmManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak-admin");

    public async Task<KeycloakBaseResponse> CreateRealmAsync(string realmName)
    {
        var adminToken = await GetKeycloakAdminTokenAsync();

        if (!adminToken.IsSuccessful)
            throw new KeycloakException(keyCloakConfiguration.Value.ClientId, keyCloakConfiguration.Value.RealmName, "could not get keycloak's admin token");

        var realmData = new
        {
            realm = realmName,
            enabled = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "admin/realms")
        {
            Content = new StringContent(JsonSerializer.Serialize(realmData), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken.Response.AccessToken);

        var response = await _httpClient.SendAsync(request);

        return await response.HandleResponseAsync();
    }

    private async Task<KeycloakBaseResponse<SigninResponseDto>> GetKeycloakAdminTokenAsync()
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", "admin-cli" },
            { "grant_type", "password" },
            { "username", keyCloakConfiguration.Value.AdminUsername },
            { "password", keyCloakConfiguration.Value.AdminPassword }
        };

        var response = await _httpClient.PostAsync("realms/master/protocol/openid-connect/token", new FormUrlEncodedContent(parameters));

        return await response.HandleResponseAsync<SigninResponseDto>();
    }
}
