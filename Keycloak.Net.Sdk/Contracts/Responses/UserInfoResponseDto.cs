using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Contracts.Responses;

public sealed record UserInfoResponseDto() 
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; }
    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }
    [JsonPropertyName("createdTimestamp")]
    public long CreatedTimestamp { get; set; }
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
    [JsonPropertyName("totp")]
    public bool Totp { get; set; }
    public List<object> disableableCredentialTypes { get; set; }
    public List<object> requiredActions { get; set; }
    [JsonPropertyName("notBefore")]
    public int NotBefore { get; set; }
}