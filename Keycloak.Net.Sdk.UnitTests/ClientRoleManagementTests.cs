using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Roles;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

/// <summary>
/// Client-role operations against an explicit client (clientUuid overloads) and client role CRUD.
/// </summary>
public class ClientRoleManagementTests
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

    private static string Path(FakeHttpMessageHandler handler) => handler.SentRequests.Single().RequestUri!.AbsolutePath;

    private static string ClientRolesPath(string clientUuid) =>
        $"/admin/realms/{TestData.RealmName}/clients/{clientUuid}/roles";

    // ── Explicit client overloads target the given client ─────────────────────

    public static TheoryData<string, string, string> ExplicitClientOperations => new()
    {
        { "get-roles",         "GET",    ClientRolesPath(OtherClientUuid) },
        { "get-role",          "GET",    $"{ClientRolesPath(OtherClientUuid)}/{TestData.RoleName}" },
        { "create-role",       "POST",   ClientRolesPath(OtherClientUuid) },
        { "update-role",       "PUT",    $"{ClientRolesPath(OtherClientUuid)}/{TestData.RoleName}" },
        { "delete-role",       "DELETE", $"{ClientRolesPath(OtherClientUuid)}/{TestData.RoleName}" },
        { "role-users",        "GET",    $"{ClientRolesPath(OtherClientUuid)}/{TestData.RoleName}/users" },
        { "user-roles",        "GET",    $"/admin/realms/{TestData.RealmName}/users/{TestData.UserId}/role-mappings/clients/{OtherClientUuid}" },
        { "assign",            "POST",   $"/admin/realms/{TestData.RealmName}/users/{TestData.UserId}/role-mappings/clients/{OtherClientUuid}" },
        { "remove",            "DELETE", $"/admin/realms/{TestData.RealmName}/users/{TestData.UserId}/role-mappings/clients/{OtherClientUuid}" },
        { "get-composites",    "GET",    $"{ClientRolesPath(OtherClientUuid)}/{TestData.RoleName}/composites" },
        { "add-composites",    "POST",   $"{ClientRolesPath(OtherClientUuid)}/{TestData.RoleName}/composites" },
        { "remove-composites", "DELETE", $"{ClientRolesPath(OtherClientUuid)}/{TestData.RoleName}/composites" },
    };

    [Theory]
    [MemberData(nameof(ExplicitClientOperations))]
    public async Task ExplicitClientOverload_TargetsGivenClient(string operation, string method, string expectedPath)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, operation == "get-role" ? SingleRoleResponse : "[]");

        await InvokeAsync(sut, operation, OtherClientUuid);

        Assert.Equal(method, handler.SentRequests.Single().Method.Method);
        Assert.Equal(expectedPath, Path(handler));
    }

    [Theory]
    [MemberData(nameof(ExplicitClientOperations))]
    public async Task ConfiguredClientOverload_TargetsConfiguredClient(string operation, string method, string expectedPath)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, operation == "get-role" ? SingleRoleResponse : "[]");

        await InvokeAsync(sut, operation, clientUuid: null);

        Assert.Equal(method, handler.SentRequests.Single().Method.Method);
        Assert.Equal(expectedPath.Replace(OtherClientUuid, TestData.ClientUuid), Path(handler));
    }

    [Theory]
    [MemberData(nameof(ExplicitClientOperations))]
    public async Task ExplicitClientOverload_ServerHasBasePath_PreservesBasePath(string operation, string method, string expectedPath)
    {
        var (sut, handler) = CreateSut("https://example.com/auth/");
        handler.AddResponse(HttpStatusCode.OK, operation == "get-role" ? SingleRoleResponse : "[]");

        await InvokeAsync(sut, operation, OtherClientUuid);

        Assert.Equal(method, handler.SentRequests.Single().Method.Method);
        Assert.Equal($"/auth{expectedPath}", Path(handler));
    }

    // ── Client role CRUD ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetClientRoleAsync_Success_ReturnsRoleWithAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, SingleRoleResponse);

        var result = await sut.GetClientRoleAsync(OtherClientUuid, TestData.RoleName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.RoleId, result.Response.Id);
        Assert.Equal(TestData.RoleName, result.Response.Name);
        Assert.NotNull(result.Response.Attributes);
        Assert.Equal(["finance"], result.Response.Attributes!["module"]);
    }

    [Fact]
    public async Task GetClientRoleAsync_NotFound_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound, """{"error":"Could not find role"}""");

        var result = await sut.GetClientRoleAsync(OtherClientUuid, "missing");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task CreateClientRoleAsync_SendsNameDescriptionAndAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        var result = await sut.CreateClientRoleAsync(OtherClientUuid, new CreateClientRoleRequestDto
        {
            Name        = "invoice-approve",
            Description = "Approve invoices",
            Attributes  = new() { ["module"] = ["finance"] }
        });

        Assert.True(result.IsSuccessful);
        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.Equal("invoice-approve", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("Approve invoices", body.RootElement.GetProperty("description").GetString());
        Assert.Equal("finance", body.RootElement.GetProperty("attributes").GetProperty("module")[0].GetString());
    }

    [Fact]
    public async Task CreateClientRoleAsync_NameOnly_OmitsNullFields()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        await sut.CreateClientRoleAsync(new CreateClientRoleRequestDto { Name = "invoice-view" });

        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.False(body.RootElement.TryGetProperty("description", out _));
        Assert.False(body.RootElement.TryGetProperty("attributes", out _));
    }

    [Fact]
    public async Task CreateClientRoleAsync_Conflict_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Conflict, """{"errorMessage":"Role with name invoice-view already exists"}""");

        var result = await sut.CreateClientRoleAsync(OtherClientUuid, new CreateClientRoleRequestDto { Name = "invoice-view" });

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task UpdateClientRoleAsync_WithoutAttributes_DoesNotSendAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.UpdateClientRoleAsync(OtherClientUuid, TestData.RoleName,
            new UpdateClientRoleRequestDto { Name = TestData.RoleName, Description = "Updated" });

        Assert.True(result.IsSuccessful);
        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.Equal("Updated", body.RootElement.GetProperty("description").GetString());
        Assert.False(body.RootElement.TryGetProperty("attributes", out _));
    }

    [Fact]
    public async Task UpdateClientRoleAsync_WithEmptyAttributes_SendsEmptyObjectToClearThem()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateClientRoleAsync(OtherClientUuid, TestData.RoleName,
            new UpdateClientRoleRequestDto { Name = TestData.RoleName, Attributes = [] });

        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Object, body.RootElement.GetProperty("attributes").ValueKind);
        Assert.Empty(body.RootElement.GetProperty("attributes").EnumerateObject());
    }

    [Fact]
    public async Task DeleteClientRoleAsync_Success_SendsNoBody()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DeleteClientRoleAsync(OtherClientUuid, TestData.RoleName);

        Assert.True(result.IsSuccessful);
        Assert.Null(handler.SentRequests[0].Content);
    }

    [Theory]
    [InlineData(null, null, "")]
    [InlineData(10, null, "?first=10")]
    [InlineData(null, 5, "?max=5")]
    [InlineData(20, 10, "?first=20&max=10")]
    public async Task GetClientRoleUsersAsync_AddsPaginationQuery(int? first, int? max, string expectedQuery)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var result = await sut.GetClientRoleUsersAsync(OtherClientUuid, TestData.RoleName, first, max);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.UserId, Assert.Single(result.Response).Id);
        Assert.Equal(expectedQuery, handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task GetUserClientRolesAsync_Success_ReturnsRoles()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.ClientRolesResponse);

        var result = await sut.GetUserClientRolesAsync(OtherClientUuid, TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.RoleName, Assert.Single(result.Response).Name);
    }

    [Fact]
    public async Task AssignClientRoleToUserAsync_ExplicitClient_SendsRoleInBody()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.AssignClientRoleToUserAsync(OtherClientUuid, TestData.UserId, TestData.RoleId, TestData.RoleName);

        using var body = JsonDocument.Parse(await handler.SentRequests[0].Content!.ReadAsStringAsync());
        var role = Assert.Single(body.RootElement.EnumerateArray());
        Assert.Equal(TestData.RoleId, role.GetProperty("id").GetString());
        Assert.Equal(TestData.RoleName, role.GetProperty("name").GetString());
    }

    // ── Escaping ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RoleNameWithReservedCharacters_IsEscapedAsSinglePathSegment()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, SingleRoleResponse);

        await sut.GetClientRoleAsync(OtherClientUuid, "sales/approve #1?");

        Assert.Equal(
            $"{ClientRolesPath(OtherClientUuid)}/sales%2Fapprove%20%231%3F",
            handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, handler.SentRequests[0].RequestUri!.Query);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string SingleRoleResponse => $$"""
        {
            "id": "{{TestData.RoleId}}",
            "name": "{{TestData.RoleName}}",
            "description": "Test role",
            "composite": false,
            "clientRole": true,
            "containerId": "{{OtherClientUuid}}",
            "attributes": { "module": ["finance"] }
        }
        """;

    private static Task InvokeAsync(RoleManagement sut, string operation, string? clientUuid)
    {
        var composites = new List<CompositeRoleRequestDto> { new() { Id = TestData.RoleId, Name = TestData.RoleName } };
        var create = new CreateClientRoleRequestDto { Name = TestData.RoleName };
        var update = new UpdateClientRoleRequestDto { Name = TestData.RoleName };
        var u = TestData.UserId;
        var r = TestData.RoleName;
        var id = TestData.RoleId;

        return (operation, clientUuid) switch
        {
            ("get-roles", null)         => sut.GetClientRolesAsync(),
            ("get-roles", var c)        => sut.GetClientRolesAsync(c),
            ("get-role", null)          => sut.GetClientRoleAsync(r),
            ("get-role", var c)         => sut.GetClientRoleAsync(c, r),
            ("create-role", null)       => sut.CreateClientRoleAsync(create),
            ("create-role", var c)      => sut.CreateClientRoleAsync(c, create),
            ("update-role", null)       => sut.UpdateClientRoleAsync(r, update),
            ("update-role", var c)      => sut.UpdateClientRoleAsync(c, r, update),
            ("delete-role", null)       => sut.DeleteClientRoleAsync(r),
            ("delete-role", var c)      => sut.DeleteClientRoleAsync(c, r),
            ("role-users", null)        => sut.GetClientRoleUsersAsync(r),
            ("role-users", var c)       => sut.GetClientRoleUsersAsync(c, r),
            ("user-roles", null)        => sut.GetUserClientRolesAsync(u),
            ("user-roles", var c)       => sut.GetUserClientRolesAsync(c, u),
            ("assign", null)            => sut.AssignClientRoleToUserAsync(u, id, r),
            ("assign", var c)           => sut.AssignClientRoleToUserAsync(c, u, id, r),
            ("remove", null)            => sut.RemoveClientRoleFromUserAsync(u, id, r),
            ("remove", var c)           => sut.RemoveClientRoleFromUserAsync(c, u, id, r),
            ("get-composites", null)    => sut.GetClientRoleCompositesAsync(r),
            ("get-composites", var c)   => sut.GetClientRoleCompositesAsync(c, r),
            ("add-composites", null)    => sut.AddClientRoleCompositesAsync(r, composites),
            ("add-composites", var c)   => sut.AddClientRoleCompositesAsync(c, r, composites),
            ("remove-composites", null) => sut.RemoveClientRoleCompositesAsync(r, composites),
            ("remove-composites", var c)=> sut.RemoveClientRoleCompositesAsync(c, r, composites),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }
}
