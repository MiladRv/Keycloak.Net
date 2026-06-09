namespace Keycloak.Net.Sdk.Configurations;

public sealed class KeycloakConfiguration
{
    public string ServerUrl { get; set; } = string.Empty;
    public string RealmName { get; set; } = string.Empty;
    public string ClientUuid { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public ushort DelayBetweenRetryRequestsInSeconds { get; set; } = 2;
    public ushort NumberOfRetries { get; set; } = 3;
}