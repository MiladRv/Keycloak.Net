using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Groups.Contracts;

/// <summary>
/// Fields left null are kept as they are. When <see cref="Attributes"/> is set it replaces all
/// of the group's attributes; pass an empty dictionary to clear them.
/// </summary>
public sealed record UpdateGroupRequestDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; set; }
}
