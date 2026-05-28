using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Roles;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class RoleManagementTests
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

    // ── GetClientRoles ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetClientRoles_Success_ReturnsRoleList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientRolesResponse);

        var result = await sut.GetClientRoles();

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.RoleId, result.Response[0].Id);
        Assert.Equal(TestData.RoleName, result.Response[0].Name);
        Assert.True(result.Response[0].ClientRole);
        Assert.Contains($"clients/{TestData.ClientUuid}/roles", handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetClientRoles_Failure_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Unauthorized);

        var result = await sut.GetClientRoles();

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    // ── AssignClientRoleToUser ────────────────────────────────────────────────

    [Fact]
    public async Task AssignClientRoleToUser_Success_SendsCorrectRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.AssignClientRoleToUser(TestData.UserId, TestData.RoleId, TestData.RoleName);

        Assert.True(result.IsSuccessful);
        Assert.Contains($"users/{TestData.UserId}/role-mappings/clients/{TestData.ClientUuid}",
            handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains(TestData.RoleId, body);
        Assert.Contains(TestData.RoleName, body);
    }

    // ── RemoveClientRoleFromUserAsync ─────────────────────────────────────────

    [Fact]
    public async Task RemoveClientRoleFromUserAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.RemoveClientRoleFromUserAsync(TestData.UserId, TestData.RoleId, TestData.RoleName);

        Assert.True(result.IsSuccessful);
        Assert.Contains($"users/{TestData.UserId}/role-mappings/clients/{TestData.ClientUuid}",
            handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
    }
}
