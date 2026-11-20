using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Authorization.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Extensions;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Authorization;

public sealed class AuthorizationManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IAuthorizationManagement
{
    private const string UmaTicketGrant = "urn:ietf:params:oauth:grant-type:uma-ticket";

    // Admin API calls carry the service-account token; evaluation calls carry the user's token instead.
    private readonly HttpClient _adminClient = httpClientFactory.CreateClient("keycloak");
    private readonly HttpClient _tokenClient = httpClientFactory.CreateClient("keycloak-token");

    private string Realm => keyCloakConfiguration.Value.RealmName;

    private string ClientUrl(string clientUuid) => $"admin/realms/{Realm}/clients/{Uri.EscapeDataString(clientUuid)}";

    private string ResourceServerUrl(string clientUuid) => $"{ClientUrl(clientUuid)}/authz/resource-server";

    // ── Resource server ───────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse> EnableAuthorizationAsync(string clientUuid, CancellationToken cancellationToken = default)
        => MergeAsync(ClientUrl(clientUuid), client =>
        {
            client["publicClient"] = false;
            client["serviceAccountsEnabled"] = true;
            client["authorizationServicesEnabled"] = true;
        }, cancellationToken);

    public Task<KeycloakBaseResponse<ResourceServerResponseDto>> GetResourceServerAsync(string clientUuid, CancellationToken cancellationToken = default)
        => GetAsync<ResourceServerResponseDto>(ResourceServerUrl(clientUuid), cancellationToken);

    public Task<KeycloakBaseResponse> UpdateResourceServerAsync(string clientUuid, UpdateResourceServerRequestDto requestDto, CancellationToken cancellationToken = default)
        => MergeAsync(ResourceServerUrl(clientUuid), server =>
        {
            if (requestDto.PolicyEnforcementMode is not null) server["policyEnforcementMode"] = requestDto.PolicyEnforcementMode;
            if (requestDto.DecisionStrategy is not null) server["decisionStrategy"] = requestDto.DecisionStrategy;
            if (requestDto.AllowRemoteResourceManagement.HasValue) server["allowRemoteResourceManagement"] = requestDto.AllowRemoteResourceManagement.Value;
        }, cancellationToken);

    // ── Scopes ────────────────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<AuthorizationScopeResponseDto>>> GetScopesAsync(string clientUuid, string? name = null, CancellationToken cancellationToken = default)
        => GetAsync<List<AuthorizationScopeResponseDto>>($"{ResourceServerUrl(clientUuid)}/scope{Query(("name", name))}", cancellationToken);

    public Task<KeycloakBaseResponse<AuthorizationScopeResponseDto>> CreateScopeAsync(string clientUuid, CreateAuthorizationScopeRequestDto requestDto, CancellationToken cancellationToken = default)
        => PostAsync<AuthorizationScopeResponseDto>($"{ResourceServerUrl(clientUuid)}/scope", requestDto, cancellationToken);

    public Task<KeycloakBaseResponse> DeleteScopeAsync(string clientUuid, string scopeId, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, $"{ResourceServerUrl(clientUuid)}/scope/{Uri.EscapeDataString(scopeId)}", null, cancellationToken);

    // ── Resources ─────────────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<ResourceResponseDto>>> GetResourcesAsync(string clientUuid, GetResourcesQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var qs = Query(
            ("deep", "true"),
            ("name", query?.Name),
            ("exactName", Bool(query?.ExactName)),
            ("type", query?.Type),
            ("uri", query?.Uri),
            ("scope", query?.Scope),
            ("first", query?.First?.ToString()),
            ("max", query?.Max?.ToString()));
        return GetAsync<List<ResourceResponseDto>>($"{ResourceServerUrl(clientUuid)}/resource{qs}", cancellationToken);
    }

    public Task<KeycloakBaseResponse<ResourceResponseDto>> GetResourceAsync(string clientUuid, string resourceId, CancellationToken cancellationToken = default)
        => GetAsync<ResourceResponseDto>(ResourceUrl(clientUuid, resourceId), cancellationToken);

    public Task<KeycloakBaseResponse<ResourceResponseDto>> CreateResourceAsync(string clientUuid, CreateResourceRequestDto requestDto, CancellationToken cancellationToken = default)
        => PostAsync<ResourceResponseDto>($"{ResourceServerUrl(clientUuid)}/resource", requestDto, cancellationToken);

    public Task<KeycloakBaseResponse> UpdateResourceAsync(string clientUuid, string resourceId, UpdateResourceRequestDto requestDto, CancellationToken cancellationToken = default)
        => MergeAsync(ResourceUrl(clientUuid, resourceId), resource =>
        {
            if (requestDto.Name is not null) resource["name"] = requestDto.Name;
            if (requestDto.DisplayName is not null) resource["displayName"] = requestDto.DisplayName;
            if (requestDto.Type is not null) resource["type"] = requestDto.Type;
            if (requestDto.Uris is not null) resource["uris"] = JsonSerializer.SerializeToNode(requestDto.Uris);
            if (requestDto.Scopes is not null) resource["scopes"] = JsonSerializer.SerializeToNode(requestDto.Scopes.Select(s => new NamedReference(s)));
            if (requestDto.Attributes is not null) resource["attributes"] = JsonSerializer.SerializeToNode(requestDto.Attributes);
        }, cancellationToken);

    public Task<KeycloakBaseResponse> DeleteResourceAsync(string clientUuid, string resourceId, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, ResourceUrl(clientUuid, resourceId), null, cancellationToken);

    private string ResourceUrl(string clientUuid, string resourceId) => $"{ResourceServerUrl(clientUuid)}/resource/{Uri.EscapeDataString(resourceId)}";

    // ── Policies ──────────────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<PolicyResponseDto>>> GetPoliciesAsync(string clientUuid, GetPoliciesQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var qs = Query(
            ("permission", "false"),
            ("name", query?.Name),
            ("type", query?.Type),
            ("first", query?.First?.ToString()),
            ("max", query?.Max?.ToString()));
        return GetAsync<List<PolicyResponseDto>>($"{ResourceServerUrl(clientUuid)}/policy{qs}", cancellationToken);
    }

    public Task<KeycloakBaseResponse<PolicyResponseDto>> GetPolicyAsync(string clientUuid, string policyId, CancellationToken cancellationToken = default)
        => GetAsync<PolicyResponseDto>(PolicyUrl(clientUuid, policyId), cancellationToken);

    public Task<KeycloakBaseResponse<PolicyResponseDto>> CreateRolePolicyAsync(string clientUuid, CreateRolePolicyRequestDto requestDto, CancellationToken cancellationToken = default)
        => PostAsync<PolicyResponseDto>($"{ResourceServerUrl(clientUuid)}/policy/role", requestDto, cancellationToken);

    public Task<KeycloakBaseResponse<PolicyResponseDto>> CreateGroupPolicyAsync(string clientUuid, CreateGroupPolicyRequestDto requestDto, CancellationToken cancellationToken = default)
        => PostAsync<PolicyResponseDto>($"{ResourceServerUrl(clientUuid)}/policy/group", requestDto, cancellationToken);

    public Task<KeycloakBaseResponse<PolicyResponseDto>> CreateUserPolicyAsync(string clientUuid, CreateUserPolicyRequestDto requestDto, CancellationToken cancellationToken = default)
        => PostAsync<PolicyResponseDto>($"{ResourceServerUrl(clientUuid)}/policy/user", requestDto, cancellationToken);

    public Task<KeycloakBaseResponse> DeletePolicyAsync(string clientUuid, string policyId, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, PolicyUrl(clientUuid, policyId), null, cancellationToken);

    private string PolicyUrl(string clientUuid, string policyId) => $"{ResourceServerUrl(clientUuid)}/policy/{Uri.EscapeDataString(policyId)}";

    // ── Permissions ───────────────────────────────────────────────────────────

    public Task<KeycloakBaseResponse<List<PolicyResponseDto>>> GetPermissionsAsync(string clientUuid, GetPermissionsQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var qs = Query(
            ("name", query?.Name),
            ("resource", query?.Resource),
            ("scope", query?.Scope),
            ("first", query?.First?.ToString()),
            ("max", query?.Max?.ToString()));
        return GetAsync<List<PolicyResponseDto>>($"{ResourceServerUrl(clientUuid)}/permission{qs}", cancellationToken);
    }

    public Task<KeycloakBaseResponse<PolicyResponseDto>> CreateResourcePermissionAsync(string clientUuid, CreateResourcePermissionRequestDto requestDto, CancellationToken cancellationToken = default)
        => PostAsync<PolicyResponseDto>($"{ResourceServerUrl(clientUuid)}/permission/resource", requestDto, cancellationToken);

    public Task<KeycloakBaseResponse<PolicyResponseDto>> CreateScopePermissionAsync(string clientUuid, CreateScopePermissionRequestDto requestDto, CancellationToken cancellationToken = default)
        => PostAsync<PolicyResponseDto>($"{ResourceServerUrl(clientUuid)}/permission/scope", requestDto, cancellationToken);

    // Permissions are policies in Keycloak and are deleted through the policy endpoint.
    public Task<KeycloakBaseResponse> DeletePermissionAsync(string clientUuid, string permissionId, CancellationToken cancellationToken = default)
        => DeletePolicyAsync(clientUuid, permissionId, cancellationToken);

    // ── Evaluation ────────────────────────────────────────────────────────────

    public async Task<KeycloakBaseResponse<PolicyEvaluationResponseDto>> EvaluateAsync(
        string clientUuid, string userId, IReadOnlyCollection<string>? resourceIds = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        var body = new
        {
            userId,
            resources   = (resourceIds ?? []).Select(id => new Dictionary<string, string> { ["_id"] = id }).ToList(),
            entitlements = false,
            context      = new { attributes = new Dictionary<string, string>() }
        };
        return await PostAsync<PolicyEvaluationResponseDto>($"{ResourceServerUrl(clientUuid)}/policy/evaluate", body, cancellationToken);
    }

    public async Task<KeycloakBaseResponse<PermissionDecisionDto>> CheckPermissionAsync(
        string userAccessToken, string audience, PermissionRequest permission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permission);
        var response = await SendUmaAsync(userAccessToken, audience, [permission], "decision", cancellationToken);

        if (await IsAccessDeniedAsync(response, cancellationToken))
            return new KeycloakBaseResponse<PermissionDecisionDto>(new PermissionDecisionDto { Granted = false }, true, response.StatusCode);
        if (!response.IsSuccessStatusCode)
            return await response.HandleResponseAsync<PermissionDecisionDto>();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var granted = document.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.True;
        return new KeycloakBaseResponse<PermissionDecisionDto>(new PermissionDecisionDto { Granted = granted }, true, response.StatusCode);
    }

    public async Task<KeycloakBaseResponse<List<GrantedPermissionDto>>> GetGrantedPermissionsAsync(
        string userAccessToken, string audience, IReadOnlyCollection<PermissionRequest>? permissions = null, CancellationToken cancellationToken = default)
    {
        var response = await SendUmaAsync(userAccessToken, audience, permissions, "permissions", cancellationToken);

        if (await IsAccessDeniedAsync(response, cancellationToken))
            return new KeycloakBaseResponse<List<GrantedPermissionDto>>([], true, response.StatusCode);
        return await response.HandleResponseAsync<List<GrantedPermissionDto>>();
    }

    public async Task<KeycloakBaseResponse<SigninResponseDto>> GetRequestingPartyTokenAsync(
        string userAccessToken, string audience, IReadOnlyCollection<PermissionRequest>? permissions = null, CancellationToken cancellationToken = default)
    {
        var response = await SendUmaAsync(userAccessToken, audience, permissions, responseMode: null, cancellationToken);
        return await response.HandleResponseAsync<SigninResponseDto>();
    }

    private async Task<HttpResponseMessage> SendUmaAsync(
        string userAccessToken, string audience, IReadOnlyCollection<PermissionRequest>? permissions, string? responseMode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(userAccessToken);
        ArgumentException.ThrowIfNullOrEmpty(audience);

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", UmaTicketGrant),
            new("audience", audience)
        };
        foreach (var permission in permissions ?? [])
            form.Add(new("permission", permission.ToFormValue()));
        if (responseMode is not null)
            form.Add(new("response_mode", responseMode));

        var request = new HttpRequestMessage(HttpMethod.Post, $"realms/{Realm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken);
        return await _tokenClient.SendAsync(request, cancellationToken);
    }

    // Keycloak answers a denied UMA request with 403 { "error": "access_denied" }.
    private static async Task<bool> IsAccessDeniedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.Forbidden) return false;
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("error", out var error) && error.GetString() == "access_denied";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    private async Task<KeycloakBaseResponse<T>> GetAsync<T>(string url, CancellationToken cancellationToken) where T : class, new()
    {
        var response = await _adminClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        return await response.HandleResponseAsync<T>();
    }

    private async Task<KeycloakBaseResponse<T>> PostAsync<T>(string url, object body, CancellationToken cancellationToken) where T : class, new()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = Json(body) };
        var response = await _adminClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync<T>();
    }

    private async Task<KeycloakBaseResponse> SendAsync(HttpMethod method, string url, HttpContent? content, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        var response = await _adminClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    // Keycloak's PUT endpoints replace the whole representation: fetch, change, send back.
    private async Task<KeycloakBaseResponse> MergeAsync(string url, Action<JsonObject> apply, CancellationToken cancellationToken)
    {
        var existing = await _adminClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        if (!existing.IsSuccessStatusCode)
            return await existing.HandleResponseAsync();

        var representation = JsonNode.Parse(await existing.Content.ReadAsStringAsync(cancellationToken))!.AsObject();
        apply(representation);

        var content = new StringContent(representation.ToJsonString(), Encoding.UTF8, "application/json");
        return await SendAsync(HttpMethod.Put, url, content, cancellationToken);
    }

    private static StringContent Json(object value) =>
        new(JsonSerializer.Serialize(value, value.GetType()), Encoding.UTF8, "application/json");

    private static string? Bool(bool? value) => value?.ToString().ToLowerInvariant();

    private static string Query(params (string Name, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => p.Value is not null)
            .Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return parts.Count > 0 ? "?" + string.Join('&', parts) : string.Empty;
    }
}
