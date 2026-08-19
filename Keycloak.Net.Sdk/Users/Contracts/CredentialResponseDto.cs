using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Users.Contracts;

public sealed record CredentialResponseDto()
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("userLabel")]
    public string? UserLabel { get; set; }
    [JsonPropertyName("createdDate")]
    public long CreatedDate { get; set; }
}
