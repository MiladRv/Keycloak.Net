using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Groups;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

/// <summary>Group attributes, child groups, lookup by path and members.</summary>
public class GroupHierarchyTests
{
    private const string ChildGroupId = "child-group-id";

    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (GroupManagement Sut, FakeHttpMessageHandler Handler) CreateSut(string baseAddress = "http://localhost:8080/")
    {
        var (factory, handler) = HttpClientFactoryHelper.Create(baseAddress);
        return (new GroupManagement(factory, _options), handler);
    }

    private static string GroupsPath => $"/admin/realms/{TestData.RealmName}/groups";

    private static string ChildGroupResponse => $$"""
        {
            "id": "{{ChildGroupId}}",
            "name": "tehran-branch",
            "path": "{{TestData.GroupPath}}/tehran-branch",
            "parentId": "{{TestData.GroupId}}",
            "subGroupCount": 0,
            "subGroups": [],
            "attributes": { "branch_id": ["12"] }
        }
        """;

    // ── Attributes ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGroupAsync_WithAttributes_SendsAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        await sut.CreateGroupAsync(new CreateGroupRequestDto
        {
            Name       = "acme",
            Attributes = new() { ["company_id"] = ["1"] }
        });

        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.Equal("1", body.RootElement.GetProperty("attributes").GetProperty("company_id")[0].GetString());
    }

    [Fact]
    public async Task CreateGroupAsync_WithoutAttributes_OmitsAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        await sut.CreateGroupAsync(new CreateGroupRequestDto { Name = "acme" });

        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.False(body.RootElement.TryGetProperty("attributes", out _));
    }

    [Fact]
    public async Task GetGroupAsync_ParsesAttributesParentAndSubGroupCount()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, ChildGroupResponse);

        var result = await sut.GetGroupAsync(ChildGroupId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.GroupId, result.Response.ParentId);
        Assert.Equal(0, result.Response.SubGroupCount);
        Assert.Equal(["12"], result.Response.Attributes!["branch_id"]);
    }

    [Fact]
    public async Task UpdateGroupAsync_WithAttributes_ReplacesAttributesAndKeepsOtherFields()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, ChildGroupResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.UpdateGroupAsync(ChildGroupId, new UpdateGroupRequestDto
        {
            Attributes = new() { ["branch_id"] = ["13"], ["region"] = ["north"] }
        });

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        using var body = JsonDocument.Parse(await handler.SentRequests[1].Content!.ReadAsStringAsync());
        Assert.Equal("tehran-branch", body.RootElement.GetProperty("name").GetString());
        var attributes = body.RootElement.GetProperty("attributes");
        Assert.Equal("13", attributes.GetProperty("branch_id")[0].GetString());
        Assert.Equal("north", attributes.GetProperty("region")[0].GetString());
    }

    [Fact]
    public async Task UpdateGroupAsync_WithoutAttributes_KeepsExistingAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, ChildGroupResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateGroupAsync(ChildGroupId, new UpdateGroupRequestDto { Name = "tehran" });

        using var body = JsonDocument.Parse(await handler.SentRequests[1].Content!.ReadAsStringAsync());
        Assert.Equal("tehran", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("12", body.RootElement.GetProperty("attributes").GetProperty("branch_id")[0].GetString());
    }

    [Fact]
    public async Task UpdateGroupAsync_WithEmptyAttributes_ClearsAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, ChildGroupResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateGroupAsync(ChildGroupId, new UpdateGroupRequestDto { Attributes = [] });

        using var body = JsonDocument.Parse(await handler.SentRequests[1].Content!.ReadAsStringAsync());
        Assert.Empty(body.RootElement.GetProperty("attributes").EnumerateObject());
    }

    // ── Hierarchy ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateChildGroupAsync_PostsToParentChildren()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        var result = await sut.CreateChildGroupAsync(TestData.GroupId, new CreateGroupRequestDto
        {
            Name       = "tehran-branch",
            Attributes = new() { ["branch_id"] = ["12"] }
        });

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
        Assert.Equal($"{GroupsPath}/{TestData.GroupId}/children", handler.SentRequests[0].RequestUri!.AbsolutePath);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("tehran-branch", body);
        Assert.Contains("branch_id", body);
    }

    [Fact]
    public async Task CreateChildGroupAsync_ParentNotFound_ReturnsFailure()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound, """{"error":"Could not find group by id"}""");

        var result = await sut.CreateChildGroupAsync("missing", new CreateGroupRequestDto { Name = "x" });

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Theory]
    [InlineData(null, null, "")]
    [InlineData(0, 50, "?first=0&max=50")]
    [InlineData(null, 10, "?max=10")]
    public async Task GetChildGroupsAsync_GetsChildrenWithPagination(int? first, int? max, string expectedQuery)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, $"[{ChildGroupResponse}]");

        var result = await sut.GetChildGroupsAsync(TestData.GroupId, first, max);

        Assert.True(result.IsSuccessful);
        Assert.Equal(ChildGroupId, Assert.Single(result.Response).Id);
        Assert.Equal($"{GroupsPath}/{TestData.GroupId}/children", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal(expectedQuery, handler.SentRequests[0].RequestUri!.Query);
    }

    [Theory]
    [InlineData("/acme/tehran-branch", "acme/tehran-branch")]
    [InlineData("acme/tehran-branch", "acme/tehran-branch")]
    [InlineData("/acme/sales & marketing/", "acme/sales%20%26%20marketing")]
    [InlineData("/acme/a#b?c", "acme/a%23b%3Fc")]
    public async Task GetGroupByPathAsync_EscapesSegmentsAndKeepsSeparators(string path, string expectedSuffix)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, ChildGroupResponse);

        var result = await sut.GetGroupByPathAsync(path);

        Assert.True(result.IsSuccessful);
        Assert.Equal($"/admin/realms/{TestData.RealmName}/group-by-path/{expectedSuffix}",
            handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, handler.SentRequests[0].RequestUri!.Query);
    }

    // ── Members ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null, null, "")]
    [InlineData(100, 50, "?first=100&max=50")]
    public async Task GetGroupMembersAsync_ReturnsUsersWithPagination(int? first, int? max, string expectedQuery)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var result = await sut.GetGroupMembersAsync(TestData.GroupId, first, max);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.UserId, Assert.Single(result.Response).Id);
        Assert.Equal($"{GroupsPath}/{TestData.GroupId}/members", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal(expectedQuery, handler.SentRequests[0].RequestUri!.Query);
    }

    // ── Base path ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("create-child")]
    [InlineData("children")]
    [InlineData("by-path")]
    [InlineData("members")]
    public async Task NewGroupOperations_ServerHasBasePath_PreserveBasePath(string operation)
    {
        var (sut, handler) = CreateSut("https://example.com/auth/");
        handler.AddResponse(HttpStatusCode.OK, operation == "by-path" ? ChildGroupResponse : "[]");

        switch (operation)
        {
            case "create-child": await sut.CreateChildGroupAsync(TestData.GroupId, new CreateGroupRequestDto { Name = "x" }); break;
            case "children":     await sut.GetChildGroupsAsync(TestData.GroupId); break;
            case "by-path":      await sut.GetGroupByPathAsync("/acme"); break;
            case "members":      await sut.GetGroupMembersAsync(TestData.GroupId); break;
        }

        Assert.StartsWith($"/auth/admin/realms/{TestData.RealmName}/", handler.SentRequests.Single().RequestUri!.AbsolutePath);
    }
}
