using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Roles.Contracts;

/// <summary>
/// Replaces a realm role's name and description.
/// Attributes are replaced only when <see cref="Attributes"/> is not null; pass an empty dictionary to clear them.
/// </summary>
public sealed record UpdateRealmRoleRequestDto
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<string>>? Attributes { get; set; }
}
