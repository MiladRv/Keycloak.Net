using System.Text.Json.Serialization;

namespace Keycloak.Net.Sdk.Clients.Contracts;

public sealed record ClientScopeAttributes
{
    [JsonPropertyName("include.in.token.scope")]
    public string Includeintokenscope { get; set; }

    [JsonPropertyName("consent.screen.text")]
    public string Consentscreentext { get; set; }

    [JsonPropertyName("display.on.consent.screen")]
    public string Displayonconsentscreen { get; set; }

    [JsonPropertyName("gui.order")]
    public string Guiorder { get; set; }
}