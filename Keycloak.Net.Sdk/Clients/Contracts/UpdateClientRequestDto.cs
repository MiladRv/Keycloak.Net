using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Clients.Contracts;

public sealed record UpdateClientRequestDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("publicClient")]
    public bool? PublicClient { get; set; }

    [JsonPropertyName("serviceAccountsEnabled")]
    public bool? ServiceAccountsEnabled { get; set; }
}
