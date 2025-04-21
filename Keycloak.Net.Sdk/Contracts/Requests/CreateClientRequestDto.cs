namespace Keycloak.Net.Sdk.Contracts.Requests;

public record CreateClientRequestDto()
{
    public string ClientId { get; set; }
    public string Name { get; set; }
    public bool Enabled { get; set; } = true;
    public bool PublicClient { get; set; } = false;
    public bool ServiceAccountsEnabled { get; set; } = false;
    public string Protocol { get; set; } = "openid-connect";
    public List<string> RedirectUris { get; set; } = new();
};