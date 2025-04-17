using System.Net;
using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Requests;
using Keycloak.Net.Sdk.Contracts.Responses;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public sealed class UserManagement(HttpClient httpClient,
    KeycloakConfiguration keyCloakConfiguration) : IUserManagement
{
    public async Task<KeycloakBaseResponse> SignupAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.RealmName}/users", UriKind.Relative);

        var signupRequestDto = new SignupRequestDto(username, password);

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(signupRequestDto), Encoding.UTF8, "application/json")
        };

        var signupResponse = await httpClient.SendAsync(request, cancellationToken);

        return !signupResponse.IsSuccessStatusCode
            ? new KeycloakFailureResponse<UserInfoResponseDto>(signupResponse.StatusCode, signupResponse.ReasonPhrase)
            : new KeycloakBaseResponse(true, HttpStatusCode.OK);
    }

    public async Task<KeycloakBaseResponse<SigninResponseDto>> SigninAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = null;

        var uri = new Uri($"realms/{keyCloakConfiguration.RealmName}/protocol/openid-connect/token", UriKind.Relative);

        var requestData = new Dictionary<string, string>
        {
            { "client_id", keyCloakConfiguration.ClientId },
            { "client_secret", keyCloakConfiguration.ClientSecret },
            { "username", username },
            { "password", password },
            { "grant_type", "password" }
        };

        var requestContent = new FormUrlEncodedContent(requestData);

        var response = await httpClient.PostAsync(uri, requestContent, cancellationToken);
        return await response.HandleResponseAsync<SigninResponseDto>();
    }

    public async Task<KeycloakBaseResponse<UserInfoResponseDto>> GetUserAsync(string id)
    {
        var requestUrl = $"admin/realms/{keyCloakConfiguration.RealmName}/users/{id}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await httpClient.SendAsync(request);

        return await response.HandleResponseAsync<UserInfoResponseDto>();
    }

    public async Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var uri = $"admin/realms/{keyCloakConfiguration.RealmName}/users?username={username}";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<UserInfoResponseDto>>();
    }
}