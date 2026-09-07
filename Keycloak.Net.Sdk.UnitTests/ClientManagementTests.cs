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

    // ── GetClientScopeAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetClientScopeAsync_Success_ReturnsScope()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientScopeResponse);

        var result = await sut.GetClientScopeAsync(TestData.ClientScopeId);

        Assert.True(result.IsSuccessful);
        Assert.Equal("profile", result.Response.Name);
        Assert.Contains($"client-scopes/{TestData.ClientScopeId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── CreateClientScopeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateClientScopeAsync_Success_SendsCorrectRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        var request = new CreateClientScopeRequestDto { Name = "new-scope", Description = "A new scope" };
        var result = await sut.CreateClientScopeAsync(request);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        Assert.Contains($"admin/realms/{TestData.RealmName}/client-scopes", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("new-scope", body);
    }

    // ── UpdateClientScopeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateClientScopeAsync_Success_SendsPutRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var request = new UpdateClientScopeRequestDto { Name = "updated-scope", Description = "Updated" };
        var result = await sut.UpdateClientScopeAsync(TestData.ClientScopeId, request);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        Assert.Contains($"client-scopes/{TestData.ClientScopeId}", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("updated-scope", body);
    }

    // ── DeleteClientScopeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteClientScopeAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DeleteClientScopeAsync(TestData.ClientScopeId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains(TestData.ClientScopeId, handler.SentRequests[0].RequestUri!.ToString());
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
    public async Task EnableServiceAccountAsync_Success_GetsThenPutsFullRepresentationWithServiceAccountsEnabled()
    {
        var (sut, handler) = CreateSut();
        // First call: GET the existing client; second call: PUT it back with the flag flipped
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.EnableServiceAccountAsync(TestData.ClientUuid);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        var body = await handler.SentRequests[1].Content!.ReadAsStringAsync();
        Assert.Contains("\"serviceAccountsEnabled\":true", body);
        // Fields not related to the service account flag must survive the round trip
        Assert.Contains("redirectUris", body);
        Assert.Contains("super-secret", body);
    }

    [Fact]
    public async Task EnableServiceAccountAsync_ClientNotFound_ReturnsFailureWithoutPutting()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.EnableServiceAccountAsync("nonexistent-id");

        Assert.False(result.IsSuccessful);
        Assert.Single(handler.SentRequests);
    }

    // ── GetProtocolMappersAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetProtocolMappersAsync_Success_ReturnsMapperList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ProtocolMappersResponse);

        var result = await sut.GetProtocolMappersAsync(TestData.ClientUuid);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.ProtocolMapperName, result.Response[0].Name);
        Assert.Contains($"clients/{TestData.ClientUuid}/protocol-mappers/models", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── GetProtocolMapperAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetProtocolMapperAsync_Success_ReturnsMapper()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ProtocolMapperResponse);

        var result = await sut.GetProtocolMapperAsync(TestData.ClientUuid, TestData.ProtocolMapperId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.ProtocolMapperName, result.Response.Name);
        Assert.Contains($"protocol-mappers/models/{TestData.ProtocolMapperId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── CreateProtocolMapperAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateProtocolMapperAsync_Success_SendsCorrectRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        var request = new CreateProtocolMapperRequestDto
        {
            Name = "new-mapper",
            ProtocolMapper = "oidc-usermodel-attribute-mapper",
            Config = new Dictionary<string, string> { ["user.attribute"] = "department" }
        };
        var result = await sut.CreateProtocolMapperAsync(TestData.ClientUuid, request);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        Assert.Contains($"clients/{TestData.ClientUuid}/protocol-mappers/models", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("new-mapper", body);
    }

    // ── UpdateProtocolMapperAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateProtocolMapperAsync_Success_SendsPutRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var request = new UpdateProtocolMapperRequestDto
        {
            Id = TestData.ProtocolMapperId,
            Name = "updated-mapper",
            ProtocolMapper = "oidc-usermodel-attribute-mapper"
        };
        var result = await sut.UpdateProtocolMapperAsync(TestData.ClientUuid, TestData.ProtocolMapperId, request);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        Assert.Contains($"protocol-mappers/models/{TestData.ProtocolMapperId}", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("updated-mapper", body);
    }

    // ── DeleteProtocolMapperAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteProtocolMapperAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DeleteProtocolMapperAsync(TestData.ClientUuid, TestData.ProtocolMapperId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains(TestData.ProtocolMapperId, handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── GetDefaultClientScopesAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetDefaultClientScopesAsync_Success_ReturnsScopeList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientScopesResponse);

        var result = await sut.GetDefaultClientScopesAsync(TestData.ClientUuid);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Contains($"clients/{TestData.ClientUuid}/default-client-scopes", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
    }

    // ── AddDefaultClientScopeAsync ────────────────────────────────────────────

    [Fact]
    public async Task AddDefaultClientScopeAsync_Success_SendsPutRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.AddDefaultClientScopeAsync(TestData.ClientUuid, TestData.ClientScopeId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        Assert.Contains($"clients/{TestData.ClientUuid}/default-client-scopes/{TestData.ClientScopeId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── RemoveDefaultClientScopeAsync ─────────────────────────────────────────

    [Fact]
    public async Task RemoveDefaultClientScopeAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.RemoveDefaultClientScopeAsync(TestData.ClientUuid, TestData.ClientScopeId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains($"clients/{TestData.ClientUuid}/default-client-scopes/{TestData.ClientScopeId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── GetOptionalClientScopesAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetOptionalClientScopesAsync_Success_ReturnsScopeList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientScopesResponse);

        var result = await sut.GetOptionalClientScopesAsync(TestData.ClientUuid);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Contains($"clients/{TestData.ClientUuid}/optional-client-scopes", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
    }

    // ── AddOptionalClientScopeAsync ───────────────────────────────────────────

    [Fact]
    public async Task AddOptionalClientScopeAsync_Success_SendsPutRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.AddOptionalClientScopeAsync(TestData.ClientUuid, TestData.ClientScopeId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        Assert.Contains($"clients/{TestData.ClientUuid}/optional-client-scopes/{TestData.ClientScopeId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── RemoveOptionalClientScopeAsync ────────────────────────────────────────

    [Fact]
    public async Task RemoveOptionalClientScopeAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.RemoveOptionalClientScopeAsync(TestData.ClientUuid, TestData.ClientScopeId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains($"clients/{TestData.ClientUuid}/optional-client-scopes/{TestData.ClientScopeId}", handler.SentRequests[0].RequestUri!.ToString());
    }
}
