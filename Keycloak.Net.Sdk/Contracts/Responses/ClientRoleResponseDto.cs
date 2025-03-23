using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Contracts.Responses;

public sealed record ClientRoleResponseDto()
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("composite")]
    public bool Composite { get; set; }
    [JsonPropertyName("clientRole")]
    public bool ClientRole { get; set; }
    [JsonPropertyName("containerId")]
    public string ContainerId { get; set; }
};