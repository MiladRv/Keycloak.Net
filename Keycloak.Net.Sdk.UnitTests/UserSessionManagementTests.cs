using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.UserSessions;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class UserSessionManagementTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (UserSessionManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new UserSessionManagement(factory, _options), handler);
    }

    // ── GetUserSessionsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUserSessionsAsync_Success_ReturnsSessionList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserSessionsResponse);

        var result = await sut.GetUserSessionsAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        var session = result.Response[0];
        Assert.Equal(TestData.SessionId, session.Id);
        Assert.Equal(TestData.Username, session.Username);
        Assert.Equal(TestData.UserId, session.UserId);
        Assert.Equal("127.0.0.1", session.IpAddress);
        Assert.Contains($"users/{TestData.UserId}/sessions", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
    }

    [Fact]
    public async Task GetUserSessionsAsync_UserNotFound_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.GetUserSessionsAsync("nonexistent-user");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    // ── RevokeSessionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeSessionAsync_Success_SendsDeleteToCorrectUrl()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.RevokeSessionAsync(TestData.SessionId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains($"sessions/{TestData.SessionId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task RevokeSessionAsync_SessionNotFound_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.RevokeSessionAsync("nonexistent-session");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    // ── LogoutUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutUserAsync_Success_SendsPostToLogoutUrl()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.LogoutUserAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        Assert.Contains($"users/{TestData.UserId}/logout", handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task LogoutUserAsync_UserNotFound_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.LogoutUserAsync("nonexistent-user");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
