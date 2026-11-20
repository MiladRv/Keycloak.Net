using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.UserFederation.Contracts;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.UserFederation;

public sealed class UserFederationManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IUserFederationManagement
{
    internal const string UserStorageProviderType = "org.keycloak.storage.UserStorageProvider";
    internal const string LdapMapperType = "org.keycloak.storage.ldap.mappers.LDAPStorageMapper";

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    private string RealmUrl => $"admin/realms/{keyCloakConfiguration.Value.RealmName}";
    private string ComponentUrl(string id) => $"{RealmUrl}/components/{Uri.EscapeDataString(id)}";
    private string UserStorageUrl(string id) => $"{RealmUrl}/user-storage/{Uri.EscapeDataString(id)}";

    public async Task<KeycloakBaseResponse<List<UserStorageProviderResponseDto>>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{RealmUrl}/components?type={Uri.EscapeDataString(UserStorageProviderType)}", cancellationToken);
        return await response.HandleResponseAsync<List<UserStorageProviderResponseDto>>();
    }

    public async Task<KeycloakBaseResponse<UserStorageProviderResponseDto>> GetProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ComponentUrl(providerId), cancellationToken);
        return await response.HandleResponseAsync<UserStorageProviderResponseDto>();
    }

    public async Task<KeycloakBaseResponse<UserStorageProviderResponseDto>> CreateProviderAsync(CreateUserStorageProviderRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestDto);

        // Components hang off the realm by its internal id, which may differ from its name.
        var realm = await _httpClient.GetAsync(RealmUrl, cancellationToken);
        if (!realm.IsSuccessStatusCode)
            return await realm.HandleResponseAsync<UserStorageProviderResponseDto>();
        using var realmJson = JsonDocument.Parse(await realm.Content.ReadAsStringAsync(cancellationToken));
        var realmId = realmJson.RootElement.GetProperty("id").GetString();

        var body = new
        {
            name         = requestDto.Name,
            providerId   = requestDto.ProviderId,
            providerType = UserStorageProviderType,
            parentId     = realmId,
            config       = requestDto.Config
        };
        var created = await SendJsonAsync(HttpMethod.Post, $"{RealmUrl}/components", JsonSerializer.Serialize(body), cancellationToken);
        if (!created.IsSuccessStatusCode)
            return await created.HandleResponseAsync<UserStorageProviderResponseDto>();

        var id = created.Headers.Location?.ToString().TrimEnd('/').Split('/').Last();
        if (string.IsNullOrEmpty(id))
            return new KeycloakFailureResponse<UserStorageProviderResponseDto>(created.StatusCode, "Keycloak did not return the new provider's location.");
        return await GetProviderAsync(Uri.UnescapeDataString(id), cancellationToken);
    }

    public async Task<KeycloakBaseResponse> UpdateProviderAsync(string providerId, UpdateUserStorageProviderRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestDto);
        var url = ComponentUrl(providerId);

        // The PUT replaces the whole component: read it and change only what was given.
        var existing = await _httpClient.GetAsync(url, cancellationToken);
        if (!existing.IsSuccessStatusCode)
            return await existing.HandleResponseAsync();

        var component = JsonNode.Parse(await existing.Content.ReadAsStringAsync(cancellationToken))!.AsObject();
        if (requestDto.Name is not null) component["name"] = requestDto.Name;
        if (requestDto.Config is { Count: > 0 })
        {
            var config = component["config"] as JsonObject ?? new JsonObject();
            foreach (var (key, values) in requestDto.Config)
                config[key] = JsonSerializer.SerializeToNode(values);
            component["config"] = config;
        }

        var response = await SendJsonAsync(HttpMethod.Put, url, component.ToJsonString(), cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> DeleteProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(ComponentUrl(providerId), cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<List<UserStorageProviderResponseDto>>> GetProviderMappersAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var url = $"{RealmUrl}/components?parent={Uri.EscapeDataString(providerId)}&type={Uri.EscapeDataString(LdapMapperType)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        return await response.HandleResponseAsync<List<UserStorageProviderResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> TestLdapConnectionAsync(string providerId, bool authenticate = true, CancellationToken cancellationToken = default)
    {
        var provider = await GetProviderAsync(providerId, cancellationToken);
        if (!provider.IsSuccessful)
            return provider;

        var config = provider.Response.Config ?? [];
        string? Value(string key) => config.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;

        // With componentId set, Keycloak uses the stored bind credential in place of the masked one.
        var body = new Dictionary<string, string?>
        {
            ["action"]            = authenticate ? "testAuthentication" : "testConnection",
            ["componentId"]       = provider.Response.Id,
            ["connectionUrl"]     = Value("connectionUrl"),
            ["authType"]          = Value("authType"),
            ["bindDn"]            = Value("bindDn"),
            ["bindCredential"]    = Value("bindCredential"),
            ["useTruststoreSpi"]  = Value("useTruststoreSpi"),
            ["connectionTimeout"] = Value("connectionTimeout"),
            ["startTls"]          = Value("startTls")
        };
        var json = JsonSerializer.Serialize(body.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value));
        var response = await SendJsonAsync(HttpMethod.Post, $"{RealmUrl}/testLDAPConnection", json, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<SynchronizationResultDto>> SyncUsersAsync(string providerId, bool changedOnly = false, CancellationToken cancellationToken = default)
    {
        var action = changedOnly ? "triggerChangedUsersSync" : "triggerFullSync";
        var response = await SendJsonAsync(HttpMethod.Post, $"{UserStorageUrl(providerId)}/sync?action={action}", null, cancellationToken);
        return await response.HandleResponseAsync<SynchronizationResultDto>();
    }

    public async Task<KeycloakBaseResponse> RemoveImportedUsersAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var response = await SendJsonAsync(HttpMethod.Post, $"{UserStorageUrl(providerId)}/remove-imported-users", null, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> UnlinkUsersAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var response = await SendJsonAsync(HttpMethod.Post, $"{UserStorageUrl(providerId)}/unlink-users", null, cancellationToken);
        return await response.HandleResponseAsync();
    }

    private Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string url, string? json, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return _httpClient.SendAsync(request, cancellationToken);
    }
}
