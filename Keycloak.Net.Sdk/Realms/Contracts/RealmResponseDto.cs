using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Realms.Contracts;

public sealed record RealmResponseDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("realm")]
    public string Realm { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("sslRequired")]
    public string? SslRequired { get; set; }

    [JsonPropertyName("registrationAllowed")]
    public bool? RegistrationAllowed { get; set; }

    [JsonPropertyName("loginWithEmailAllowed")]
    public bool? LoginWithEmailAllowed { get; set; }

    [JsonPropertyName("resetPasswordAllowed")]
    public bool? ResetPasswordAllowed { get; set; }

    [JsonPropertyName("editUsernameAllowed")]
    public bool? EditUsernameAllowed { get; set; }

    [JsonPropertyName("verifyEmail")]
    public bool? VerifyEmail { get; set; }

    [JsonPropertyName("rememberMe")]
    public bool? RememberMe { get; set; }

    [JsonPropertyName("bruteForceProtected")]
    public bool? BruteForceProtected { get; set; }

    [JsonPropertyName("accessTokenLifespan")]
    public int? AccessTokenLifespan { get; set; }
}
