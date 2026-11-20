using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.IdentityProviders.Contracts;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.IdentityProviders;

public sealed class IdentityProviderManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IIdentityProviderManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    private string RealmUrl => $"admin/realms/{keyCloakConfiguration.Value.RealmName}";
    private string InstancesUrl => $"{RealmUrl}/identity-provider/instances";
    private string InstanceUrl(string alias) => $"{InstancesUrl}/{Uri.EscapeDataString(alias)}";
    private string FederatedIdentitiesUrl(string userId) => $"{RealmUrl}/users/{Uri.EscapeDataString(userId)}/federated-identity";

    public Task<KeycloakBaseResponse<List<IdentityProviderResponseDto>>> GetIdentityProvidersAsync(CancellationToken cancellationToken = default)
        => GetAsync<List<IdentityProviderResponseDto>>(InstancesUrl, cancellationToken);

    public Task<KeycloakBaseResponse<IdentityProviderResponseDto>> GetIdentityProviderAsync(string alias, CancellationToken cancellationToken = default)
        => GetAsync<IdentityProviderResponseDto>(InstanceUrl(alias), cancellationToken);

    public Task<KeycloakBaseResponse> CreateIdentityProviderAsync(CreateIdentityProviderRequestDto requestDto, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, InstancesUrl, requestDto, cancellationToken);

    public async Task<KeycloakBaseResponse> UpdateIdentityProviderAsync(string alias, UpdateIdentityProviderRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestDto);
        var url = InstanceUrl(alias);

        // The PUT replaces the whole representation: read it and change only what was given.
        var existing = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        if (!existing.IsSuccessStatusCode)
            return await existing.HandleResponseAsync();

        var provider = JsonNode.Parse(await existing.Content.ReadAsStringAsync(cancellationToken))!.AsObject();
        if (requestDto.DisplayName is not null) provider["displayName"] = requestDto.DisplayName;
        if (requestDto.Enabled.HasValue) provider["enabled"] = requestDto.Enabled.Value;
        if (requestDto.TrustEmail.HasValue) provider["trustEmail"] = requestDto.TrustEmail.Value;
        if (requestDto.StoreToken.HasValue) provider["storeToken"] = requestDto.StoreToken.Value;
        if (requestDto.LinkOnly.HasValue) provider["linkOnly"] = requestDto.LinkOnly.Value;
        if (requestDto.HideOnLogin.HasValue) provider["hideOnLogin"] = requestDto.HideOnLogin.Value;
        if (requestDto.FirstBrokerLoginFlowAlias is not null) provider["firstBrokerLoginFlowAlias"] = requestDto.FirstBrokerLoginFlowAlias;
        if (requestDto.Config is { Count: > 0 })
        {
            var config = provider["config"] as JsonObject ?? new JsonObject();
            foreach (var (key, value) in requestDto.Config)
                config[key] = value;
            provider["config"] = config;
        }

        return await SendJsonAsync(HttpMethod.Put, url, provider.ToJsonString(), cancellationToken);
    }

    public Task<KeycloakBaseResponse> DeleteIdentityProviderAsync(string alias, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, InstanceUrl(alias), null, cancellationToken);

    // ── Mappers ───────────────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<IdentityProviderMapperResponseDto>>> GetMappersAsync(string alias, CancellationToken cancellationToken = default)
        => GetAsync<List<IdentityProviderMapperResponseDto>>($"{InstanceUrl(alias)}/mappers", cancellationToken);

    public Task<KeycloakBaseResponse> CreateMapperAsync(string alias, CreateIdentityProviderMapperRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestDto);
        var body = new
        {
            name                   = requestDto.Name,
            identityProviderAlias  = alias,
            identityProviderMapper = requestDto.IdentityProviderMapper,
            config                 = requestDto.Config
        };
        return SendAsync(HttpMethod.Post, $"{InstanceUrl(alias)}/mappers", body, cancellationToken);
    }

    public Task<KeycloakBaseResponse> DeleteMapperAsync(string alias, string mapperId, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, $"{InstanceUrl(alias)}/mappers/{Uri.EscapeDataString(mapperId)}", null, cancellationToken);

    // ── Federated identities ─────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<FederatedIdentityDto>>> GetUserFederatedIdentitiesAsync(string userId, CancellationToken cancellationToken = default)
        => GetAsync<List<FederatedIdentityDto>>(FederatedIdentitiesUrl(userId), cancellationToken);

    public Task<KeycloakBaseResponse> LinkUserAsync(string userId, string alias, string externalUserId, string externalUserName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(externalUserId);
        ArgumentException.ThrowIfNullOrEmpty(externalUserName);
        var body = new FederatedIdentityDto { IdentityProvider = alias, UserId = externalUserId, UserName = externalUserName };
        return SendAsync(HttpMethod.Post, $"{FederatedIdentitiesUrl(userId)}/{Uri.EscapeDataString(alias)}", body, cancellationToken);
    }

    public Task<KeycloakBaseResponse> UnlinkUserAsync(string userId, string alias, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, $"{FederatedIdentitiesUrl(userId)}/{Uri.EscapeDataString(alias)}", null, cancellationToken);

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    private async Task<KeycloakBaseResponse<T>> GetAsync<T>(string url, CancellationToken cancellationToken) where T : class, new()
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        return await response.HandleResponseAsync<T>();
    }

    private Task<KeycloakBaseResponse> SendAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
        => SendJsonAsync(method, url, body is null ? null : JsonSerializer.Serialize(body, body.GetType()), cancellationToken);

    private async Task<KeycloakBaseResponse> SendJsonAsync(HttpMethod method, string url, string? json, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }
}
