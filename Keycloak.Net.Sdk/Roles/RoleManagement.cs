using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Roles.Contracts;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Roles;

public sealed class RoleManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IRoleManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles(CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{keyCloakConfiguration.Value.ClientUuid}/roles";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<ClientRoleResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> AssignClientRoleToUser(string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/clients/{keyCloakConfiguration.Value.ClientUuid}";

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

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> RemoveClientRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/clients/{keyCloakConfiguration.Value.ClientUuid}", UriKind.Relative);

        var roles = new[]
        {
            new
            {
                id = roleId,
                name = roleName
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    // ── Realm Roles ───────────────────────────────────────────────────────────

    public async Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetRealmRolesAsync(CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/roles";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<RealmRoleResponseDto>>();
    }

    public async Task<KeycloakBaseResponse<RealmRoleResponseDto>> GetRealmRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/roles/{roleName}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<RealmRoleResponseDto>();
    }

    public async Task<KeycloakBaseResponse> CreateRealmRoleAsync(CreateRealmRoleRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/roles";

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteRealmRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/roles/{roleName}";
        var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    // ── Realm Role ↔ User ─────────────────────────────────────────────────────

    public async Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetUserRealmRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/realm";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<RealmRoleResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> AssignRealmRoleToUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/realm";

        var roles = new[] { new { id = roleId, name = roleName } };

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> RemoveRealmRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/realm";

        var roles = new[] { new { id = roleId, name = roleName } };

        var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    // ── Realm Role ↔ Group ────────────────────────────────────────────────────

    public async Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetGroupRealmRolesAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}/role-mappings/realm";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<RealmRoleResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> AssignRealmRoleToGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}/role-mappings/realm";

        var roles = new[] { new { id = roleId, name = roleName } };

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> RemoveRealmRoleFromGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}/role-mappings/realm";

        var roles = new[] { new { id = roleId, name = roleName } };

        var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }
}