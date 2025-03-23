using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Requests;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public sealed class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly KeycloakConfiguration _keyCloakConfiguration;
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public KeycloakAdminClient(HttpClient httpClient,
        IOptions<KeycloakConfiguration> keycloakOptions)
    {
        _httpClient = httpClient;
        _keyCloakConfiguration = keycloakOptions.Value;
        // Define the circuit breaker policy
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 3, // Number of retries
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(2), // Delay between each retry
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    Console.WriteLine(
                        $"Retry {retryAttempt} after {timespan.TotalSeconds} seconds due to: {outcome.Exception?.Message ?? outcome.Result.ReasonPhrase}");
                });


        var adminToken = GetAdminTokenAsync()
            .GetAwaiter()
            .GetResult();

        if (adminToken.IsSuccessful)
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", adminToken.Response!.AccessToken);
        else
            throw new AuthenticationException("Failed to authenticate to Keycloak");
    }

    private async Task<KeycloakBaseResponse<SigninResponseDto>> GetAdminTokenAsync()
    {
        var uri = $"realms/{_keyCloakConfiguration.RealmName}/protocol/openid-connect/token";

        var requestData = new Dictionary<string, string>
        {
            { "client_id", _keyCloakConfiguration.ClientId },
            { "client_secret", _keyCloakConfiguration.ClientSecret },
            // { "username", _keyCloakConfiguration.AdminUsername },
            // { "password", _keyCloakConfiguration.AdminPassword },
            { "grant_type", "client_credentials" }
        };

        var requestContent = new FormUrlEncodedContent(requestData);

        var response = await _retryPolicy
            .ExecuteAsync(async () => await _httpClient.PostAsync(uri, requestContent));

        return await HandleResponse<SigninResponseDto>(response);
    }

    public async Task<KeycloakBaseResponse> SignupAsync(string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{_keyCloakConfiguration.RealmName}/users", UriKind.Relative);

        var signupRequestDto = new SignupRequestDto(username, password);

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(signupRequestDto),
                Encoding.UTF8,
                "application/json")
        };

        var signupResponse = await _retryPolicy
            .ExecuteAsync(async () => await _httpClient.SendAsync(request, cancellationToken));

        if (!signupResponse.IsSuccessStatusCode)
            return new KeycloakFailureResponse<UserInfoResponseDto>(signupResponse.StatusCode,
                signupResponse.ReasonPhrase);

        return new KeycloakBaseResponse(true, HttpStatusCode.OK);
    }

    public async Task<KeycloakBaseResponse<SigninResponseDto>> SigninAsync(string username, string password,
        CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var uri = new Uri($"realms/{_keyCloakConfiguration.RealmName}/protocol/openid-connect/token", UriKind.Relative);

        var requestData = new Dictionary<string, string>
        {
            { "client_id", _keyCloakConfiguration.ClientId },
            { "client_secret", _keyCloakConfiguration.ClientSecret },
            { "username", username },
            { "password", password },
            { "grant_type", "password" }
        };

        var requestContent = new FormUrlEncodedContent(requestData);

        var response = await _httpClient.PostAsync(uri, requestContent, cancellationToken);
        // var response = await _retryPolicy
        //     .ExecuteAsync(async () => await _httpClient.PostAsync(uri, requestContent, cancellationToken));

        return await HandleResponse<SigninResponseDto>(response);
    }

    public async Task<KeycloakBaseResponse<SigninResponseDto>> RefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken = default)
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

        var response = await _httpClient.PostAsync(uri, requestContent, cancellationToken);

        return await HandleResponse<SigninResponseDto>(response);
    }

    public async Task<KeycloakBaseResponse<UserInfoResponseDto>> GetUserAsync(string id)
    {
        var requestUrl = $"admin/realms/{_keyCloakConfiguration.RealmName}/users/{id}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _retryPolicy
            .ExecuteAsync(async () => await _httpClient.SendAsync(request));

        return await HandleResponse<UserInfoResponseDto>(response);
    }

    public async Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUserByUsernameAsync(string username,
        CancellationToken cancellationToken = default)
    {
        var uri = $"admin/realms/{_keyCloakConfiguration.RealmName}/users?username={username}";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _retryPolicy
            .ExecuteAsync(async () => await _httpClient.SendAsync(request, cancellationToken));

        return await HandleResponse<List<UserInfoResponseDto>>(response);
    }

    public async Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes()
    {
        var requestUrl = $"/admin/realms/{_keyCloakConfiguration.RealmName}/client-scopes";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _retryPolicy
            .ExecuteAsync(async () => await _httpClient.SendAsync(request));

        return await HandleResponse<List<ClientScopeResponseDto>>(response);
    }

    public async Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles()
    {
        var requestUrl = $"/admin/realms/{_keyCloakConfiguration.RealmName}" +
                         $"/clients/{_keyCloakConfiguration.ClientUuid}/roles";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        var response = await _retryPolicy
            .ExecuteAsync(async () => await _httpClient.SendAsync(request));

        return await HandleResponse<List<ClientRoleResponseDto>>(response);
    }

    public async Task AssignRoleToUser(string userId,
        string roleId,
        string roleName)
    {
        var requestUrl = $"/admin/realms/{_keyCloakConfiguration.RealmName}/" +
                         $"users/{userId}/role-mappings/" +
                         $"clients/{_keyCloakConfiguration.ClientUuid}";

        var roles = new[]
        {
            new
            {
                id = roleId,
                name = roleName
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
        };

        await _retryPolicy
            .ExecuteAsync(async () => await _httpClient.SendAsync(request));
    }

    public async Task<KeycloakBaseResponse> CreateRealmAsync(string realmName)
    {
        var adminToken = await GetKeycloakAdminTokenAsync();

        if (!adminToken.IsSuccessful)
            throw new KeycloakException(_keyCloakConfiguration.ClientId,
                _keyCloakConfiguration.RealmName,
                "could not get keycloak's admin token");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken.Response.AccessToken);

        var realmData = new
        {
            realm = realmName,
            enabled = true
        };

        var json = JsonSerializer.Serialize(realmData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{_keyCloakConfiguration.ServerUrl}/admin/realms", content);

        return !response.IsSuccessStatusCode
            ? new KeycloakFailureResponse(response.StatusCode)
            : new KeycloakBaseResponse(true, HttpStatusCode.OK);
    }

    private static async Task<KeycloakBaseResponse<T>> HandleResponse<T>(HttpResponseMessage response)
        where T : class, new()
    {
        if (!response.IsSuccessStatusCode)
            return new KeycloakFailureResponse<T>(response.StatusCode, response.ReasonPhrase);

        var responseContent = await response.Content.ReadAsStringAsync();

        var deserializedResponse = JsonSerializer.Deserialize<T>(responseContent)!;

        return new KeycloakSuccessResponse<T>(deserializedResponse);
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

        var response = await client
            .PostAsync($"{_keyCloakConfiguration.ServerUrl}/realms/master/protocol/openid-connect/token",
                new FormUrlEncodedContent(parameters));

        return await HandleResponse<SigninResponseDto>(response);
    }
}