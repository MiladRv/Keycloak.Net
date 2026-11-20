using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Roles.Contracts;

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
    /// <summary>Returned by Keycloak only for single-role lookups (<c>GetClientRoleAsync</c>); null in list responses.</summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; set; }
};