using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Athentications.Contracts;

public sealed record SigninResponseDto
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; } = null!;
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    [JsonPropertyName("refresh_expires_in")]
    public int RefreshExpiresIn { get; init; }
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; }= null!;
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; }= null!;
    [JsonPropertyName("not-before-policy")]
    public int NotBeforePolicy { get; init; }
    [JsonPropertyName("session_state")]
    public string SessionState { get; init; }= null!;
    [JsonPropertyName("scope")]
    public string Scope { get; init; }= null!;
}