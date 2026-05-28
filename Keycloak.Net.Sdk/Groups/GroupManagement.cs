using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Groups.Contracts;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Groups;

public sealed class GroupManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IGroupManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse> CreateGroupAsync(CreateGroupRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups", UriKind.Relative);
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}", UriKind.Relative);
        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<List<GroupResponseDto>>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var uri = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<List<GroupResponseDto>>();
    }

    public async Task<KeycloakBaseResponse<GroupResponseDto>> GetGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var uri = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/groups/{groupId}";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<GroupResponseDto>();
    }

    public async Task<KeycloakBaseResponse> AddUserToGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/groups/{groupId}", UriKind.Relative);
        var request = new HttpRequestMessage(HttpMethod.Put, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> RemoveUserFromGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/groups/{groupId}", UriKind.Relative);
        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<List<GroupResponseDto>>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var uri = $"admin/realms/{keyCloakConfiguration.Value.RealmName}/users/{userId}/groups";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<List<GroupResponseDto>>();
    }
}
