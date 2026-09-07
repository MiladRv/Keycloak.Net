using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Users;

public sealed class UserManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IUserManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse> SignupAsync(SignupRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users", UriKind.Relative);
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var signupResponse = await _httpClient.SendAsync(request, cancellationToken);
        return await signupResponse.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<SigninResponseDto>> SigninAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"realms/{keyCloakConfiguration.Value.RealmName}/protocol/openid-connect/token", UriKind.Relative);

        var requestData = new Dictionary<string, string>
        {
            { "client_id", keyCloakConfiguration.Value.ClientId },
            { "client_secret", keyCloakConfiguration.Value.ClientSecret },
            { "username", username },
            { "password", password },
            { "grant_type", "password" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new FormUrlEncodedContent(requestData)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<SigninResponseDto>();
    }

    public async Task<KeycloakBaseResponse<UserInfoResponseDto>> GetUserAsync(string id, CancellationToken cancellationToken = default)
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

    public async Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUsersAsync(GetUsersQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var qs = BuildUsersQueryString(query);
        var uri = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users{qs}";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<List<UserInfoResponseDto>>();
    }

    private static string BuildUsersQueryString(GetUsersQueryDto? query)
    {
        if (query is null) return string.Empty;

        var parameters = new List<string>();

        if (query.First.HasValue)     parameters.Add($"first={query.First.Value}");
        if (query.Max.HasValue)       parameters.Add($"max={query.Max.Value}");
        if (query.Search is not null) parameters.Add($"search={Uri.EscapeDataString(query.Search)}");
        if (query.Username is not null) parameters.Add($"username={Uri.EscapeDataString(query.Username)}");
        if (query.Email is not null)  parameters.Add($"email={Uri.EscapeDataString(query.Email)}");
        if (query.FirstName is not null) parameters.Add($"firstName={Uri.EscapeDataString(query.FirstName)}");
        if (query.LastName is not null)  parameters.Add($"lastName={Uri.EscapeDataString(query.LastName)}");
        if (query.Enabled.HasValue)   parameters.Add($"enabled={query.Enabled.Value.ToString().ToLowerInvariant()}");

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : string.Empty;
    }

    public async Task<KeycloakBaseResponse> SetUserPasswordAsync(string userId, string password, bool temporary = false, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/reset-password", UriKind.Relative);

        var passwordPayload = new SetUserPasswordRequestDto
        {
            Value = password,
            Temporary = temporary
        };

        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(passwordPayload), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}", UriKind.Relative);
        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> EnableUserAsync(string userId, CancellationToken cancellationToken = default)
        => await UpdateUserEnabledStatus(userId, true, cancellationToken);

    public async Task<KeycloakBaseResponse> DisableUserAsync(string userId, CancellationToken cancellationToken = default)
        => await UpdateUserEnabledStatus(userId, false, cancellationToken);

    private async Task<KeycloakBaseResponse> UpdateUserEnabledStatus(string userId, bool enabled, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}", UriKind.Relative);

        // Keycloak's user PUT replaces the whole representation, so we fetch it first and
        // only flip the one field we care about - otherwise every other user field
        // (email, attributes, required actions, ...) would be wiped out.
        var getResponse = await _httpClient.GetAsync(uri, cancellationToken);
        if (!getResponse.IsSuccessStatusCode)
            return new KeycloakFailureResponse(getResponse.StatusCode, getResponse.ReasonPhrase);

        var existingUser = JsonNode.Parse(await getResponse.Content.ReadAsStringAsync(cancellationToken))!.AsObject();
        existingUser["enabled"] = enabled;

        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(existingUser.ToJsonString(), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> UpdateUserAsync(string userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}", UriKind.Relative);

        var httpRequest = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<Dictionary<string, List<string>>>> GetUserAttributesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = await GetUserAsync(userId, cancellationToken);
        if (!result.IsSuccessful)
            return new KeycloakFailureResponse<Dictionary<string, List<string>>>(result.StatusCode, result.ErrorMessage);

        var attributes = result.Response.Attributes ?? new Dictionary<string, List<string>>();
        return new KeycloakBaseResponse<Dictionary<string, List<string>>>(attributes, true, result.StatusCode);
    }

    public async Task<KeycloakBaseResponse> SetUserAttributeAsync(string userId, string key, string value, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}", UriKind.Relative);

        // Keycloak's user PUT replaces the whole representation, so we fetch it first and
        // merge the new attribute in - otherwise every other user field would be wiped out.
        var getResponse = await _httpClient.GetAsync(uri, cancellationToken);
        if (!getResponse.IsSuccessStatusCode)
            return new KeycloakFailureResponse(getResponse.StatusCode, getResponse.ReasonPhrase);

        var existingUser = JsonNode.Parse(await getResponse.Content.ReadAsStringAsync(cancellationToken))!.AsObject();
        var attributes = existingUser["attributes"]?.AsObject() ?? new JsonObject();
        attributes[key] = new JsonArray(value);
        existingUser["attributes"] = attributes;

        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(existingUser.ToJsonString(), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUsersByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var uri = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users?email={Uri.EscapeDataString(email)}&exact=true";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<List<UserInfoResponseDto>>();
    }

    // â”€â”€ Credentials â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<KeycloakBaseResponse<List<CredentialResponseDto>>> GetUserCredentialsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/credentials", UriKind.Relative);

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await response.HandleResponseAsync<List<CredentialResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> DeleteUserCredentialAsync(string userId, string credentialId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/credentials/{credentialId}", UriKind.Relative);

        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
    }
}
