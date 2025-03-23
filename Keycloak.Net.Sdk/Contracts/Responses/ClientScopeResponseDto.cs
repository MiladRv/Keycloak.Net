using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Contracts.Responses;

public sealed record ClientScopeResponseDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; }
    [JsonPropertyName("name")]
    public string Name { get; init; }
    [JsonPropertyName("description")]
    public string Description { get;init; }
    [JsonPropertyName("protocol")]
    public string Protocol { get; init; }
    [JsonPropertyName("attributes")]
    public ClientScopeAttributes Attributes { get; init; }
    // public List<ProtocolMapper> protocolMappers { get; set; }
}