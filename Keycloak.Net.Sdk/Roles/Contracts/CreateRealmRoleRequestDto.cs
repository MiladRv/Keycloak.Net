using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Roles.Contracts;

public sealed record CreateRealmRoleRequestDto
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<string>>? Attributes { get; set; }
}
