using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Roles;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class RealmRoleCompositeTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (RoleManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new RoleManagement(factory, _options), handler);
    }

    private static string Realm => $"/admin/realms/{TestData.RealmName}";

    private static readonly List<CompositeRoleRequestDto> Composites =
    [
        new() { Id = "r1", Name = "invoice-view" },
        new() { Id = "r2", Name = "report-view" }
    ];

    [Fact]
    public async Task GetRealmRoleCompositesAsync_CallsRealmCompositesEndpoint()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """
            [ { "id": "r1", "name": "viewer", "composite": false, "clientRole": false, "containerId": "realm" } ]
            """);

        var result = await sut.GetRealmRoleCompositesAsync("branch-manager");

        Assert.True(result.IsSuccessful);
        Assert.Equal("viewer", Assert.Single(result.Response).Name);
        var request = handler.SentRequests.Single();
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"{Realm}/roles/branch-manager/composites/realm", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetRealmRoleClientCompositesAsync_CallsClientCompositesEndpoint()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """
            [ { "id": "c1", "name": "invoice-view", "composite": false, "clientRole": true, "containerId": "finance-uuid" } ]
            """);

        var result = await sut.GetRealmRoleClientCompositesAsync("finance-uuid", "branch-manager");

        Assert.True(result.IsSuccessful);
        Assert.Equal("invoice-view", Assert.Single(result.Response).Name);
        Assert.Equal($"{Realm}/roles/branch-manager/composites/clients/finance-uuid", handler.SentRequests.Single().RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AddRealmRoleCompositesAsync_PostsRoleList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.AddRealmRoleCompositesAsync("branch-manager", Composites);

        Assert.True(result.IsSuccessful);
        var request = handler.SentRequests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"{Realm}/roles/branch-manager/composites", request.RequestUri!.AbsolutePath);
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        Assert.Equal(["r1", "r2"], body.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToList());
    }

    [Fact]
    public async Task RemoveRealmRoleCompositesAsync_SendsDeleteWithRoleList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.RemoveRealmRoleCompositesAsync("branch-manager", Composites);

        Assert.True(result.IsSuccessful);
        var request = handler.SentRequests.Single();
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal($"{Realm}/roles/branch-manager/composites", request.RequestUri!.AbsolutePath);
        Assert.Contains("\"id\":\"r2\"", await request.Content!.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("a/b", "a%2Fb")]
    [InlineData("a b", "a%20b")]
    [InlineData("a#b", "a%23b")]
    public async Task RealmRoleCompositeMethods_EscapeRoleName(string roleName, string escaped)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.GetRealmRoleCompositesAsync(roleName);
        await sut.AddRealmRoleCompositesAsync(roleName, Composites);

        Assert.All(handler.SentRequests, r => Assert.Contains($"/roles/{escaped}/composites", r.RequestUri!.AbsoluteUri));
    }

    [Fact]
    public async Task GetRealmRoleClientCompositesAsync_EscapesClientUuid()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await sut.GetRealmRoleClientCompositesAsync("x/y", "role");

        Assert.EndsWith("/composites/clients/x%2Fy", handler.SentRequests.Single().RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetRealmRoleCompositesAsync_NotFound_ReturnsFailure()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.GetRealmRoleCompositesAsync("missing");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task AddRealmRoleCompositesAsync_Failure_ReturnsFailure()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.AddRealmRoleCompositesAsync("missing", Composites);

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
