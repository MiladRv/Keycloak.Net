using System.Net;
using Keycloak.Net.Sdk.Authentications;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class TokenProviderTests
{
    [Fact]
    public async Task GetTokenAsync_InvalidCredentials_ReportsErrorAndAllowsRetry()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        handler.AddResponse(HttpStatusCode.Unauthorized, "{\"error\":\"invalid_client\"}");
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        var sut = new TokenProvider(factory, _options);

        var error = await Assert.ThrowsAsync<KeycloakException>(() => sut.GetTokenAsync());

        Assert.Equal(TestData.RealmName, error.RealmName);
        Assert.Equal(TestData.ClientId, error.ClientId);
        Assert.Contains("401", error.Message);
        Assert.Contains("invalid_client", error.Message);
        Assert.Equal(TestData.AccessToken, await sut.GetTokenAsync());
        Assert.Equal(2, handler.SentRequests.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task GetTokenAsync_MissingAccessToken_ThrowsKeycloakException(string body)
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        handler.AddResponse(HttpStatusCode.OK, body);
        var sut = new TokenProvider(factory, _options);

        var error = await Assert.ThrowsAsync<KeycloakException>(() => sut.GetTokenAsync());

        Assert.Contains("did not contain an access token", error.Message);
    }

    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    [Fact]
    public async Task GetTokenAsync_FirstCall_FetchesTokenFromKeycloak()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        var sut = new TokenProvider(factory, _options);

        var token = await sut.GetTokenAsync();

        Assert.Equal(TestData.AccessToken, token);
        Assert.Single(handler.SentRequests);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("grant_type=client_credentials", body);
    }

    [Fact]
    public async Task GetTokenAsync_SecondCallWithinExpiry_ReturnsCachedToken()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        var sut = new TokenProvider(factory, _options);

        var first  = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        Assert.Equal(first, second);
        // Only one HTTP call  second call hits the cache
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task GetTokenAsync_ConcurrentCalls_FetchesTokenOnlyOnce()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        // Add one response  if two requests are made concurrently, the second will throw
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        var sut = new TokenProvider(factory, _options);

        var tasks = Enumerable.Range(0, 5).Select(_ => sut.GetTokenAsync());
        var results = await Task.WhenAll(tasks);

        Assert.All(results, t => Assert.Equal(TestData.AccessToken, t));
        Assert.Single(handler.SentRequests);
    }
}
