using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Roles;
using Keycloak.Net.Sdk.Roles.Contracts;
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

    // ── GetRealmRolesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetRealmRolesAsync_Success_ReturnsRealmRoleList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmRolesResponse);

        var result = await sut.GetRealmRolesAsync();

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.RealmRoleId, result.Response[0].Id);
        Assert.Equal(TestData.RealmRoleName, result.Response[0].Name);
        Assert.False(result.Response[0].ClientRole);
        Assert.Contains($"realms/{TestData.RealmName}/roles", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
    }

    [Fact]
    public async Task GetRealmRolesAsync_Failure_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Unauthorized);

        var result = await sut.GetRealmRolesAsync();

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    // ── GetRealmRoleAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRealmRoleAsync_Success_ReturnsSingleRole()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmRoleResponse);

        var result = await sut.GetRealmRoleAsync(TestData.RealmRoleName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.RealmRoleId, result.Response.Id);
        Assert.Equal(TestData.RealmRoleName, result.Response.Name);
        Assert.Contains($"roles/{TestData.RealmRoleName}", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── CreateRealmRoleAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateRealmRoleAsync_Success_SendsPostWithRoleBody()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        var request = new CreateRealmRoleRequestDto { Name = TestData.RealmRoleName, Description = "Test realm role" };
        var result = await sut.CreateRealmRoleAsync(request);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        Assert.Contains($"realms/{TestData.RealmName}/roles", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains(TestData.RealmRoleName, body);
    }

    // ── DeleteRealmRoleAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRealmRoleAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DeleteRealmRoleAsync(TestData.RealmRoleName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains($"roles/{TestData.RealmRoleName}", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── GetUserRealmRolesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserRealmRolesAsync_Success_ReturnsRoleList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmRolesResponse);

        var result = await sut.GetUserRealmRolesAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Contains($"users/{TestData.UserId}/role-mappings/realm", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
    }

    // ── AssignRealmRoleToUserAsync ────────────────────────────────────────────

    [Fact]
    public async Task AssignRealmRoleToUserAsync_Success_SendsPostWithRoleBody()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.AssignRealmRoleToUserAsync(TestData.UserId, TestData.RealmRoleId, TestData.RealmRoleName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        Assert.Contains($"users/{TestData.UserId}/role-mappings/realm", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains(TestData.RealmRoleId, body);
        Assert.Contains(TestData.RealmRoleName, body);
    }

    // ── RemoveRealmRoleFromUserAsync ──────────────────────────────────────────

    [Fact]
    public async Task RemoveRealmRoleFromUserAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.RemoveRealmRoleFromUserAsync(TestData.UserId, TestData.RealmRoleId, TestData.RealmRoleName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains($"users/{TestData.UserId}/role-mappings/realm", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains(TestData.RealmRoleId, body);
    }

    // ── GetGroupRealmRolesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetGroupRealmRolesAsync_Success_ReturnsRoleList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmRolesResponse);

        var result = await sut.GetGroupRealmRolesAsync(TestData.GroupId);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Contains($"groups/{TestData.GroupId}/role-mappings/realm", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
    }

    // ── AssignRealmRoleToGroupAsync ───────────────────────────────────────────

    [Fact]
    public async Task AssignRealmRoleToGroupAsync_Success_SendsPostWithRoleBody()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.AssignRealmRoleToGroupAsync(TestData.GroupId, TestData.RealmRoleId, TestData.RealmRoleName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        Assert.Contains($"groups/{TestData.GroupId}/role-mappings/realm", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains(TestData.RealmRoleId, body);
        Assert.Contains(TestData.RealmRoleName, body);
    }

    // ── RemoveRealmRoleFromGroupAsync ─────────────────────────────────────────

    [Fact]
    public async Task RemoveRealmRoleFromGroupAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.RemoveRealmRoleFromGroupAsync(TestData.GroupId, TestData.RealmRoleId, TestData.RealmRoleName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains($"groups/{TestData.GroupId}/role-mappings/realm", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains(TestData.RealmRoleId, body);
    }
}
