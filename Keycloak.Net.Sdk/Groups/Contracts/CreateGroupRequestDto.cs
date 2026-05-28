using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Groups.Contracts;

public class CreateGroupRequestDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
