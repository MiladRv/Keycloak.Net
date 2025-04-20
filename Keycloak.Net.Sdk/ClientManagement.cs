using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public class ClientManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IClientManagement
{
    // private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    private readonly HttpClient _httpClient = null;
    public async Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes()
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/client-scopes";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request);
        return await response.HandleResponseAsync<List<ClientScopeResponseDto>>();
    }
}