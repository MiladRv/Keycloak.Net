using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Realms.Contracts;

namespace Keycloak.Net.Sdk.Realms;

public sealed class RealmManagement(
    IHttpClientFactory httpClientFactory,
    IRealmAdminTokenProvider adminTokenProvider) : IRealmManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak-admin");

    public async Task<KeycloakBaseResponse> CreateRealmAsync(string realmName, CancellationToken cancellationToken = default)
    {
        var adminToken = await adminTokenProvider.GetTokenAsync(cancellationToken);

        var realmData = new
        {
            realm = realmName,
            enabled = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "admin/realms")
        {
            Content = new StringContent(JsonSerializer.Serialize(realmData), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<List<RealmResponseDto>>> GetRealmsAsync(CancellationToken cancellationToken = default)
    {
        var adminToken = await adminTokenProvider.GetTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(HttpMethod.Get, "admin/realms");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<List<RealmResponseDto>>();
    }

    public async Task<KeycloakBaseResponse<RealmResponseDto>> GetRealmAsync(string realmName, CancellationToken cancellationToken = default)
    {
        var adminToken = await adminTokenProvider.GetTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(HttpMethod.Get, $"admin/realms/{realmName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync<RealmResponseDto>();
    }

    public async Task<KeycloakBaseResponse> UpdateRealmAsync(string realmName, UpdateRealmRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var adminToken = await adminTokenProvider.GetTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(HttpMethod.Put, $"admin/realms/{realmName}")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteRealmAsync(string realmName, CancellationToken cancellationToken = default)
    {
        var adminToken = await adminTokenProvider.GetTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"admin/realms/{realmName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        return await response.HandleResponseAsync();
    }
}
