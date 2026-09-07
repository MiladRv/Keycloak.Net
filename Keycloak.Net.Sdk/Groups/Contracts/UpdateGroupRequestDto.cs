using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Groups.Contracts;

public sealed record UpdateGroupRequestDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
