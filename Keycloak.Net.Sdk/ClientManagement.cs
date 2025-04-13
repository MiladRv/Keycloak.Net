using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;
using Polly.Retry;

namespace Keycloak.Net.Sdk;

public class ClientManagement(
    AsyncRetryPolicy<HttpResponseMessage> retryPolicy,
    HttpClient httpClient,
    IOptions<KeycloakConfiguration> keyCloakConfiguration) : IClientManagement
{
    private readonly KeycloakConfiguration _keyCloakConfiguration = keyCloakConfiguration.Value;

    public async Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes()
    {
        var requestUrl = $"/admin/realms/{_keyCloakConfiguration.RealmName}/client-scopes";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await retryPolicy.ExecuteAsync(async () => await httpClient.SendAsync(request));

        return await response.HandleResponseAsync<List<ClientScopeResponseDto>>();
    }
}