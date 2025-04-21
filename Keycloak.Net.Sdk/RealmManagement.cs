using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public sealed class RealmManagement(IOptions<KeycloakConfiguration> keyCloakConfiguration) : IRealmManagement
{
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: keyCloakConfiguration.Value.NumberOfRetries, // Number of retries
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(keyCloakConfiguration.Value.DelayBetweenRetryRequestsInSeconds));

    public async Task<KeycloakBaseResponse> CreateRealmAsync(string realmName)
    {
        var adminToken = await GetKeycloakAdminTokenAsync();

        if (!adminToken.IsSuccessful)
            throw new KeycloakException(keyCloakConfiguration.Value.ClientId, keyCloakConfiguration.Value.RealmName, "could not get keycloak's admin token");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken.Response.AccessToken);

        var realmData = new
        {
            realm = realmName,
            enabled = true
        };

        var json = JsonSerializer.Serialize(realmData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _retryPolicy.ExecuteAsync(async () => await client.PostAsync($"{keyCloakConfiguration.Value.ServerUrl}/admin/realms", content));

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
            { "username", keyCloakConfiguration.Value.AdminUsername },
            { "password", keyCloakConfiguration.Value.AdminPassword }
        };

        var response = await client.PostAsync($"{keyCloakConfiguration.Value.ServerUrl}/realms/master/protocol/openid-connect/token", new FormUrlEncodedContent(parameters));

        return await response.HandleResponseAsync<SigninResponseDto>();
    }
}