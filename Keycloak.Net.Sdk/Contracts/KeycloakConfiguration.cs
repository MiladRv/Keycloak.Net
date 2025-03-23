namespace Keycloak.Net.Sdk.Contracts;

public record KeycloakConfiguration
{
    public string ServerUrl { get; set; }
    public string RealmName { get; set; }
    public string ClientUuid { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string AdminUsername { get; set; }
    public string AdminPassword { get; set; }
}