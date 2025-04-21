namespace Keycloak.Net.Sdk.Clients.Contracts;

public sealed record UpdateClientStatusRequestDto()
{
    public bool ServiceAccountsEnabled { get; set; }
}