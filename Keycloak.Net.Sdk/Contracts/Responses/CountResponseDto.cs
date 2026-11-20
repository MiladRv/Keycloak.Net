namespace Keycloak.Net.Sdk.Contracts.Responses;

/// <summary>A count returned by Keycloak, for example the number of users matching a query.</summary>
public sealed record CountResponseDto
{
    public long Count { get; init; }
}
