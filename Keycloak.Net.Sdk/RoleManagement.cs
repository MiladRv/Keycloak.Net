using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public sealed class RoleManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IRoleManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles()
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{keyCloakConfiguration.Value.ClientUuid}/roles";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request);

        return await response.HandleResponseAsync<List<ClientRoleResponseDto>>();
    }

    public async Task AssignRoleToUser(string userId, string roleId, string roleName)
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

        await _httpClient.SendAsync(request);
    }
}