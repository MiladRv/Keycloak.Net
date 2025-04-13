using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public sealed class RoleManagement(AsyncRetryPolicy<HttpResponseMessage> retryPolicy,
    HttpClient httpClient,
    IOptions<KeycloakConfiguration> keyCloakConfiguration) : IRoleManagement
{
    private readonly KeycloakConfiguration _keyCloakConfiguration = keyCloakConfiguration.Value;

    public async Task<KeycloakBaseResponse<List<ClientRoleResponseDto>>> GetClientRoles()
    {
        var requestUrl = $"/admin/realms/{_keyCloakConfiguration.RealmName}/clients/{_keyCloakConfiguration.ClientUuid}/roles";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        
        var response = await retryPolicy.ExecuteAsync(async () => await httpClient.SendAsync(request));

        return await response.HandleResponseAsync<List<ClientRoleResponseDto>>();
    }

    public async Task AssignRoleToUser(string userId, string roleId, string roleName)
    {
        var requestUrl = $"/admin/realms/{_keyCloakConfiguration.RealmName}/users/{userId}/role-mappings/clients/{_keyCloakConfiguration.ClientUuid}";

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

        await retryPolicy.ExecuteAsync(async () => await httpClient.SendAsync(request));
    }
}