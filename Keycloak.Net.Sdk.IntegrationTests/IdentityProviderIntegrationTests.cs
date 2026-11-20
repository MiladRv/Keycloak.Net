using Keycloak.Net.Sdk.IdentityProviders.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>A corporate OIDC identity provider: configuration, mappers, and linking a user to their external account.</summary>
[Collection(nameof(KeycloakCollection))]
public class IdentityProviderIntegrationTests(KeycloakFixture fixture)
{
    private IIdentityProviderManagement Idp =>
        fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IIdentityProviderManagement>();

    private static CreateIdentityProviderRequestDto CorporateOidc(string alias) => new()
    {
        Alias       = alias,
        ProviderId  = "oidc",
        DisplayName = "Corporate login",
        TrustEmail  = true,
        Config      = new()
        {
            ["authorizationUrl"] = "https://login.corp.example/authorize",
            ["tokenUrl"]         = "https://login.corp.example/token",
            ["clientId"]         = "keycloak-broker",
            ["clientSecret"]     = "broker-secret",
            ["clientAuthMethod"] = "client_secret_post",
            ["defaultScope"]     = "openid email"
        }
    };

    [Fact]
    public async Task IdentityProvider_CanBeCreatedReadUpdatedAndDeleted()
    {
        var alias = $"corp-{Guid.NewGuid():N}";
        var created = await Idp.CreateIdentityProviderAsync(CorporateOidc(alias));
        Assert.True(created.IsSuccessful, created.ErrorMessage);
        try
        {
            var provider = await Idp.GetIdentityProviderAsync(alias);
            Assert.True(provider.IsSuccessful, provider.ErrorMessage);
            Assert.Equal("oidc", provider.Response.ProviderId);
            Assert.Equal("Corporate login", provider.Response.DisplayName);
            Assert.True(provider.Response.Enabled);
            Assert.True(provider.Response.TrustEmail);
            Assert.Equal("https://login.corp.example/token", provider.Response.Config!["tokenUrl"]);
            Assert.NotEqual("broker-secret", provider.Response.Config["clientSecret"]); // masked

            Assert.Contains((await Idp.GetIdentityProvidersAsync()).Response, p => p.Alias == alias);

            var update = await Idp.UpdateIdentityProviderAsync(alias, new UpdateIdentityProviderRequestDto
            {
                DisplayName = "Corporate SSO",
                HideOnLogin = true,
                Config      = new() { ["defaultScope"] = "openid email profile" }
            });
            Assert.True(update.IsSuccessful, update.ErrorMessage);

            var updated = (await Idp.GetIdentityProviderAsync(alias)).Response;
            Assert.Equal("Corporate SSO", updated.DisplayName);
            Assert.True(updated.HideOnLogin);
            Assert.True(updated.TrustEmail);
            Assert.Equal("openid email profile", updated.Config!["defaultScope"]);
            Assert.Equal("https://login.corp.example/authorize", updated.Config["authorizationUrl"]);
        }
        finally
        {
            Assert.True((await Idp.DeleteIdentityProviderAsync(alias)).IsSuccessful);
        }

        Assert.False((await Idp.GetIdentityProviderAsync(alias)).IsSuccessful);
    }

    [Fact]
    public async Task Mappers_AndUserLinks_CanBeManaged()
    {
        var alias = $"corp-{Guid.NewGuid():N}";
        Assert.True((await Idp.CreateIdentityProviderAsync(CorporateOidc(alias))).IsSuccessful);
        try
        {
            var mapper = await Idp.CreateMapperAsync(alias, new CreateIdentityProviderMapperRequestDto
            {
                Name                   = "company",
                IdentityProviderMapper = "hardcoded-attribute-idp-mapper",
                Config                 = new() { ["syncMode"] = "INHERIT", ["attribute"] = "company_id", ["attribute.value"] = "1" }
            });
            Assert.True(mapper.IsSuccessful, mapper.ErrorMessage);

            var mappers = await Idp.GetMappersAsync(alias);
            var company = Assert.Single(mappers.Response);
            Assert.Equal("hardcoded-attribute-idp-mapper", company.IdentityProviderMapper);
            Assert.Equal("company_id", company.Config!["attribute"]);

            Assert.True((await Idp.DeleteMapperAsync(alias, company.Id)).IsSuccessful);
            Assert.Empty((await Idp.GetMappersAsync(alias)).Response);

            // Link the test user to an external account
            var link = await Idp.LinkUserAsync(fixture.TestUserId, alias, "ext-123", "sdk.tester@corp.example");
            Assert.True(link.IsSuccessful, link.ErrorMessage);

            var identities = await Idp.GetUserFederatedIdentitiesAsync(fixture.TestUserId);
            var identity = Assert.Single(identities.Response, i => i.IdentityProvider == alias);
            Assert.Equal("ext-123", identity.UserId);
            Assert.Equal("sdk.tester@corp.example", identity.UserName);

            Assert.True((await Idp.UnlinkUserAsync(fixture.TestUserId, alias)).IsSuccessful);
            Assert.DoesNotContain((await Idp.GetUserFederatedIdentitiesAsync(fixture.TestUserId)).Response, i => i.IdentityProvider == alias);
        }
        finally
        {
            await Idp.UnlinkUserAsync(fixture.TestUserId, alias);
            await Idp.DeleteIdentityProviderAsync(alias);
        }
    }

    [Fact]
    public async Task CreateIdentityProviderAsync_DuplicateAlias_ReturnsConflict()
    {
        var alias = $"corp-{Guid.NewGuid():N}";
        Assert.True((await Idp.CreateIdentityProviderAsync(CorporateOidc(alias))).IsSuccessful);
        try
        {
            var duplicate = await Idp.CreateIdentityProviderAsync(CorporateOidc(alias));

            Assert.False(duplicate.IsSuccessful);
            Assert.Equal(System.Net.HttpStatusCode.Conflict, duplicate.StatusCode);
        }
        finally
        {
            await Idp.DeleteIdentityProviderAsync(alias);
        }
    }
}
