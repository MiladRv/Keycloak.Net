using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Clients.Contracts;

public sealed record ClientResponseDto()
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
    [JsonPropertyName("publicClient")]
    public bool? PublicClient { get; set; }
    [JsonPropertyName("serviceAccountsEnabled")]
    public bool? ServiceAccountsEnabled { get; set; }
}
