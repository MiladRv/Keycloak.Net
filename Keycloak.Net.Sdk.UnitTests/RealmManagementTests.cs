using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.Realms.Contracts;
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
        var adminTokenProvider = new RealmAdminTokenProvider(factory, _options);
        return (new RealmManagement(factory, adminTokenProvider), handler);
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

    // ── GetRealmsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRealmsAsync_Success_ReturnsRealmList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmsResponse);

        var result = await sut.GetRealmsAsync();

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.RealmName, result.Response[0].Realm);
        Assert.Equal(HttpMethod.Get, handler.SentRequests[1].Method);
        Assert.Contains("admin/realms", handler.SentRequests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRealmsAsync_AdminTokenFails_ThrowsKeycloakException()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<KeycloakException>(() => sut.GetRealmsAsync());
    }

    // ── GetRealmAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRealmAsync_Success_ReturnsRealm()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmResponse);

        var result = await sut.GetRealmAsync(TestData.RealmName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.RealmName, result.Response.Realm);
        Assert.Equal(TestData.RealmDisplayName, result.Response.DisplayName);
        Assert.Equal(HttpMethod.Get, handler.SentRequests[1].Method);
        Assert.Contains($"admin/realms/{TestData.RealmName}", handler.SentRequests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRealmAsync_NotFound_ReturnsFailure()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.GetRealmAsync("non-existent-realm");

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task GetRealmAsync_AdminTokenFails_ThrowsKeycloakException()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<KeycloakException>(() => sut.GetRealmAsync(TestData.RealmName));
    }

    // ── UpdateRealmAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRealmAsync_Success_SendsPutRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        var request = new UpdateRealmRequestDto { DisplayName = "Updated Display Name", Enabled = true };
        var result = await sut.UpdateRealmAsync(TestData.RealmName, request);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        Assert.Contains($"admin/realms/{TestData.RealmName}", handler.SentRequests[1].RequestUri!.ToString());
        var body = await handler.SentRequests[1].Content!.ReadAsStringAsync();
        Assert.Contains("Updated Display Name", body);
    }

    [Fact]
    public async Task UpdateRealmAsync_AdminTokenFails_ThrowsKeycloakException()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<KeycloakException>(
            () => sut.UpdateRealmAsync(TestData.RealmName, new UpdateRealmRequestDto()));
    }

    // ── DeleteRealmAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRealmAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DeleteRealmAsync(TestData.RealmName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[1].Method);
        Assert.Contains($"admin/realms/{TestData.RealmName}", handler.SentRequests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task DeleteRealmAsync_AdminTokenFails_ThrowsKeycloakException()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<KeycloakException>(() => sut.DeleteRealmAsync(TestData.RealmName));
    }

    // ── Admin token caching ───────────────────────────────────────────────────

    [Fact]
    public async Task MultipleOperations_ShareOneAdminTokenProvider_OnlyFetchTokenOnce()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        var adminTokenProvider = new RealmAdminTokenProvider(factory, _options);
        var sut = new RealmManagement(factory, adminTokenProvider);

        // One token response, then one response per realm operation below
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmsResponse);
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.GetRealmsAsync();
        await sut.GetRealmAsync(TestData.RealmName);
        await sut.DeleteRealmAsync(TestData.RealmName);

        Assert.Equal(4, handler.SentRequests.Count);
        Assert.Contains("openid-connect/token", handler.SentRequests[0].RequestUri!.ToString());
        // None of the remaining requests re-fetch the admin token
        Assert.DoesNotContain(handler.SentRequests.Skip(1), r => r.RequestUri!.ToString().Contains("openid-connect/token"));
    }
}
