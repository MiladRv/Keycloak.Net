using System.Net;
using Keycloak.Net.Sdk.Clients;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class ClientManagementTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (ClientManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new ClientManagement(factory, _options), handler);
    }

    // ── GetClientScopes ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetClientScopes_Success_ReturnsScopeList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientScopesResponse);

        var result = await sut.GetClientScopes();

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal("profile", result.Response[0].Name);
        Assert.Contains($"realms/{TestData.RealmName}/client-scopes", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── GetClientsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetClientsAsync_Success_ReturnsClientList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientsResponse);

        var result = await sut.GetClientsAsync();

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal("test-client", result.Response[0].ClientId);
        Assert.Equal("client-abc", result.Response[0].Id);
        Assert.True(result.Response[0].ServiceAccountsEnabled);
    }

    // ── CreateClientAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateClientAsync_Success_SendsCorrectRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        var request = new CreateClientRequestDto { ClientId = "new-client", Name = "New Client" };
        var result = await sut.CreateClientAsync(request);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        Assert.Contains($"admin/realms/{TestData.RealmName}/clients", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("new-client", body);
    }

    // ── DeleteClientAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteClientAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DeleteClientAsync(TestData.ClientUuid);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains(TestData.ClientUuid, handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── EnableServiceAccountAsync ─────────────────────────────────────────────

    [Fact]
    public async Task EnableServiceAccountAsync_Success_SendsPutWithServiceAccountsEnabled()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.EnableServiceAccountAsync(TestData.ClientUuid);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("\"serviceAccountsEnabled\":true", body);
    }
}
