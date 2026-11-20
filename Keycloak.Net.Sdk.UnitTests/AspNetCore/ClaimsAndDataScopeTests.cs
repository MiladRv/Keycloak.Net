using System.Security.Claims;
using Keycloak.Net.Sdk.AspNetCore;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests.AspNetCore;

public class ClaimsAndDataScopeTests
{
    private const string RealmAccess    = """{"roles":["accountant","offline_access"]}""";
    private const string ResourceAccess = """{"finance":{"roles":["invoice-approve"]},"erp-api":{"roles":["report-view","report-export"]}}""";

    private static ClaimsPrincipal User(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test", "preferred_username", ClaimTypes.Role));

    private static Claim Json(string type, string json) => new(type, json, "JSON");

    private static ClaimsPrincipal KeycloakUser(params Claim[] extra) =>
        User([new Claim("sub", "user-1"), Json("realm_access", RealmAccess), Json("resource_access", ResourceAccess), .. extra]);

    // ── Roles ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetRealmRoles_ReadsRealmAccess()
    {
        Assert.Equal(["accountant", "offline_access"], KeycloakUser().GetRealmRoles());
    }

    [Fact]
    public void GetClientRoles_ReadsOneClient()
    {
        var user = KeycloakUser();

        Assert.Equal(["invoice-approve"], user.GetClientRoles("finance"));
        Assert.Equal(["report-view", "report-export"], user.GetClientRoles("erp-api"));
        Assert.Empty(user.GetClientRoles("inventory"));
    }

    [Fact]
    public void GetAllClientRoles_ReturnsEveryClient()
    {
        var all = KeycloakUser().GetAllClientRoles();

        Assert.Equal(["erp-api", "finance"], all.Keys.Order().ToList());
        Assert.Equal(["invoice-approve"], all["finance"]);
    }

    [Fact]
    public void HasRealmRole_AndHasClientRole_AreCaseSensitiveAndSourceSpecific()
    {
        var user = KeycloakUser();

        Assert.True(user.HasRealmRole("accountant"));
        Assert.False(user.HasRealmRole("Accountant"));
        Assert.False(user.HasRealmRole("invoice-approve"));
        Assert.True(user.HasClientRole("finance", "invoice-approve"));
        Assert.False(user.HasClientRole("erp-api", "invoice-approve"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("""{"roles":"accountant"}""")]
    [InlineData("""{"roles":[1, null, "accountant"]}""")]
    public void GetRealmRoles_MalformedOrUnexpectedJson_DoesNotThrow(string json)
    {
        var roles = User(Json("realm_access", json)).GetRealmRoles();

        Assert.True(roles.Count == 0 || roles.SequenceEqual(["accountant"]));
    }

    [Fact]
    public void RoleReaders_WithoutKeycloakClaims_ReturnEmpty()
    {
        var user = User();

        Assert.Empty(user.GetRealmRoles());
        Assert.Empty(user.GetClientRoles("finance"));
        Assert.Empty(user.GetAllClientRoles());
        Assert.Null(user.GetKeycloakUserId());
    }

    [Fact]
    public void GetKeycloakUserId_ReadsSubOrNameIdentifier()
    {
        Assert.Equal("user-1", KeycloakUser().GetKeycloakUserId());
        Assert.Equal("user-2", User(new Claim(ClaimTypes.NameIdentifier, "user-2")).GetKeycloakUserId());
    }

    // ── Attribute values ─────────────────────────────────────────────────────

    [Fact]
    public void GetAttributeValues_RepeatedClaims_AreAllReturned()
    {
        var user = User(new Claim("branch_id", "12"), new Claim("branch_id", "13"), new Claim("branch_id", "12"));

        Assert.Equal(["12", "13"], user.GetAttributeValues("branch_id"));
    }

    [Fact]
    public void GetAttributeValues_JsonArray_IsFlattened()
    {
        var user = User(Json("branch_id", """["12", 13, null]"""));

        Assert.Equal(["12", "13"], user.GetAttributeValues("branch_id"));
    }

    [Fact]
    public void GetAttributeValues_TextThatLooksLikeArrayButIsNot_IsKeptAsIs()
    {
        var user = User(new Claim("label", "[draft"));

        Assert.Equal(["[draft"], user.GetAttributeValues("label"));
    }

    // ── DataScope ────────────────────────────────────────────────────────────

    [Fact]
    public void GetDataScope_RegularUser_IsLimitedToTokenValues()
    {
        var scope = KeycloakUser(new Claim("branch_id", "12")).GetDataScope("branch_id", "erp-admin");

        Assert.False(scope.IsUnrestricted);
        Assert.Equal(["12"], scope.Values);
        Assert.True(scope.Allows("12"));
        Assert.False(scope.Allows("13"));
    }

    [Fact]
    public void GetDataScope_RealmAdminRole_IsUnrestricted()
    {
        var user = User(Json("realm_access", """{"roles":["erp-admin"]}"""));

        var scope = user.GetDataScope("branch_id", "erp-admin");

        Assert.True(scope.IsUnrestricted);
        Assert.True(scope.Allows("anything"));
    }

    [Fact]
    public void GetDataScope_AdminRoleClaim_IsUnrestricted()
    {
        var scope = User(new Claim(ClaimTypes.Role, "finance-admin")).GetDataScope("branch_id", "erp-admin", "finance-admin");

        Assert.True(scope.IsUnrestricted);
    }

    [Fact]
    public void GetDataScope_NoValues_AllowsNothing()
    {
        var scope = KeycloakUser().GetDataScope("branch_id", "erp-admin");

        Assert.False(scope.IsUnrestricted);
        Assert.Empty(scope.Values);
        Assert.False(scope.Allows("12"));
    }

    [Fact]
    public void GetDataScope_Anonymous_AllowsNothingEvenWithAdminRoleClaim()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "erp-admin")]));

        var scope = anonymous.GetDataScope("branch_id", "erp-admin");

        Assert.Same(DataScope.None, scope);
    }

    [Fact]
    public void ValuesAs_SkipsValuesThatDoNotParse()
    {
        var scope = DataScope.Of(["12", "x", "13", " "]);

        Assert.Equal([12, 13], scope.ValuesAs<int>());
    }

    // ── WhereInScope ─────────────────────────────────────────────────────────

    private sealed record Invoice(int Id, int BranchId, string Region, Guid CompanyId);

    private static readonly Guid CompanyA = Guid.Parse("8a5d5c2e-0b5f-4d0e-9a51-0f4f7f6b1a01");
    private static readonly Guid CompanyB = Guid.Parse("8a5d5c2e-0b5f-4d0e-9a51-0f4f7f6b1a02");

    private static IQueryable<Invoice> Invoices() => new[]
    {
        new Invoice(1, 12, "north", CompanyA),
        new Invoice(2, 13, "south", CompanyA),
        new Invoice(3, 14, "north", CompanyB)
    }.AsQueryable();

    [Fact]
    public void WhereInScope_Unrestricted_ReturnsQueryUnchanged()
    {
        var query = Invoices();

        Assert.Same(query, query.WhereInScope(DataScope.Unrestricted, i => i.BranchId));
    }

    [Fact]
    public void WhereInScope_IntKey_FiltersRows()
    {
        var result = Invoices().WhereInScope(DataScope.Of(["12", "14", "bad"]), i => i.BranchId).Select(i => i.Id);

        Assert.Equal([1, 3], result.ToList());
    }

    [Fact]
    public void WhereInScope_StringAndGuidKeys_FilterRows()
    {
        Assert.Equal([1, 3], Invoices().WhereInScope(DataScope.Of(["north"]), i => i.Region).Select(i => i.Id).ToList());
        Assert.Equal([3], Invoices().WhereInScope(DataScope.Of([CompanyB.ToString()]), i => i.CompanyId).Select(i => i.Id).ToList());
    }

    [Fact]
    public void WhereInScope_EmptyScope_ReturnsNoRows()
    {
        Assert.Empty(Invoices().WhereInScope(DataScope.None, i => i.BranchId));
    }

    [Fact]
    public void WhereInScope_BuildsContainsOverCapturedList()
    {
        var expression = Invoices().WhereInScope(DataScope.Of(["12"]), i => i.BranchId).Expression.ToString();

        Assert.Contains("Contains(i.BranchId)", expression);
        Assert.Contains("value(", expression); // captured closure, not an inlined constant
    }
}
