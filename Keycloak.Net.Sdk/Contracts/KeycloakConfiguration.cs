namespace Keycloak.Net.Sdk.Contracts;

public sealed record KeycloakConfiguration
{
    public string ServerUrl { get; init; }
    public string RealmName { get; init; }
    public string ClientUuid { get; init; }
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string AdminUsername { get; init; }
    public string AdminPassword { get; init; }
    public ushort DelayBetweenRetryRequestsInSeconds { get; init; } = 2;
    public ushort NumberOfRetries { get; init; } = 3;
}