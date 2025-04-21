using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Requests;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk;

public sealed class UserManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IUserManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Keycloak");

    public async Task<KeycloakBaseResponse> SignupAsync(SignupRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users", UriKind.Relative);
        var a = JsonSerializer.Serialize(requestDto);
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var signupResponse = await _httpClient.SendAsync(request, cancellationToken);
        return await signupResponse.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<SigninResponseDto>> SigninAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var uri = new Uri($"realms/{keyCloakConfiguration.Value.RealmName}/protocol/openid-connect/token", UriKind.Relative);

        var requestData = new Dictionary<string, string>
        {
            { "client_id", keyCloakConfiguration.Value.ClientId },
            { "client_secret", keyCloakConfiguration.Value.ClientSecret },
            { "username", username },
            { "password", password },
            { "grant_type", "password" }
        };

        var requestContent = new FormUrlEncodedContent(requestData);

        var response = await _httpClient.PostAsync(uri, requestContent, cancellationToken);
        return await response.HandleResponseAsync<SigninResponseDto>();
    }

    public async Task<KeycloakBaseResponse<UserInfoResponseDto>> GetUserAsync(string id, CancellationToken cancellationToken)
    {
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{id}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<UserInfoResponseDto>();
    }

    public async Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var uri = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users?username={username}";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<UserInfoResponseDto>>();
    }
}