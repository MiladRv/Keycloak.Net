using System.Net;
using Keycloak.Net.Sdk.Athentications;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class TokenManagementTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (TokenManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new TokenManagement(factory, _options), handler);
    }

    // ── RefreshTokenAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_Success_ReturnsNewTokens()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);

        var result = await sut.RefreshTokenAsync(TestData.RefreshToken);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.AccessToken, result.Response.AccessToken);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("grant_type=refresh_token", body);
        Assert.Contains($"refresh_token={TestData.RefreshToken}", body);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredRefreshToken_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.BadRequest);

        var result = await sut.RefreshTokenAsync("expired-refresh-token");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    // ── RevokeTokenAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeTokenAsync_Success_ReturnsSuccessResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK);

        var result = await sut.RevokeTokenAsync(TestData.RefreshToken);

        Assert.True(result.IsSuccessful);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains($"token={TestData.RefreshToken}", body);
        Assert.Contains("token_type_hint=refresh_token", body);
        Assert.Contains($"realms/{TestData.RealmName}/protocol/openid-connect/revoke",
            handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task RevokeTokenAsync_Failure_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.BadRequest);

        var result = await sut.RevokeTokenAsync("invalid-token");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
