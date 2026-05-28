using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Clients.Contracts;

public sealed record UpdateClientStatusRequestDto()
{
    [JsonPropertyName("serviceAccountsEnabled")]
    public bool ServiceAccountsEnabled { get; set; }
}