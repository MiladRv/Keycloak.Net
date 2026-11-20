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

    /// <summary>Id of the parent group; null for top-level groups or on Keycloak versions before 23.</summary>
    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    /// <summary>Number of direct child groups; reported by Keycloak 23 and later.</summary>
    [JsonPropertyName("subGroupCount")]
    public long? SubGroupCount { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; set; }
}
