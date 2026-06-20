using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Clients.Contracts;

public sealed record CreateClientScopeRequestDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "openid-connect";

    [JsonPropertyName("attributes")]
    public ClientScopeAttributes Attributes { get; set; }
}
