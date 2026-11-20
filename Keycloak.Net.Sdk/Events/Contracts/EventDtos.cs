using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Events.Contracts;

/// <summary>A user event such as <c>LOGIN</c>, <c>LOGIN_ERROR</c> or <c>UPDATE_PASSWORD</c>.</summary>
public sealed record EventResponseDto
{
    /// <summary>Unix time in milliseconds.</summary>
    [JsonPropertyName("time")] public long Time { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("realmId")] public string? RealmId { get; set; }
    [JsonPropertyName("clientId")] public string? ClientId { get; set; }
    [JsonPropertyName("userId")] public string? UserId { get; set; }
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
    [JsonPropertyName("ipAddress")] public string? IpAddress { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("details")] public Dictionary<string, string>? Details { get; set; }

    [JsonIgnore] public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(Time);
}

public sealed record GetEventsQueryDto
{
    /// <summary>Event types, for example <c>LOGIN</c> and <c>LOGIN_ERROR</c>.</summary>
    public IReadOnlyCollection<string>? Types { get; init; }
    /// <summary>The client's <c>clientId</c>.</summary>
    public string? Client { get; init; }
    public string? User { get; init; }
    public string? IpAddress { get; init; }
    /// <summary>Only the date part is used by Keycloak (yyyy-MM-dd).</summary>
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public int? First { get; init; }
    public int? Max { get; init; }
}

public sealed record AdminEventAuthDetailsDto
{
    [JsonPropertyName("realmId")] public string? RealmId { get; set; }
    [JsonPropertyName("clientId")] public string? ClientId { get; set; }
    [JsonPropertyName("userId")] public string? UserId { get; set; }
    [JsonPropertyName("ipAddress")] public string? IpAddress { get; set; }
}

/// <summary>A change made through the admin API or console, for example <c>CREATE</c> on a <c>GROUP</c>.</summary>
public sealed record AdminEventResponseDto
{
    /// <summary>Unix time in milliseconds.</summary>
    [JsonPropertyName("time")] public long Time { get; set; }
    [JsonPropertyName("realmId")] public string? RealmId { get; set; }
    /// <summary>Who made the change.</summary>
    [JsonPropertyName("authDetails")] public AdminEventAuthDetailsDto? AuthDetails { get; set; }
    /// <summary><c>CREATE</c>, <c>UPDATE</c>, <c>DELETE</c> or <c>ACTION</c>.</summary>
    [JsonPropertyName("operationType")] public string OperationType { get; set; } = string.Empty;
    /// <summary>For example <c>USER</c>, <c>GROUP</c>, <c>REALM_ROLE_MAPPING</c>.</summary>
    [JsonPropertyName("resourceType")] public string? ResourceType { get; set; }
    /// <summary>For example <c>users/{id}</c>.</summary>
    [JsonPropertyName("resourcePath")] public string? ResourcePath { get; set; }
    /// <summary>The JSON that was sent; only when admin event details are enabled.</summary>
    [JsonPropertyName("representation")] public string? Representation { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }

    [JsonIgnore] public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(Time);
}

public sealed record GetAdminEventsQueryDto
{
    /// <summary><c>CREATE</c>, <c>UPDATE</c>, <c>DELETE</c>, <c>ACTION</c>.</summary>
    public IReadOnlyCollection<string>? OperationTypes { get; init; }
    /// <summary>For example <c>USER</c>, <c>GROUP</c>.</summary>
    public IReadOnlyCollection<string>? ResourceTypes { get; init; }
    /// <summary>Supports <c>*</c> wildcards, for example <c>users/*</c>.</summary>
    public string? ResourcePath { get; init; }
    public string? AuthRealm { get; init; }
    public string? AuthClient { get; init; }
    public string? AuthUser { get; init; }
    public string? AuthIpAddress { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public int? First { get; init; }
    public int? Max { get; init; }
}

public sealed record EventsConfigResponseDto
{
    [JsonPropertyName("eventsEnabled")] public bool EventsEnabled { get; set; }
    /// <summary>Seconds after which user events are deleted; null keeps them.</summary>
    [JsonPropertyName("eventsExpiration")] public long? EventsExpiration { get; set; }
    [JsonPropertyName("eventsListeners")] public List<string>? EventsListeners { get; set; }
    [JsonPropertyName("enabledEventTypes")] public List<string>? EnabledEventTypes { get; set; }
    [JsonPropertyName("adminEventsEnabled")] public bool AdminEventsEnabled { get; set; }
    [JsonPropertyName("adminEventsDetailsEnabled")] public bool AdminEventsDetailsEnabled { get; set; }
}

/// <summary>Fields left null are not changed.</summary>
public sealed record UpdateEventsConfigRequestDto
{
    /// <summary>Store user events (login, logout, errors, ...).</summary>
    public bool? EventsEnabled { get; init; }
    /// <summary>Seconds after which stored user events are deleted.</summary>
    public long? EventsExpiration { get; init; }
    /// <summary>Event listener ids, for example <c>jboss-logging</c>. Replaces the list.</summary>
    public List<string>? EventsListeners { get; init; }
    /// <summary>Event types to store. Replaces the list; an empty list stores the default types.</summary>
    public List<string>? EnabledEventTypes { get; init; }
    /// <summary>Store admin events.</summary>
    public bool? AdminEventsEnabled { get; init; }
    /// <summary>Include the request body in admin events.</summary>
    public bool? AdminEventsDetailsEnabled { get; init; }
}
