using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Contracts.Requests;

public sealed record SignupRequestDto
{
    [JsonPropertyName("username")]
    public string Username { get; init; }
    [JsonPropertyName("email")]
    public string Email { get; init; }
    [JsonPropertyName("firstName")]
    public string Firstname { get; init; }
    [JsonPropertyName("lastName")]
    public string Lastname { get; init; }
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;
    [JsonPropertyName("credentials")]
    public CredentialDto[] Credentials { get; init; }

    public SignupRequestDto(string Username , string password)
    {
        this.Username = Username;
        Credentials = [
            new CredentialDto
            {
                Type = "password",
                Value = password,
                Temporary = false // Set to true if you want the user to change the password on first login
            }
        ];
    }
    
    public sealed record CredentialDto
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }
        [JsonPropertyName("value")]
        public required string Value { get; init; }
        [JsonPropertyName("temporary")]
        public bool Temporary { get; init; }
    }
}