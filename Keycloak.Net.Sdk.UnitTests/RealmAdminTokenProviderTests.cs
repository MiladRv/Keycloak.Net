using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class RealmAdminTokenProviderTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl     = "http://localhost:8080/",
        RealmName     = TestData.RealmName,
        ClientId      = TestData.ClientId,
        ClientSecret  = TestData.ClientSecret,
        ClientUuid    = TestData.ClientUuid,
        AdminUsername = "admin",
        AdminPassword = "admin-password"
    });

    [Fact]
    public async Task GetTokenAsync_FirstCall_FetchesTokenFromKeycloak()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        var sut = new RealmAdminTokenProvider(factory, _options);

        var token = await sut.GetTokenAsync();

        Assert.Equal(TestData.AccessToken, token);
        Assert.Single(handler.SentRequests);
        Assert.Contains("realms/master/protocol/openid-connect/token", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("grant_type=password", body);
        Assert.Contains("username=admin", body);
    }

    [Fact]
    public async Task GetTokenAsync_SecondCallWithinExpiry_ReturnsCachedTokenWithoutAnotherRequest()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        var sut = new RealmAdminTokenProvider(factory, _options);

        var first = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        Assert.Equal(first, second);
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task GetTokenAsync_ConcurrentCalls_FetchesTokenOnlyOnce()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        // Only one response queued - if two requests raced through to Keycloak, the
        // second would find nothing left in the queue and throw.
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        var sut = new RealmAdminTokenProvider(factory, _options);

        var tasks = Enumerable.Range(0, 5).Select(_ => sut.GetTokenAsync());
        var results = await Task.WhenAll(tasks);

        Assert.All(results, t => Assert.Equal(TestData.AccessToken, t));
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task GetTokenAsync_KeycloakRejectsCredentials_ThrowsKeycloakException()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        handler.AddResponse(HttpStatusCode.Unauthorized);
        var sut = new RealmAdminTokenProvider(factory, _options);

        await Assert.ThrowsAsync<KeycloakException>(() => sut.GetTokenAsync());
    }
}
