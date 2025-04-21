namespace Keycloak.Net.Sdk.Clients.Contracts;

public sealed record ClientResponseDto()
{
    public string Id { get; set; }
    public string ClientId { get; set; }
    public string Name { get; set; }
    public bool? Enabled { get; set; }
    public bool? PublicClient { get; set; }
    public bool? ServiceAccountsEnabled { get; set; }
};