using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Clients.Contracts;

public sealed record UpdateProtocolMapperRequestDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "openid-connect";

    [JsonPropertyName("protocolMapper")]
    public string ProtocolMapper { get; set; }

    [JsonPropertyName("consentRequired")]
    public bool ConsentRequired { get; set; }

    [JsonPropertyName("config")]
    public Dictionary<string, string> Config { get; set; }
}
