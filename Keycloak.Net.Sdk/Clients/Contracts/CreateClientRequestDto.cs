using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Clients.Contracts;

public record CreateClientRequestDto()
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("publicClient")]
    public bool PublicClient { get; set; } = false;

    [JsonPropertyName("serviceAccountsEnabled")]
    public bool ServiceAccountsEnabled { get; set; } = false;

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "openid-connect";

    [JsonPropertyName("redirectUris")]
    public List<string> RedirectUris { get; set; } = new();
}
