using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Authorization;
using Keycloak.Net.Sdk.Authorization.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class AuthorizationManagementTests
{
    private const string ClientUuid = "rs-uuid";

    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (AuthorizationManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new AuthorizationManagement(factory, _options), handler);
    }

    private static string Server => $"/admin/realms/{TestData.RealmName}/clients/{ClientUuid}/authz/resource-server";
    private static string TokenPath => $"/realms/{TestData.RealmName}/protocol/openid-connect/token";

    private static async Task<JsonElement> BodyAsync(HttpRequestMessage request)
    {
        using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static async Task<List<KeyValuePair<string, string>>> FormAsync(HttpRequestMessage request)
    {
        var form = await request.Content!.ReadAsStringAsync();
        return form.Split('&').Select(p => p.Split('=')).Select(kv =>
            new KeyValuePair<string, string>(Uri.UnescapeDataString(kv[0]), Uri.UnescapeDataString(kv[1].Replace('+', ' ')))).ToList();
    }

    // ── Resource server ───────────────────────────────────────────────────────

    [Fact]
    public async Task EnableAuthorizationAsync_MergesFlagsIntoClient()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """{ "id": "rs-uuid", "clientId": "erp", "publicClient": true, "redirectUris": ["https://erp/*"] }""");
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.EnableAuthorizationAsync(ClientUuid);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
        Assert.Equal($"/admin/realms/{TestData.RealmName}/clients/{ClientUuid}", handler.SentRequests[1].RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        var body = await BodyAsync(handler.SentRequests[1]);
        Assert.True(body.GetProperty("authorizationServicesEnabled").GetBoolean());
        Assert.True(body.GetProperty("serviceAccountsEnabled").GetBoolean());
        Assert.False(body.GetProperty("publicClient").GetBoolean());
        Assert.Equal("https://erp/*", body.GetProperty("redirectUris")[0].GetString());
    }

    [Fact]
    public async Task EnableAuthorizationAsync_ClientNotFound_DoesNotPut()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.EnableAuthorizationAsync(ClientUuid);

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task UpdateResourceServerAsync_ChangesOnlyGivenFields()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """{ "id": "rs", "clientId": "rs-uuid", "policyEnforcementMode": "ENFORCING", "decisionStrategy": "UNANIMOUS", "resources": [] }""");
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateResourceServerAsync(ClientUuid, new UpdateResourceServerRequestDto { DecisionStrategy = DecisionStrategies.Affirmative });

        Assert.Equal(Server, handler.SentRequests[1].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[1]);
        Assert.Equal("AFFIRMATIVE", body.GetProperty("decisionStrategy").GetString());
        Assert.Equal("ENFORCING", body.GetProperty("policyEnforcementMode").GetString());
        Assert.True(body.TryGetProperty("resources", out _));
    }

    [Fact]
    public async Task GetResourceServerAsync_ParsesSettings()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """{ "id": "rs", "clientId": "rs-uuid", "name": "erp", "policyEnforcementMode": "PERMISSIVE", "decisionStrategy": "AFFIRMATIVE", "allowRemoteResourceManagement": true }""");

        var result = await sut.GetResourceServerAsync(ClientUuid);

        Assert.Equal("PERMISSIVE", result.Response.PolicyEnforcementMode);
        Assert.True(result.Response.AllowRemoteResourceManagement);
    }

    // ── Scopes and resources ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateScopeAsync_PostsAndParsesCreatedScope()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created, """{ "id": "s1", "name": "approve" }""");

        var result = await sut.CreateScopeAsync(ClientUuid, new CreateAuthorizationScopeRequestDto { Name = "approve" });

        Assert.Equal("s1", result.Response.Id);
        Assert.Equal($"{Server}/scope", handler.SentRequests[0].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[0]);
        Assert.Equal("approve", body.GetProperty("name").GetString());
        Assert.False(body.TryGetProperty("displayName", out _));
    }

    [Fact]
    public async Task GetScopesAsync_WithName_SendsNameQuery()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await sut.GetScopesAsync(ClientUuid, "a b");

        Assert.Equal("?name=a%20b", handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task CreateResourceAsync_SendsScopesByName()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created, """{ "_id": "r1", "name": "invoice", "scopes": [ { "id": "s1", "name": "view" } ] }""");

        var result = await sut.CreateResourceAsync(ClientUuid, new CreateResourceRequestDto
        {
            Name   = "invoice",
            Type   = "urn:erp:resources:invoice",
            Scopes = ["view", "approve"]
        });

        Assert.Equal("r1", result.Response.Id);
        Assert.Equal("view", result.Response.Scopes![0].Name);
        var body = await BodyAsync(handler.SentRequests[0]);
        Assert.Equal(["view", "approve"], body.GetProperty("scopes").EnumerateArray().Select(s => s.GetProperty("name").GetString()).ToList());
        Assert.Equal("urn:erp:resources:invoice", body.GetProperty("type").GetString());
        Assert.False(body.TryGetProperty("uris", out _));
        Assert.False(body.TryGetProperty("Scopes", out _));
    }

    [Fact]
    public async Task GetResourcesAsync_SendsDeepAndFilters()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await sut.GetResourcesAsync(ClientUuid, new GetResourcesQueryDto { Name = "invoice", ExactName = true, Type = "t", First = 0, Max = 10 });

        Assert.Equal($"{Server}/resource", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal("?deep=true&name=invoice&exactName=true&type=t&first=0&max=10", handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task UpdateResourceAsync_MergesFields()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """{ "_id": "r1", "name": "invoice", "type": "t", "scopes": [ { "id": "s1", "name": "view" } ], "attributes": { "module": ["finance"] } }""");
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateResourceAsync(ClientUuid, "r1", new UpdateResourceRequestDto { DisplayName = "Invoices", Scopes = ["approve"] });

        Assert.Equal($"{Server}/resource/r1", handler.SentRequests[1].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[1]);
        Assert.Equal("Invoices", body.GetProperty("displayName").GetString());
        Assert.Equal("t", body.GetProperty("type").GetString());
        Assert.Equal("approve", Assert.Single(body.GetProperty("scopes").EnumerateArray()).GetProperty("name").GetString());
        Assert.Equal("finance", body.GetProperty("attributes").GetProperty("module")[0].GetString());
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("resource")]
    [InlineData("policy")]
    public async Task Deletes_UseEscapedIds(string kind)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = kind switch
        {
            "scope"    => await sut.DeleteScopeAsync(ClientUuid, "a/b"),
            "resource" => await sut.DeleteResourceAsync(ClientUuid, "a/b"),
            _          => await sut.DeletePermissionAsync(ClientUuid, "a/b")
        };

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.EndsWith($"/authz/resource-server/{kind}/a%2Fb", handler.SentRequests[0].RequestUri!.AbsoluteUri);
    }

    // ── Policies and permissions ──────────────────────────────────────────────

    [Fact]
    public async Task CreateRolePolicyAsync_PostsRoles()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created, """{ "id": "p1", "name": "accountants", "type": "role" }""");

        var result = await sut.CreateRolePolicyAsync(ClientUuid, new CreateRolePolicyRequestDto
        {
            Name  = "accountants",
            Roles = [new PolicyRole("role-1", Required: true)]
        });

        Assert.Equal("p1", result.Response.Id);
        Assert.Equal($"{Server}/policy/role", handler.SentRequests[0].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[0]);
        Assert.Equal("role-1", body.GetProperty("roles")[0].GetProperty("id").GetString());
        Assert.True(body.GetProperty("roles")[0].GetProperty("required").GetBoolean());
        Assert.Equal("POSITIVE", body.GetProperty("logic").GetString());
        Assert.Equal("UNANIMOUS", body.GetProperty("decisionStrategy").GetString());
        Assert.False(body.TryGetProperty("fetchRoles", out _));
    }

    [Fact]
    public async Task CreateGroupAndUserPolicies_UseTheirEndpoints()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created, """{ "id": "p1", "name": "g", "type": "group" }""");
        handler.AddResponse(HttpStatusCode.Created, """{ "id": "p2", "name": "u", "type": "user" }""");

        await sut.CreateGroupPolicyAsync(ClientUuid, new CreateGroupPolicyRequestDto { Name = "g", Groups = [new PolicyGroup("g1", ExtendChildren: true)] });
        await sut.CreateUserPolicyAsync(ClientUuid, new CreateUserPolicyRequestDto { Name = "u", Users = ["u1"], Logic = PolicyLogic.Negative });

        Assert.Equal($"{Server}/policy/group", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.True((await BodyAsync(handler.SentRequests[0])).GetProperty("groups")[0].GetProperty("extendChildren").GetBoolean());
        Assert.Equal($"{Server}/policy/user", handler.SentRequests[1].RequestUri!.AbsolutePath);
        var userBody = await BodyAsync(handler.SentRequests[1]);
        Assert.Equal("u1", userBody.GetProperty("users")[0].GetString());
        Assert.Equal("NEGATIVE", userBody.GetProperty("logic").GetString());
    }

    [Fact]
    public async Task GetPoliciesAsync_ExcludesPermissions()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """[ { "id": "p1", "name": "a", "type": "role", "config": { "roles": "[]" } } ]""");

        var result = await sut.GetPoliciesAsync(ClientUuid, new GetPoliciesQueryDto { Type = "role" });

        Assert.Equal("[]", result.Response[0].Config!["roles"]);
        Assert.Equal("?permission=false&type=role", handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task CreatePermissions_UseTheirEndpointsAndOmitUnsetResources()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created, """{ "id": "x1", "name": "a", "type": "resource" }""");
        handler.AddResponse(HttpStatusCode.Created, """{ "id": "x2", "name": "b", "type": "scope" }""");

        await sut.CreateResourcePermissionAsync(ClientUuid, new CreateResourcePermissionRequestDto
        {
            Name = "a", ResourceType = "urn:erp:resources:invoice", Policies = ["p1"]
        });
        await sut.CreateScopePermissionAsync(ClientUuid, new CreateScopePermissionRequestDto
        {
            Name = "b", Scopes = ["s1"], Policies = ["p1"], DecisionStrategy = DecisionStrategies.Affirmative
        });

        Assert.Equal($"{Server}/permission/resource", handler.SentRequests[0].RequestUri!.AbsolutePath);
        var resourceBody = await BodyAsync(handler.SentRequests[0]);
        Assert.Equal("urn:erp:resources:invoice", resourceBody.GetProperty("resourceType").GetString());
        Assert.False(resourceBody.TryGetProperty("resources", out _));

        Assert.Equal($"{Server}/permission/scope", handler.SentRequests[1].RequestUri!.AbsolutePath);
        var scopeBody = await BodyAsync(handler.SentRequests[1]);
        Assert.Equal("s1", scopeBody.GetProperty("scopes")[0].GetString());
        Assert.Equal("AFFIRMATIVE", scopeBody.GetProperty("decisionStrategy").GetString());
        Assert.False(scopeBody.TryGetProperty("resources", out _));
    }

    [Fact]
    public async Task GetPermissionsAsync_SendsResourceFilter()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await sut.GetPermissionsAsync(ClientUuid, new GetPermissionsQueryDto { Resource = "r1", Max = 5 });

        Assert.Equal($"{Server}/permission", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal("?resource=r1&max=5", handler.SentRequests[0].RequestUri!.Query);
    }

    // ── Evaluation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_PostsUserAndResources()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """
            { "status": "PERMIT", "results": [ { "resource": { "_id": "r1", "name": "invoice" }, "status": "PERMIT",
              "allowedScopes": [ { "id": "s1", "name": "view" } ], "policies": [] } ] }
            """);

        var result = await sut.EvaluateAsync(ClientUuid, "user-1", ["r1"]);

        Assert.Equal("PERMIT", result.Response.Status);
        Assert.Equal("view", result.Response.Results[0].AllowedScopes![0].Name);
        Assert.Equal($"{Server}/policy/evaluate", handler.SentRequests[0].RequestUri!.AbsolutePath);
        var body = await BodyAsync(handler.SentRequests[0]);
        Assert.Equal("user-1", body.GetProperty("userId").GetString());
        Assert.Equal("r1", body.GetProperty("resources")[0].GetProperty("_id").GetString());
    }

    [Fact]
    public async Task CheckPermissionAsync_SendsUmaRequestWithUserToken()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """{ "result": true }""");

        var result = await sut.CheckPermissionAsync("user-token", "erp-api", new PermissionRequest("invoice", "view", "approve"));

        Assert.True(result.IsSuccessful);
        Assert.True(result.Response.Granted);
        var request = handler.SentRequests.Single();
        Assert.Equal(TokenPath, request.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("user-token", request.Headers.Authorization.Parameter);
        var form = await FormAsync(request);
        Assert.Contains(new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"), form);
        Assert.Contains(new KeyValuePair<string, string>("audience", "erp-api"), form);
        Assert.Contains(new KeyValuePair<string, string>("permission", "invoice#view,approve"), form);
        Assert.Contains(new KeyValuePair<string, string>("response_mode", "decision"), form);
    }

    [Fact]
    public async Task CheckPermissionAsync_AccessDenied_IsSuccessfulAndNotGranted()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Forbidden, """{ "error": "access_denied", "error_description": "not_authorized" }""");

        var result = await sut.CheckPermissionAsync("user-token", "erp-api", new PermissionRequest("invoice", "approve"));

        Assert.True(result.IsSuccessful);
        Assert.False(result.Response.Granted);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, """{ "error": "insufficient_scope" }""")]
    [InlineData(HttpStatusCode.Unauthorized, """{ "error": "invalid_grant" }""")]
    [InlineData(HttpStatusCode.BadRequest, """{ "error": "invalid_resource" }""")]
    public async Task CheckPermissionAsync_OtherErrors_AreFailures(HttpStatusCode status, string body)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(status, body);

        var result = await sut.CheckPermissionAsync("user-token", "erp-api", new PermissionRequest("invoice", "view"));

        Assert.False(result.IsSuccessful);
        Assert.Equal(status, result.StatusCode);
        Assert.Contains("\"error\"", result.ErrorMessage);
    }

    [Fact]
    public async Task GetGrantedPermissionsAsync_ParsesPermissions_AndScopeOnlyRequests()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """[ { "rsid": "r1", "rsname": "invoice", "scopes": ["view"] } ]""");

        var result = await sut.GetGrantedPermissionsAsync("user-token", "erp-api",
            [new PermissionRequest("invoice"), new PermissionRequest(null, "approve")]);

        var grant = Assert.Single(result.Response);
        Assert.Equal("r1", grant.ResourceId);
        Assert.Equal(["view"], grant.Scopes);
        var form = await FormAsync(handler.SentRequests[0]);
        Assert.Equal(["invoice", "#approve"], form.Where(kv => kv.Key == "permission").Select(kv => kv.Value).ToList());
        Assert.Contains(new KeyValuePair<string, string>("response_mode", "permissions"), form);
    }

    [Fact]
    public async Task GetGrantedPermissionsAsync_NothingGranted_ReturnsEmptyList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Forbidden, """{ "error": "access_denied" }""");

        var result = await sut.GetGrantedPermissionsAsync("user-token", "erp-api");

        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Response);
        Assert.DoesNotContain("permission=", await handler.SentRequests[0].Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetRequestingPartyTokenAsync_OmitsResponseMode_AndReturnsToken()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """{ "access_token": "rpt", "expires_in": 300, "token_type": "Bearer" }""");

        var result = await sut.GetRequestingPartyTokenAsync("user-token", "erp-api");

        Assert.Equal("rpt", result.Response.AccessToken);
        Assert.DoesNotContain(await FormAsync(handler.SentRequests[0]), kv => kv.Key == "response_mode");
    }

    [Fact]
    public async Task Evaluation_RequiresTokenAndAudience()
    {
        var (sut, _) = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CheckPermissionAsync("", "erp-api", new PermissionRequest("invoice")));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetGrantedPermissionsAsync("token", ""));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.EvaluateAsync(ClientUuid, ""));
    }
}
