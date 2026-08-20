using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Roles.Contracts;

public sealed record CompositeRoleRequestDto
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
