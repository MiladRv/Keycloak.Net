using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Contracts.Requests;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk;

public class ClientManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IClientManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes()
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/client-scopes";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request);
        return await response.HandleResponseAsync<List<ClientScopeResponseDto>>();
    }
    
      public async Task<KeycloakBaseResponse<List<ClientResponseDto>>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients", UriKind.Relative);
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await response.HandleResponseAsync<List<ClientResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> CreateClientAsync(CreateClientRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var requestUrl = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients", UriKind.Relative);
        
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientId}", UriKind.Relative);

        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> EnableServiceAccountAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var requestDto = new UpdateClientStatusRequestDto() { ServiceAccountsEnabled = true };

        var requestUrl = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientId}", UriKind.Relative);
        
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        return await response.HandleResponseAsync();
    }

}