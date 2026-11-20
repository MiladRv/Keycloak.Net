using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Events.Contracts;
using Keycloak.Net.Sdk.Extensions;
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Events;

public sealed class EventManagement(IHttpClientFactory httpClientFactory, IOptions<KeycloakConfiguration> keyCloakConfiguration)
    : IEventManagement
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("keycloak");

    private string RealmUrl => $"admin/realms/{keyCloakConfiguration.Value.RealmName}";

    public async Task<KeycloakBaseResponse<EventsConfigResponseDto>> GetEventsConfigAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{RealmUrl}/events/config"), cancellationToken);
        return await response.HandleResponseAsync<EventsConfigResponseDto>();
    }

    public async Task<KeycloakBaseResponse> UpdateEventsConfigAsync(UpdateEventsConfigRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestDto);
        var url = $"{RealmUrl}/events/config";

        // The PUT replaces the whole configuration: read it and change only what was given.
        var existing = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        if (!existing.IsSuccessStatusCode)
            return await existing.HandleResponseAsync();

        var config = JsonNode.Parse(await existing.Content.ReadAsStringAsync(cancellationToken))!.AsObject();
        if (requestDto.EventsEnabled.HasValue) config["eventsEnabled"] = requestDto.EventsEnabled.Value;
        if (requestDto.EventsExpiration.HasValue) config["eventsExpiration"] = requestDto.EventsExpiration.Value;
        if (requestDto.EventsListeners is not null) config["eventsListeners"] = JsonSerializer.SerializeToNode(requestDto.EventsListeners);
        if (requestDto.EnabledEventTypes is not null) config["enabledEventTypes"] = JsonSerializer.SerializeToNode(requestDto.EnabledEventTypes);
        if (requestDto.AdminEventsEnabled.HasValue) config["adminEventsEnabled"] = requestDto.AdminEventsEnabled.Value;
        if (requestDto.AdminEventsDetailsEnabled.HasValue) config["adminEventsDetailsEnabled"] = requestDto.AdminEventsDetailsEnabled.Value;

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(config.ToJsonString(), Encoding.UTF8, "application/json")
        };
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse<List<EventResponseDto>>> GetEventsAsync(GetEventsQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var parameters = new List<(string, string?)>();
        foreach (var type in query?.Types ?? [])
            parameters.Add(("type", type));
        parameters.Add(("client", query?.Client));
        parameters.Add(("user", query?.User));
        parameters.Add(("ipAddress", query?.IpAddress));
        parameters.Add(("dateFrom", Date(query?.DateFrom)));
        parameters.Add(("dateTo", Date(query?.DateTo)));
        parameters.Add(("first", query?.First?.ToString(CultureInfo.InvariantCulture)));
        parameters.Add(("max", query?.Max?.ToString(CultureInfo.InvariantCulture)));

        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{RealmUrl}/events{Query(parameters)}"), cancellationToken);
        return await response.HandleResponseAsync<List<EventResponseDto>>();
    }

    public async Task<KeycloakBaseResponse<List<AdminEventResponseDto>>> GetAdminEventsAsync(GetAdminEventsQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        var parameters = new List<(string, string?)>();
        foreach (var operation in query?.OperationTypes ?? [])
            parameters.Add(("operationTypes", operation));
        foreach (var resourceType in query?.ResourceTypes ?? [])
            parameters.Add(("resourceTypes", resourceType));
        parameters.Add(("resourcePath", query?.ResourcePath));
        parameters.Add(("authRealm", query?.AuthRealm));
        parameters.Add(("authClient", query?.AuthClient));
        parameters.Add(("authUser", query?.AuthUser));
        parameters.Add(("authIpAddress", query?.AuthIpAddress));
        parameters.Add(("dateFrom", Date(query?.DateFrom)));
        parameters.Add(("dateTo", Date(query?.DateTo)));
        parameters.Add(("first", query?.First?.ToString(CultureInfo.InvariantCulture)));
        parameters.Add(("max", query?.Max?.ToString(CultureInfo.InvariantCulture)));

        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{RealmUrl}/admin-events{Query(parameters)}"), cancellationToken);
        return await response.HandleResponseAsync<List<AdminEventResponseDto>>();
    }

    public async Task<KeycloakBaseResponse> ClearEventsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{RealmUrl}/events"), cancellationToken);
        return await response.HandleResponseAsync();
    }

    public async Task<KeycloakBaseResponse> ClearAdminEventsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{RealmUrl}/admin-events"), cancellationToken);
        return await response.HandleResponseAsync();
    }

    private static string? Date(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Query(IEnumerable<(string Name, string? Value)> parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return parts.Count > 0 ? "?" + string.Join('&', parts) : string.Empty;
    }
}
