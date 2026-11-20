using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.IdentityProviders;
using Keycloak.Net.Sdk.IdentityProviders.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Keycloak.Net.Sdk.UserFederation;
using Keycloak.Net.Sdk.UserFederation.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class IdentityProviderAndFederationTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private static string Realm => $"/admin/realms/{TestData.RealmName}";

    private (IdentityProviderManagement Sut, FakeHttpMessageHandler Handler) CreateIdp()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new IdentityProviderManagement(factory, _options), handler);
    }

    private (UserFederationManagement Sut, FakeHttpMessageHandler Handler) CreateFederation()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new UserFederationManagement(factory, _options), handler);
    }

    private static async Task<JsonElement> BodyAsync(HttpRequestMessage request)
    {
        using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    // ── Identity providers ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateIdentityProviderAsync_PostsRepresentation()
    {
        var (sut, handler) = CreateIdp();
        handler.AddResponse(HttpStatusCode.Created);

        var result = await sut.CreateIdentityProviderAsync(new CreateIdentityProviderRequestDto
        {
            Alias      = "corp",
            ProviderId = "oidc",
            TrustEmail = true,
            Config     = new() { ["clientId"] = "broker" }
        });

        Assert.True(result.IsSuccessful);
        Assert.Equal($"{Realm}/identity-provider/instances", handler.SentRequests[0].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[0]);
        Assert.Equal("corp", body.GetProperty("alias").GetString());
        Assert.True(body.GetProperty("enabled").GetBoolean());
        Assert.True(body.GetProperty("trustEmail").GetBoolean());
        Assert.Equal("broker", body.GetProperty("config").GetProperty("clientId").GetString());
        Assert.False(body.TryGetProperty("displayName", out _));
    }

    [Fact]
    public async Task GetIdentityProviderAsync_EscapesAliasAndParses()
    {
        var (sut, handler) = CreateIdp();
        handler.AddResponse(HttpStatusCode.OK, """{ "alias": "a b", "providerId": "saml", "enabled": true, "hideOnLogin": true, "config": { "entityId": "x" } }""");

        var result = await sut.GetIdentityProviderAsync("a b");

        Assert.Equal("saml", result.Response.ProviderId);
        Assert.True(result.Response.HideOnLogin);
        Assert.EndsWith("/identity-provider/instances/a%20b", handler.SentRequests[0].RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task UpdateIdentityProviderAsync_MergesFieldsAndConfig()
    {
        var (sut, handler) = CreateIdp();
        handler.AddResponse(HttpStatusCode.OK, """
            { "alias": "corp", "providerId": "oidc", "displayName": "Old", "enabled": true, "trustEmail": true,
              "config": { "clientId": "broker", "clientSecret": "**********", "defaultScope": "openid" } }
            """);
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateIdentityProviderAsync("corp", new UpdateIdentityProviderRequestDto
        {
            DisplayName = "New",
            Enabled     = false,
            Config      = new() { ["defaultScope"] = "openid email" }
        });

        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        var body = await BodyAsync(handler.SentRequests[1]);
        Assert.Equal("New", body.GetProperty("displayName").GetString());
        Assert.False(body.GetProperty("enabled").GetBoolean());
        Assert.True(body.GetProperty("trustEmail").GetBoolean());
        var config = body.GetProperty("config");
        Assert.Equal("openid email", config.GetProperty("defaultScope").GetString());
        Assert.Equal("broker", config.GetProperty("clientId").GetString());
        Assert.Equal("**********", config.GetProperty("clientSecret").GetString()); // masked value tells Keycloak to keep the secret
    }

    [Fact]
    public async Task UpdateIdentityProviderAsync_NotFound_DoesNotPut()
    {
        var (sut, handler) = CreateIdp();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.UpdateIdentityProviderAsync("missing", new UpdateIdentityProviderRequestDto { Enabled = false });

        Assert.False(result.IsSuccessful);
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task CreateMapperAsync_IncludesAlias()
    {
        var (sut, handler) = CreateIdp();
        handler.AddResponse(HttpStatusCode.Created);

        await sut.CreateMapperAsync("corp", new CreateIdentityProviderMapperRequestDto
        {
            Name = "company", IdentityProviderMapper = "hardcoded-attribute-idp-mapper", Config = new() { ["attribute"] = "company_id" }
        });

        Assert.Equal($"{Realm}/identity-provider/instances/corp/mappers", handler.SentRequests[0].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[0]);
        Assert.Equal("corp", body.GetProperty("identityProviderAlias").GetString());
        Assert.Equal("hardcoded-attribute-idp-mapper", body.GetProperty("identityProviderMapper").GetString());
    }

    [Fact]
    public async Task DeleteCalls_UseExpectedPaths()
    {
        var (sut, handler) = CreateIdp();
        handler.AddResponse(HttpStatusCode.NoContent);
        handler.AddResponse(HttpStatusCode.NoContent);
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.DeleteIdentityProviderAsync("corp");
        await sut.DeleteMapperAsync("corp", "m1");
        await sut.UnlinkUserAsync("u1", "corp");

        Assert.All(handler.SentRequests, r => Assert.Equal(HttpMethod.Delete, r.Method));
        Assert.Equal($"{Realm}/identity-provider/instances/corp", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal($"{Realm}/identity-provider/instances/corp/mappers/m1", handler.SentRequests[1].RequestUri!.AbsolutePath);
        Assert.Equal($"{Realm}/users/u1/federated-identity/corp", handler.SentRequests[2].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task LinkUserAsync_PostsFederatedIdentity()
    {
        var (sut, handler) = CreateIdp();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.LinkUserAsync("u1", "corp", "ext-1", "alice@corp");

        Assert.Equal($"{Realm}/users/u1/federated-identity/corp", handler.SentRequests[0].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[0]);
        Assert.Equal("corp", body.GetProperty("identityProvider").GetString());
        Assert.Equal("ext-1", body.GetProperty("userId").GetString());
        Assert.Equal("alice@corp", body.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task LinkUserAsync_RequiresExternalIdentity()
    {
        var (sut, _) = CreateIdp();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.LinkUserAsync("u1", "corp", "", "alice"));
    }

    [Fact]
    public async Task GetUserFederatedIdentitiesAsync_Parses()
    {
        var (sut, handler) = CreateIdp();
        handler.AddResponse(HttpStatusCode.OK, """[ { "identityProvider": "corp", "userId": "ext-1", "userName": "alice" } ]""");

        var result = await sut.GetUserFederatedIdentitiesAsync("u1");

        Assert.Equal("ext-1", Assert.Single(result.Response).UserId);
    }

    // ── User federation ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetProvidersAsync_FiltersByUserStorageType()
    {
        var (sut, handler) = CreateFederation();
        handler.AddResponse(HttpStatusCode.OK, """[ { "id": "c1", "name": "ad", "providerId": "ldap", "config": { "vendor": ["ad"] } } ]""");

        var result = await sut.GetProvidersAsync();

        Assert.Equal(["ad"], Assert.Single(result.Response).Config!["vendor"]);
        Assert.Equal($"{Realm}/components", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal("?type=org.keycloak.storage.UserStorageProvider", handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task CreateProviderAsync_UsesRealmIdAndReturnsCreatedProvider()
    {
        var handler = new FakeHttpMessageHandler();
        handler.AddResponse(HttpStatusCode.OK, """{ "id": "realm-internal-id", "realm": "test" }""");
        handler.AddResponse(HttpStatusCode.Created);
        handler.AddResponse(HttpStatusCode.OK, """{ "id": "c1", "name": "ad", "providerId": "ldap" }""");

        // FakeHttpMessageHandler cannot set headers, so wrap it to add the Location of the new component.
        var factory = new LocationFactory(handler, "http://localhost:8080/admin/realms/test/components/c1");
        var sutWithLocation = new UserFederationManagement(factory, _options);

        var result = await sutWithLocation.CreateProviderAsync(new CreateUserStorageProviderRequestDto
        {
            Name = "ad", ProviderId = "ldap", Config = new() { ["vendor"] = ["ad"] }
        });

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal("c1", result.Response.Id);
        var body = await BodyAsync(handler.SentRequests[1]);
        Assert.Equal("realm-internal-id", body.GetProperty("parentId").GetString());
        Assert.Equal("org.keycloak.storage.UserStorageProvider", body.GetProperty("providerType").GetString());
        Assert.Equal("ad", body.GetProperty("config").GetProperty("vendor")[0].GetString());
        Assert.Equal($"{Realm}/components/c1", handler.SentRequests[2].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateProviderAsync_MergesConfigKeys()
    {
        var (sut, handler) = CreateFederation();
        handler.AddResponse(HttpStatusCode.OK, """{ "id": "c1", "name": "ad", "providerId": "ldap", "config": { "bindCredential": ["**********"], "editMode": ["READ_ONLY"] } }""");
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateProviderAsync("c1", new UpdateUserStorageProviderRequestDto { Name = "corp", Config = new() { ["editMode"] = ["WRITABLE"] } });

        var body = await BodyAsync(handler.SentRequests[1]);
        Assert.Equal("corp", body.GetProperty("name").GetString());
        Assert.Equal("WRITABLE", body.GetProperty("config").GetProperty("editMode")[0].GetString());
        Assert.Equal("**********", body.GetProperty("config").GetProperty("bindCredential")[0].GetString());
    }

    [Theory]
    [InlineData(false, "triggerFullSync")]
    [InlineData(true, "triggerChangedUsersSync")]
    public async Task SyncUsersAsync_SendsAction(bool changedOnly, string action)
    {
        var (sut, handler) = CreateFederation();
        handler.AddResponse(HttpStatusCode.OK, """{ "ignored": false, "added": 3, "updated": 1, "removed": 0, "failed": 0, "status": "3 imported users, 1 updated users" }""");

        var result = await sut.SyncUsersAsync("c1", changedOnly);

        Assert.Equal(3, result.Response.Added);
        Assert.Equal($"{Realm}/user-storage/c1/sync", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal($"?action={action}", handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task ImportedUserActions_PostToUserStorage()
    {
        var (sut, handler) = CreateFederation();
        handler.AddResponse(HttpStatusCode.NoContent);
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.RemoveImportedUsersAsync("c1");
        await sut.UnlinkUsersAsync("c1");

        Assert.Equal($"{Realm}/user-storage/c1/remove-imported-users", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal($"{Realm}/user-storage/c1/unlink-users", handler.SentRequests[1].RequestUri!.AbsolutePath);
        Assert.All(handler.SentRequests, r => Assert.Equal(HttpMethod.Post, r.Method));
    }

    [Fact]
    public async Task GetProviderMappersAsync_FiltersByParentAndMapperType()
    {
        var (sut, handler) = CreateFederation();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await sut.GetProviderMappersAsync("c1");

        Assert.Equal("?parent=c1&type=org.keycloak.storage.ldap.mappers.LDAPStorageMapper", handler.SentRequests[0].RequestUri!.Query);
    }

    [Theory]
    [InlineData(true, "testAuthentication")]
    [InlineData(false, "testConnection")]
    public async Task TestLdapConnectionAsync_SendsStoredSettingsWithComponentId(bool authenticate, string action)
    {
        var (sut, handler) = CreateFederation();
        handler.AddResponse(HttpStatusCode.OK, """
            { "id": "c1", "name": "ad", "providerId": "ldap",
              "config": { "connectionUrl": ["ldap://ad:389"], "bindDn": ["cn=admin"], "bindCredential": ["**********"], "authType": ["simple"] } }
            """);
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.TestLdapConnectionAsync("c1", authenticate);

        Assert.True(result.IsSuccessful);
        Assert.Equal($"{Realm}/testLDAPConnection", handler.SentRequests[1].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[1]);
        Assert.Equal(action, body.GetProperty("action").GetString());
        Assert.Equal("c1", body.GetProperty("componentId").GetString());
        Assert.Equal("ldap://ad:389", body.GetProperty("connectionUrl").GetString());
        Assert.Equal("**********", body.GetProperty("bindCredential").GetString());
        Assert.False(body.TryGetProperty("startTls", out _));
    }

    [Fact]
    public async Task TestLdapConnectionAsync_Rejected_ReturnsFailure()
    {
        var (sut, handler) = CreateFederation();
        handler.AddResponse(HttpStatusCode.OK, """{ "id": "c1", "name": "ad", "providerId": "ldap", "config": {} }""");
        handler.AddResponse(HttpStatusCode.BadRequest, """{ "errorMessage": "LDAPErrorConnection" }""");

        var result = await sut.TestLdapConnectionAsync("c1");

        Assert.False(result.IsSuccessful);
        Assert.Contains("LDAP", result.ErrorMessage);
    }

    [Fact]
    public void AddKeycloak_RegistersBothServices()
    {
        var services = new ServiceCollection();
        services.AddKeycloak("erp", s => { s.ServerUrl = "https://sso.example.test/"; s.RealmName = "erp"; });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<IdentityProviderManagement>(scope.ServiceProvider.GetRequiredKeyedService<IIdentityProviderManagement>("erp"));
        Assert.IsType<UserFederationManagement>(scope.ServiceProvider.GetRequiredKeyedService<IUserFederationManagement>("erp"));
    }

    /// <summary>Adds a Location header to 201 responses from the wrapped fake handler.</summary>
    private sealed class LocationFactory(FakeHttpMessageHandler inner, string location) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new LocationHandler(inner, location)) { BaseAddress = new Uri("http://localhost:8080/") };

        private sealed class LocationHandler(FakeHttpMessageHandler inner, string location) : DelegatingHandler(inner)
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.Created)
                    response.Headers.Location = new Uri(location);
                return response;
            }
        }
    }
}
