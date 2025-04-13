using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public class RealmManagement(AsyncRetryPolicy<HttpResponseMessage> retryPolicy,
    IOptions<KeycloakConfiguration> keyCloakConfiguration) : IRealmManagement
{
    private readonly KeycloakConfiguration _keyCloakConfiguration = keyCloakConfiguration.Value;

    public async Task<KeycloakBaseResponse> CreateRealmAsync(string realmName)
    {
        var adminToken = await GetKeycloakAdminTokenAsync();

        if (!adminToken.IsSuccessful)
            throw new KeycloakException(_keyCloakConfiguration.ClientId, _keyCloakConfiguration.RealmName, "could not get keycloak's admin token");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken.Response.AccessToken);

        var realmData = new
        {
            realm = realmName,
            enabled = true
        };

        var json = JsonSerializer.Serialize(realmData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response =await retryPolicy.ExecuteAsync(async () => await client.PostAsync($"{_keyCloakConfiguration.ServerUrl}/admin/realms", content));

        return !response.IsSuccessStatusCode
            ? new KeycloakFailureResponse(response.StatusCode)
            : new KeycloakBaseResponse(true, HttpStatusCode.OK);
    }
    
    private async Task<KeycloakBaseResponse<SigninResponseDto>> GetKeycloakAdminTokenAsync()
    {
        using var client = new HttpClient();

        var parameters = new Dictionary<string, string>
        {
            { "client_id", "admin-cli" },
            { "grant_type", "password" },
            { "username", _keyCloakConfiguration.AdminUsername },
            { "password", _keyCloakConfiguration.AdminPassword }
        };

        var response = await client.PostAsync($"{_keyCloakConfiguration.ServerUrl}/realms/master/protocol/openid-connect/token", new FormUrlEncodedContent(parameters));

        return await response.HandleResponseAsync<SigninResponseDto>();
    }
}