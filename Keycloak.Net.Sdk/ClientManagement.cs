using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public class ClientManagement(IHttpClientFactory httpClientFactory, KeycloakConfiguration keyCloakConfiguration)
    : IClientManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes()
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.RealmName}/client-scopes";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request);
        return await response.HandleResponseAsync<List<ClientScopeResponseDto>>();
    }
}