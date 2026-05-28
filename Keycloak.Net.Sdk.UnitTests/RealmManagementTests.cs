using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class RealmManagementTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl      = "http://localhost:8080/",
        RealmName      = TestData.RealmName,
        ClientId       = TestData.ClientId,
        ClientSecret   = TestData.ClientSecret,
        ClientUuid     = TestData.ClientUuid,
        AdminUsername  = "admin",
        AdminPassword  = "admin-password"
    });

    private (RealmManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new RealmManagement(factory, _options), handler);
    }

    [Fact]
    public async Task CreateRealmAsync_Success_FirstGetsAdminTokenThenCreatesRealm()
    {
        var (sut, handler) = CreateSut();
        // First call: admin token; second call: realm creation
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        handler.AddResponse(HttpStatusCode.Created);

        var result = await sut.CreateRealmAsync("new-realm");

        Assert.True(result.IsSuccessful);
        Assert.Equal(2, handler.SentRequests.Count);
        // First request must be the token endpoint
        Assert.Contains("openid-connect/token", handler.SentRequests[0].RequestUri!.ToString());
        // Second request must be the realm creation
        Assert.Contains("admin/realms", handler.SentRequests[1].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.SentRequests[1].Method);
        var body = await handler.SentRequests[1].Content!.ReadAsStringAsync();
        Assert.Contains("new-realm", body);
    }

    [Fact]
    public async Task CreateRealmAsync_AdminTokenFails_ThrowsKeycloakException()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<KeycloakException>(() => sut.CreateRealmAsync("new-realm"));
    }
}
