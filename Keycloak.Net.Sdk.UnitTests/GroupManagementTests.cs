using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Groups;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class GroupManagementTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (GroupManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new GroupManagement(factory, _options), handler);
    }

    // ── CreateGroupAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGroupAsync_Success_ReturnsSuccessResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        var result = await sut.CreateGroupAsync(new CreateGroupRequestDto { Name = TestData.GroupName });

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Contains($"admin/realms/{TestData.RealmName}/groups", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
    }

    [Fact]
    public async Task CreateGroupAsync_Conflict_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Conflict);

        var result = await sut.CreateGroupAsync(new CreateGroupRequestDto { Name = TestData.GroupName });

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    // ── DeleteGroupAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteGroupAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DeleteGroupAsync(TestData.GroupId);

        Assert.True(result.IsSuccessful);
        Assert.Contains($"groups/{TestData.GroupId}", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
    }

    // ── GetGroupsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGroupsAsync_Success_ReturnsGroupList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.GroupsResponse);

        var result = await sut.GetGroupsAsync();

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.GroupId, result.Response[0].Id);
        Assert.Equal(TestData.GroupName, result.Response[0].Name);
        Assert.Contains($"admin/realms/{TestData.RealmName}/groups", handler.SentRequests[0].RequestUri!.ToString());
    }

    // ── GetGroupAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGroupAsync_Success_ReturnsGroupInfo()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.GroupResponse);

        var result = await sut.GetGroupAsync(TestData.GroupId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.GroupId, result.Response.Id);
        Assert.Equal(TestData.GroupName, result.Response.Name);
        Assert.Equal(TestData.GroupPath, result.Response.Path);
        Assert.Contains($"groups/{TestData.GroupId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetGroupAsync_NotFound_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.GetGroupAsync("nonexistent-id");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    // ── AddUserToGroupAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AddUserToGroupAsync_Success_SendsCorrectRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.AddUserToGroupAsync(TestData.UserId, TestData.GroupId);

        Assert.True(result.IsSuccessful);
        Assert.Contains($"users/{TestData.UserId}/groups/{TestData.GroupId}", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
    }

    // ── RemoveUserFromGroupAsync ──────────────────────────────────────────────

    [Fact]
    public async Task RemoveUserFromGroupAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.RemoveUserFromGroupAsync(TestData.UserId, TestData.GroupId);

        Assert.True(result.IsSuccessful);
        Assert.Contains($"users/{TestData.UserId}/groups/{TestData.GroupId}", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
    }

    // ── GetUserGroupsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserGroupsAsync_Success_ReturnsUserGroups()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.GroupsResponse);

        var result = await sut.GetUserGroupsAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.GroupId, result.Response[0].Id);
        Assert.Contains($"users/{TestData.UserId}/groups", handler.SentRequests[0].RequestUri!.ToString());
    }
}
