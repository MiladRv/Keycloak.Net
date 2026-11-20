using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Roles;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

/// <summary>Realm role attributes/update, client roles on groups, and effective (composite) role mappings.</summary>
public class RoleMappingTests
{
    private const string OtherClientUuid = "finance-client-uuid";

    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (RoleManagement Sut, FakeHttpMessageHandler Handler) CreateSut(string baseAddress = "http://localhost:8080/")
    {
        var (factory, handler) = HttpClientFactoryHelper.Create(baseAddress);
        return (new RoleManagement(factory, _options), handler);
    }

    private static string Realm => $"/admin/realms/{TestData.RealmName}";

    // ── Realm role attributes and update ──────────────────────────────────────

    [Fact]
    public async Task CreateRealmRoleAsync_WithAttributes_SendsAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        await sut.CreateRealmRoleAsync(new CreateRealmRoleRequestDto
        {
            Name       = "branch-manager",
            Attributes = new() { ["scope_level"] = ["branch"] }
        });

        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.Equal("branch", body.RootElement.GetProperty("attributes").GetProperty("scope_level")[0].GetString());
    }

    [Fact]
    public async Task CreateRealmRoleAsync_WithoutAttributes_OmitsAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        await sut.CreateRealmRoleAsync(new CreateRealmRoleRequestDto { Name = "branch-manager" });

        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.False(body.RootElement.TryGetProperty("attributes", out _));
    }

    [Fact]
    public async Task GetRealmRoleAsync_ParsesAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """
            { "id": "r1", "name": "branch-manager", "composite": false, "clientRole": false,
              "containerId": "realm", "attributes": { "scope_level": ["branch"] } }
            """);

        var result = await sut.GetRealmRoleAsync("branch-manager");

        Assert.Equal(["branch"], result.Response.Attributes!["scope_level"]);
    }

    [Fact]
    public async Task UpdateRealmRoleAsync_PutsRoleWithoutNullAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.UpdateRealmRoleAsync("branch-manager",
            new UpdateRealmRoleRequestDto { Name = "branch-manager", Description = "Manages one branch" });

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        Assert.Equal($"{Realm}/roles/branch-manager", handler.SentRequests[0].RequestUri!.AbsolutePath);
        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.Equal("Manages one branch", body.RootElement.GetProperty("description").GetString());
        Assert.False(body.RootElement.TryGetProperty("attributes", out _));
    }

    [Fact]
    public async Task UpdateRealmRoleAsync_WithAttributes_SendsAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateRealmRoleAsync("branch-manager", new UpdateRealmRoleRequestDto
        {
            Name       = "branch-manager",
            Attributes = new() { ["scope_level"] = ["company"] }
        });

        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.Equal("company", body.RootElement.GetProperty("attributes").GetProperty("scope_level")[0].GetString());
    }

    [Theory]
    [InlineData("get")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task RealmRoleName_WithReservedCharacters_IsEscaped(string operation)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, operation == "get" ? TestData.RealmRoleResponse : null);

        const string name = "sales/approve #1?";
        switch (operation)
        {
            case "get":    await sut.GetRealmRoleAsync(name); break;
            case "update": await sut.UpdateRealmRoleAsync(name, new UpdateRealmRoleRequestDto { Name = name }); break;
            case "delete": await sut.DeleteRealmRoleAsync(name); break;
        }

        Assert.Equal($"{Realm}/roles/sales%2Fapprove%20%231%3F", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, handler.SentRequests[0].RequestUri!.Query);
    }

    // ── Client roles ↔ groups, effective roles: routing ───────────────────────

    public static TheoryData<string, string, string> Operations => new()
    {
        { "group-client-roles",     "GET",    $"/groups/{TestData.GroupId}/role-mappings/clients/{{client}}" },
        { "assign-group",           "POST",   $"/groups/{TestData.GroupId}/role-mappings/clients/{{client}}" },
        { "remove-group",           "DELETE", $"/groups/{TestData.GroupId}/role-mappings/clients/{{client}}" },
        { "user-effective-client",  "GET",    $"/users/{TestData.UserId}/role-mappings/clients/{{client}}/composite" },
        { "group-effective-client", "GET",    $"/groups/{TestData.GroupId}/role-mappings/clients/{{client}}/composite" },
    };

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task ExplicitClientOverload_TargetsGivenClient(string operation, string method, string pathTemplate)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await InvokeAsync(sut, operation, OtherClientUuid);

        Assert.Equal(method, handler.SentRequests.Single().Method.Method);
        Assert.Equal(Realm + pathTemplate.Replace("{client}", OtherClientUuid), handler.SentRequests[0].RequestUri!.AbsolutePath);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task ConfiguredClientOverload_TargetsConfiguredClient(string operation, string method, string pathTemplate)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await InvokeAsync(sut, operation, clientUuid: null);

        Assert.Equal(method, handler.SentRequests.Single().Method.Method);
        Assert.Equal(Realm + pathTemplate.Replace("{client}", TestData.ClientUuid), handler.SentRequests[0].RequestUri!.AbsolutePath);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task ExplicitClientOverload_ServerHasBasePath_PreservesBasePath(string operation, string method, string pathTemplate)
    {
        var (sut, handler) = CreateSut("https://example.com/auth/");
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await InvokeAsync(sut, operation, OtherClientUuid);

        Assert.Equal(method, handler.SentRequests.Single().Method.Method);
        Assert.Equal("/auth" + Realm + pathTemplate.Replace("{client}", OtherClientUuid), handler.SentRequests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AssignClientRoleToGroupAsync_SendsRoleInBody()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.AssignClientRoleToGroupAsync(OtherClientUuid, TestData.GroupId, TestData.RoleId, TestData.RoleName);

        Assert.True(result.IsSuccessful);
        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        var role = Assert.Single(body.RootElement.EnumerateArray());
        Assert.Equal(TestData.RoleId, role.GetProperty("id").GetString());
        Assert.Equal(TestData.RoleName, role.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetGroupClientRolesAsync_ReturnsRoles()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientRolesResponse);

        var result = await sut.GetGroupClientRolesAsync(OtherClientUuid, TestData.GroupId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.RoleName, Assert.Single(result.Response).Name);
    }

    [Fact]
    public async Task GetUserEffectiveRealmRolesAsync_GetsRealmComposite()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmRolesResponse);

        var result = await sut.GetUserEffectiveRealmRolesAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.NotEmpty(result.Response);
        Assert.Equal($"{Realm}/users/{TestData.UserId}/role-mappings/realm/composite", handler.SentRequests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetGroupEffectiveRealmRolesAsync_GetsRealmComposite()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.RealmRolesResponse);

        var result = await sut.GetGroupEffectiveRealmRolesAsync(TestData.GroupId);

        Assert.True(result.IsSuccessful);
        Assert.Equal($"{Realm}/groups/{TestData.GroupId}/role-mappings/realm/composite", handler.SentRequests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetUserEffectiveClientRolesAsync_Failure_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound, """{"error":"User not found"}""");

        var result = await sut.GetUserEffectiveClientRolesAsync(OtherClientUuid, "missing");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    private static Task InvokeAsync(RoleManagement sut, string operation, string? clientUuid)
    {
        var g = TestData.GroupId;
        var u = TestData.UserId;
        var id = TestData.RoleId;
        var r = TestData.RoleName;

        return (operation, clientUuid) switch
        {
            ("group-client-roles", null)      => sut.GetGroupClientRolesAsync(g),
            ("group-client-roles", var c)     => sut.GetGroupClientRolesAsync(c, g),
            ("assign-group", null)            => sut.AssignClientRoleToGroupAsync(g, id, r),
            ("assign-group", var c)           => sut.AssignClientRoleToGroupAsync(c, g, id, r),
            ("remove-group", null)            => sut.RemoveClientRoleFromGroupAsync(g, id, r),
            ("remove-group", var c)           => sut.RemoveClientRoleFromGroupAsync(c, g, id, r),
            ("user-effective-client", null)   => sut.GetUserEffectiveClientRolesAsync(u),
            ("user-effective-client", var c)  => sut.GetUserEffectiveClientRolesAsync(c, u),
            ("group-effective-client", null)  => sut.GetGroupEffectiveClientRolesAsync(g),
            ("group-effective-client", var c) => sut.GetGroupEffectiveClientRolesAsync(c, g),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }
}
