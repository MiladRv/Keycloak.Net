using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Roles;

public sealed class RoleManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IRoleManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    private string ConfiguredClientUuid => keyCloakConfiguration.Value.ClientUuid;

    private string ClientRolesUrl(string clientUuid) =>
        $"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{Uri.EscapeDataString(clientUuid)}/roles";

    private string ClientRoleUrl(string clientUuid, string roleName) =>
        $"{ClientRolesUrl(clientUuid)}/{Uri.EscapeDataString(roleName)}";

    private string UserClientRoleMappingsUrl(string clientUuid, string userId) =>
        $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/clients/{Uri.EscapeDataString(clientUuid)}";

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private async Task<KeycloakBaseResponse<T>> GetAsync<T>(string requestUrl, CancellationToken cancellationToken) where T : class, new()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<T>();
    }

    private async Task<KeycloakBaseResponse> SendAsync(HttpMethod method, string requestUrl, HttpContent? content, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, requestUrl) { Content = content };
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    // ── Client Roles ──────────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRolesAsync(CancellationToken cancellationToken = default)
        => GetClientRolesAsync(ConfiguredClientUuid, cancellationToken);

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRolesAsync(string clientUuid, CancellationToken cancellationToken = default)
        => GetAsync<List<ClientRoleResponseDto>>(ClientRolesUrl(clientUuid), cancellationToken);

    public Task<KeycloakBaseResponse<ClientRoleResponseDto>> GetClientRoleAsync(string roleName, CancellationToken cancellationToken = default)
        => GetClientRoleAsync(ConfiguredClientUuid, roleName, cancellationToken);

    public Task<KeycloakBaseResponse<ClientRoleResponseDto>> GetClientRoleAsync(string clientUuid, string roleName, CancellationToken cancellationToken = default)
        => GetAsync<ClientRoleResponseDto>(ClientRoleUrl(clientUuid, roleName), cancellationToken);

    public Task<KeycloakBaseResponse> CreateClientRoleAsync(CreateClientRoleRequestDto requestDto, CancellationToken cancellationToken = default)
        => CreateClientRoleAsync(ConfiguredClientUuid, requestDto, cancellationToken);

    public Task<KeycloakBaseResponse> CreateClientRoleAsync(string clientUuid, CreateClientRoleRequestDto requestDto, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, ClientRolesUrl(clientUuid), JsonContent(requestDto), cancellationToken);

    public Task<KeycloakBaseResponse> UpdateClientRoleAsync(string roleName, UpdateClientRoleRequestDto requestDto, CancellationToken cancellationToken = default)
        => UpdateClientRoleAsync(ConfiguredClientUuid, roleName, requestDto, cancellationToken);

    public Task<KeycloakBaseResponse> UpdateClientRoleAsync(string clientUuid, string roleName, UpdateClientRoleRequestDto requestDto, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, ClientRoleUrl(clientUuid, roleName), JsonContent(requestDto), cancellationToken);

    public Task<KeycloakBaseResponse> DeleteClientRoleAsync(string roleName, CancellationToken cancellationToken = default)
        => DeleteClientRoleAsync(ConfiguredClientUuid, roleName, cancellationToken);

    public Task<KeycloakBaseResponse> DeleteClientRoleAsync(string clientUuid, string roleName, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, ClientRoleUrl(clientUuid, roleName), null, cancellationToken);

    public Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetClientRoleUsersAsync(string roleName, int? first = null, int? max = null, CancellationToken cancellationToken = default)
        => GetClientRoleUsersAsync(ConfiguredClientUuid, roleName, first, max, cancellationToken);

    public Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetClientRoleUsersAsync(string clientUuid, string roleName, int? first = null, int? max = null, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>();
        if (first is not null) parameters.Add($"first={first}");
        if (max is not null) parameters.Add($"max={max}");
        var query = parameters.Count > 0 ? "?" + string.Join("&", parameters) : string.Empty;

        return GetAsync<List<UserInfoResponseDto>>($"{ClientRoleUrl(clientUuid, roleName)}/users{query}", cancellationToken);
    }

    // ── Client Role ↔ User ────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetUserClientRolesAsync(string userId, CancellationToken cancellationToken = default)
        => GetUserClientRolesAsync(ConfiguredClientUuid, userId, cancellationToken);

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetUserClientRolesAsync(string clientUuid, string userId, CancellationToken cancellationToken = default)
        => GetAsync<List<ClientRoleResponseDto>>(UserClientRoleMappingsUrl(clientUuid, userId), cancellationToken);

    public Task<KeycloakBaseResponse> AssignClientRoleToUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
        => AssignClientRoleToUserAsync(ConfiguredClientUuid, userId, roleId, roleName, cancellationToken);

    public Task<KeycloakBaseResponse> AssignClientRoleToUserAsync(string clientUuid, string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, UserClientRoleMappingsUrl(clientUuid, userId),
            JsonContent(new[] { new { id = roleId, name = roleName } }), cancellationToken);

    public Task<KeycloakBaseResponse> RemoveClientRoleFromUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
        => RemoveClientRoleFromUserAsync(ConfiguredClientUuid, userId, roleId, roleName, cancellationToken);

    public Task<KeycloakBaseResponse> RemoveClientRoleFromUserAsync(string clientUuid, string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, UserClientRoleMappingsUrl(clientUuid, userId),
            JsonContent(new[] { new { id = roleId, name = roleName } }), cancellationToken);

    // ── Client Role Composites ───────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoleCompositesAsync(string roleName, CancellationToken cancellationToken = default)
        => GetClientRoleCompositesAsync(ConfiguredClientUuid, roleName, cancellationToken);

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoleCompositesAsync(string clientUuid, string roleName, CancellationToken cancellationToken = default)
        => GetAsync<List<ClientRoleResponseDto>>($"{ClientRoleUrl(clientUuid, roleName)}/composites", cancellationToken);

    public Task<KeycloakBaseResponse> AddClientRoleCompositesAsync(string roleName, List<CompositeRoleRequestDto> composites, CancellationToken cancellationToken = default)
        => AddClientRoleCompositesAsync(ConfiguredClientUuid, roleName, composites, cancellationToken);

    public Task<KeycloakBaseResponse> AddClientRoleCompositesAsync(string clientUuid, string roleName, List<CompositeRoleRequestDto> composites, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, $"{ClientRoleUrl(clientUuid, roleName)}/composites", JsonContent(composites), cancellationToken);

    public Task<KeycloakBaseResponse> RemoveClientRoleCompositesAsync(string roleName, List<CompositeRoleRequestDto> composites, CancellationToken cancellationToken = default)
        => RemoveClientRoleCompositesAsync(ConfiguredClientUuid, roleName, composites, cancellationToken);

    public Task<KeycloakBaseResponse> RemoveClientRoleCompositesAsync(string clientUuid, string roleName, List<CompositeRoleRequestDto> composites, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, $"{ClientRoleUrl(clientUuid, roleName)}/composites", JsonContent(composites), cancellationToken);

    // ── Realm Roles ───────────────────────────────────────────────────────────

    public async Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetRealmRolesAsync(CancellationToken cancellationToken = default)
    {
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/roles";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<RealmRoleResponseDto>>();
    }

    public async Task<KeycloakBaseResponse<RealmRoleResponseDto>> GetRealmRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = RealmRoleUrl(roleName);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<RealmRoleResponseDto>();
    }

    public Task<KeycloakBaseResponse> UpdateRealmRoleAsync(string roleName, UpdateRealmRoleRequestDto requestDto, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, RealmRoleUrl(roleName), JsonContent(requestDto), cancellationToken);

    public async Task<KeycloakBaseResponse> CreateRealmRoleAsync(CreateRealmRoleRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/roles";

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteRealmRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = RealmRoleUrl(roleName);
        var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    // ── Realm Role ↔ User ─────────────────────────────────────────────────────

    public async Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetUserRealmRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/realm";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<RealmRoleResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> AssignRealmRoleToUserAsync(string userId, string roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/realm";

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
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/realm";

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
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}/role-mappings/realm";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<RealmRoleResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> AssignRealmRoleToGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}/role-mappings/realm";

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
        var requestUrl = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}/role-mappings/realm";

        var roles = new[] { new { id = roleId, name = roleName } };

        var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    // ── Client Role ↔ Group ───────────────────────────────────────────────────

    private string GroupClientRoleMappingsUrl(string clientUuid, string groupId) =>
        $"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}/role-mappings/clients/{Uri.EscapeDataString(clientUuid)}";

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetGroupClientRolesAsync(string groupId, CancellationToken cancellationToken = default)
        => GetGroupClientRolesAsync(ConfiguredClientUuid, groupId, cancellationToken);

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetGroupClientRolesAsync(string clientUuid, string groupId, CancellationToken cancellationToken = default)
        => GetAsync<List<ClientRoleResponseDto>>(GroupClientRoleMappingsUrl(clientUuid, groupId), cancellationToken);

    public Task<KeycloakBaseResponse> AssignClientRoleToGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default)
        => AssignClientRoleToGroupAsync(ConfiguredClientUuid, groupId, roleId, roleName, cancellationToken);

    public Task<KeycloakBaseResponse> AssignClientRoleToGroupAsync(string clientUuid, string groupId, string roleId, string roleName, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, GroupClientRoleMappingsUrl(clientUuid, groupId),
            JsonContent(new[] { new { id = roleId, name = roleName } }), cancellationToken);

    public Task<KeycloakBaseResponse> RemoveClientRoleFromGroupAsync(string groupId, string roleId, string roleName, CancellationToken cancellationToken = default)
        => RemoveClientRoleFromGroupAsync(ConfiguredClientUuid, groupId, roleId, roleName, cancellationToken);

    public Task<KeycloakBaseResponse> RemoveClientRoleFromGroupAsync(string clientUuid, string groupId, string roleId, string roleName, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, GroupClientRoleMappingsUrl(clientUuid, groupId),
            JsonContent(new[] { new { id = roleId, name = roleName } }), cancellationToken);

    // ── Effective roles ───────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetUserEffectiveRealmRolesAsync(string userId, CancellationToken cancellationToken = default)
        => GetAsync<List<RealmRoleResponseDto>>(
            $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/role-mappings/realm/composite", cancellationToken);

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetUserEffectiveClientRolesAsync(string userId, CancellationToken cancellationToken = default)
        => GetUserEffectiveClientRolesAsync(ConfiguredClientUuid, userId, cancellationToken);

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetUserEffectiveClientRolesAsync(string clientUuid, string userId, CancellationToken cancellationToken = default)
        => GetAsync<List<ClientRoleResponseDto>>($"{UserClientRoleMappingsUrl(clientUuid, userId)}/composite", cancellationToken);

    public Task<KeycloakBaseResponse<List<RealmRoleResponseDto>>> GetGroupEffectiveRealmRolesAsync(string groupId, CancellationToken cancellationToken = default)
        => GetAsync<List<RealmRoleResponseDto>>(
            $"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}/role-mappings/realm/composite", cancellationToken);

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetGroupEffectiveClientRolesAsync(string groupId, CancellationToken cancellationToken = default)
        => GetGroupEffectiveClientRolesAsync(ConfiguredClientUuid, groupId, cancellationToken);

    public Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetGroupEffectiveClientRolesAsync(string clientUuid, string groupId, CancellationToken cancellationToken = default)
        => GetAsync<List<ClientRoleResponseDto>>($"{GroupClientRoleMappingsUrl(clientUuid, groupId)}/composite", cancellationToken);

    private string RealmRoleUrl(string roleName) =>
        $"admin/realms/{keyCloakConfiguration.Value.RealmName}/roles/{Uri.EscapeDataString(roleName)}";
}
