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
    public object[] Credentials;

    public SignupRequestDto(string Username , string password)
    {
        this.Username = Username;
        Credentials = [
            new
            {
                type = "password",
                value = password,
                temporary = false // Set to true if you want the user to change the password on first login
            }
        ];
    }
}