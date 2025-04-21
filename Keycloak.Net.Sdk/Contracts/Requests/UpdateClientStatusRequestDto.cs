namespace Keycloak.Net.Sdk.Contracts.Requests;

public sealed record UpdateClientStatusRequestDto()
{
    public bool ServiceAccountsEnabled { get; set; }
}