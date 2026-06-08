using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Users.Contracts;

public sealed record UpdateUserRequestDto
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }
}
