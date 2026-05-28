using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Users.Contracts;

public sealed record SetUserPasswordRequestDto()
{
    [JsonPropertyName("type")]
    public string Type => "password";

    [JsonPropertyName("value")]
    public string Value { get; init; }

    [JsonPropertyName("temporary")]
    public bool Temporary { get; init; }
}
