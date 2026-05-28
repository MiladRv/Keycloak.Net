using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Groups.Contracts;

public sealed record GroupResponseDto()
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("subGroups")]
    public List<GroupResponseDto> SubGroups { get; set; } = [];
}
