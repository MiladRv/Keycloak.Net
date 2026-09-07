using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Clients;

public class ClientManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IClientManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    public async Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetClientScopes(CancellationToken cancellationToken = default)
    {
        var requestUrl = $"/admin/realms/{keyCloakConfiguration.Value.RealmName}/client-scopes";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<List<ClientScopeResponseDto>>();
    }

    public async Task<KeycloakBaseResponse<ClientScopeResponseDto>> GetClientScopeAsync(string scopeId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/client-scopes/{scopeId}", UriKind.Relative);
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<ClientScopeResponseDto>();
    }

    public async Task<KeycloakBaseResponse> CreateClientScopeAsync(CreateClientScopeRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/client-scopes", UriKind.Relative);

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> UpdateClientScopeAsync(string scopeId, UpdateClientScopeRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/client-scopes/{scopeId}", UriKind.Relative);

        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteClientScopeAsync(string scopeId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/client-scopes/{scopeId}", UriKind.Relative);

        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
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

    public async Task<KeycloakBaseResponse<ClientResponseDto>> GetClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientId}", UriKind.Relative);

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await response.HandleResponseAsync<ClientResponseDto>();
    }

    public async Task<KeycloakBaseResponse> UpdateClientAsync(string clientId, UpdateClientRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientId}", UriKind.Relative);

        // Keycloak's client PUT replaces the whole representation, so we fetch it first and
        // only apply the fields the caller actually provided - otherwise every other client
        // setting (redirect URIs, secret, protocol mappers, ...) would be wiped out.
        var getResponse = await _httpClient.GetAsync(uri, cancellationToken);
        if (!getResponse.IsSuccessStatusCode)
            return new KeycloakFailureResponse(getResponse.StatusCode, getResponse.ReasonPhrase);

        var existingClient = JsonNode.Parse(await getResponse.Content.ReadAsStringAsync(cancellationToken))!.AsObject();

        if (requestDto.Name is not null) existingClient["name"] = requestDto.Name;
        if (requestDto.Enabled.HasValue) existingClient["enabled"] = requestDto.Enabled.Value;
        if (requestDto.PublicClient.HasValue) existingClient["publicClient"] = requestDto.PublicClient.Value;
        if (requestDto.ServiceAccountsEnabled.HasValue) existingClient["serviceAccountsEnabled"] = requestDto.ServiceAccountsEnabled.Value;

        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(existingClient.ToJsonString(), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> EnableServiceAccountAsync(string clientId, CancellationToken cancellationToken = default)
        => await UpdateClientAsync(clientId, new UpdateClientRequestDto { ServiceAccountsEnabled = true }, cancellationToken);

    public async Task<KeycloakBaseResponse<List<ProtocolMapperResponseDto>>> GetProtocolMappersAsync(string clientUuid, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/protocol-mappers/models", UriKind.Relative);

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await response.HandleResponseAsync<List<ProtocolMapperResponseDto>>();
    }

    public async Task<KeycloakBaseResponse<ProtocolMapperResponseDto>> GetProtocolMapperAsync(string clientUuid, string mapperId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/protocol-mappers/models/{mapperId}", UriKind.Relative);

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await response.HandleResponseAsync<ProtocolMapperResponseDto>();
    }

    public async Task<KeycloakBaseResponse> CreateProtocolMapperAsync(string clientUuid, CreateProtocolMapperRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/protocol-mappers/models", UriKind.Relative);

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> UpdateProtocolMapperAsync(string clientUuid, string mapperId, UpdateProtocolMapperRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/protocol-mappers/models/{mapperId}", UriKind.Relative);

        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteProtocolMapperAsync(string clientUuid, string mapperId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/protocol-mappers/models/{mapperId}", UriKind.Relative);

        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
    }

    // ── Default Client Scopes ─────────────────────────────────────────────────

    public async Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetDefaultClientScopesAsync(string clientUuid, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/default-client-scopes", UriKind.Relative);

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await response.HandleResponseAsync<List<ClientScopeResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> AddDefaultClientScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/default-client-scopes/{scopeId}", UriKind.Relative);
        var request = new HttpRequestMessage(HttpMethod.Put, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> RemoveDefaultClientScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/default-client-scopes/{scopeId}", UriKind.Relative);

        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
    }

    // ── Optional Client Scopes ────────────────────────────────────────────────

    public async Task<KeycloakBaseResponse<List<ClientScopeResponseDto>>> GetOptionalClientScopesAsync(string clientUuid, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/optional-client-scopes", UriKind.Relative);

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await response.HandleResponseAsync<List<ClientScopeResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> AddOptionalClientScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/optional-client-scopes/{scopeId}", UriKind.Relative);
        var request = new HttpRequestMessage(HttpMethod.Put, uri);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> RemoveOptionalClientScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"admin/realms/{keyCloakConfiguration.Value.RealmName}/clients/{clientUuid}/optional-client-scopes/{scopeId}", UriKind.Relative);

        var response = await _httpClient.DeleteAsync(uri, cancellationToken);
        return await response.HandleResponseAsync();
    }
}