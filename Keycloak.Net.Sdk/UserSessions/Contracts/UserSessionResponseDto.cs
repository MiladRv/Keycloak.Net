using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.UserSessions.Contracts;

public sealed record UserSessionResponseDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("username")]
    public string Username { get; init; } = null!;

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = null!;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; init; } = null!;

    [JsonPropertyName("start")]
    public long Start { get; init; }

    [JsonPropertyName("lastAccess")]
    public long LastAccess { get; init; }

    [JsonPropertyName("rememberMe")]
    public bool RememberMe { get; init; }

    [JsonPropertyName("clients")]
    public Dictionary<string, string> Clients { get; init; } = new();
}
