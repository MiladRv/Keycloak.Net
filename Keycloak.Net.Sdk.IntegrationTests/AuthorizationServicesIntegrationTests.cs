using Keycloak.Net.Sdk.Authorization.Contracts;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// An ERP resource server: an <c>invoice</c> resource with <c>view</c> and <c>approve</c> scopes.
/// Accountants (realm role) may view; members of the managers group may approve.
/// </summary>
[Collection(nameof(KeycloakCollection))]
public class AuthorizationServicesIntegrationTests(KeycloakFixture fixture)
{
    private IServiceProvider Scoped => fixture.Services.CreateScope().ServiceProvider;
    private IAuthorizationManagement Authz => Scoped.GetRequiredService<IAuthorizationManagement>();
    private IClientManagement Client => Scoped.GetRequiredService<IClientManagement>();
    private IRoleManagement Role => Scoped.GetRequiredService<IRoleManagement>();
    private IGroupManagement Group => Scoped.GetRequiredService<IGroupManagement>();
    private IUserManagement User => Scoped.GetRequiredService<IUserManagement>();

    private sealed record ResourceServer(string ClientId, string ClientUuid, ResourceResponseDto Invoice,
        AuthorizationScopeResponseDto View, AuthorizationScopeResponseDto Approve);

    private async Task<string> CreateClientAsync(string clientId)
    {
        Assert.True((await Client.CreateClientAsync(new CreateClientRequestDto { ClientId = clientId, Name = clientId })).IsSuccessful);
        return (await Client.GetClientsAsync()).Response.Single(c => c.ClientId == clientId).Id;
    }

    private async Task<ResourceServer> CreateResourceServerAsync(string run)
    {
        var clientId = $"erp-authz-{run}";
        var clientUuid = await CreateClientAsync(clientId);

        var enable = await Authz.EnableAuthorizationAsync(clientUuid);
        Assert.True(enable.IsSuccessful, enable.ErrorMessage);

        var view = await Authz.CreateScopeAsync(clientUuid, new CreateAuthorizationScopeRequestDto { Name = "view" });
        Assert.True(view.IsSuccessful, view.ErrorMessage);
        var approve = await Authz.CreateScopeAsync(clientUuid, new CreateAuthorizationScopeRequestDto { Name = "approve", DisplayName = "Approve" });
        Assert.True(approve.IsSuccessful, approve.ErrorMessage);

        var invoice = await Authz.CreateResourceAsync(clientUuid, new CreateResourceRequestDto
        {
            Name       = "invoice",
            Type       = "urn:erp:resources:invoice",
            Uris       = ["/invoices/*"],
            Scopes     = ["view", "approve"],
            Attributes = new() { ["module"] = ["finance"] }
        });
        Assert.True(invoice.IsSuccessful, invoice.ErrorMessage);

        return new ResourceServer(clientId, clientUuid, invoice.Response, view.Response, approve.Response);
    }

    private async Task<string> SignInAsync()
    {
        var signin = await User.SigninAsync(KeycloakFixture.TestUsername, KeycloakFixture.TestPassword);
        Assert.True(signin.IsSuccessful, signin.ErrorMessage);
        return signin.Response.AccessToken!;
    }

    [Fact]
    public async Task ResourceServer_ScopesAndResources_CanBeManaged()
    {
        var server = await CreateResourceServerAsync(Guid.NewGuid().ToString("N"));
        try
        {
            var settings = await Authz.GetResourceServerAsync(server.ClientUuid);
            Assert.True(settings.IsSuccessful, settings.ErrorMessage);
            Assert.Equal(server.ClientUuid, settings.Response.ClientId);

            Assert.True((await Authz.UpdateResourceServerAsync(server.ClientUuid, new UpdateResourceServerRequestDto
            {
                PolicyEnforcementMode = PolicyEnforcementModes.Permissive,
                DecisionStrategy      = DecisionStrategies.Affirmative
            })).IsSuccessful);
            var updatedSettings = (await Authz.GetResourceServerAsync(server.ClientUuid)).Response;
            Assert.Equal(PolicyEnforcementModes.Permissive, updatedSettings.PolicyEnforcementMode);
            Assert.Equal(DecisionStrategies.Affirmative, updatedSettings.DecisionStrategy);

            var scopes = await Authz.GetScopesAsync(server.ClientUuid);
            Assert.Equal(["approve", "view"], scopes.Response.Select(s => s.Name).Order().ToList());
            Assert.Single((await Authz.GetScopesAsync(server.ClientUuid, "approve")).Response);

            Assert.Equal("invoice", server.Invoice.Name);
            Assert.False(string.IsNullOrEmpty(server.Invoice.Id));
            var found = await Authz.GetResourcesAsync(server.ClientUuid, new GetResourcesQueryDto { Name = "invoice", ExactName = true });
            Assert.True(found.IsSuccessful, found.ErrorMessage);
            var listed = Assert.Single(found.Response);
            Assert.Equal(["approve", "view"], listed.Scopes!.Select(s => s.Name).Order().ToList());
            Assert.Single((await Authz.GetResourcesAsync(server.ClientUuid, new GetResourcesQueryDto { Type = "urn:erp:resources:invoice" })).Response);

            Assert.True((await Authz.UpdateResourceAsync(server.ClientUuid, server.Invoice.Id, new UpdateResourceRequestDto
            {
                DisplayName = "Invoices",
                Scopes      = ["view"]
            })).IsSuccessful);
            var updated = (await Authz.GetResourceAsync(server.ClientUuid, server.Invoice.Id)).Response;
            Assert.Equal("Invoices", updated.DisplayName);
            Assert.Equal("urn:erp:resources:invoice", updated.Type);
            Assert.Equal(["finance"], updated.Attributes!["module"]);
            Assert.Equal(["view"], updated.Scopes!.Select(s => s.Name).ToList());

            Assert.True((await Authz.DeleteResourceAsync(server.ClientUuid, server.Invoice.Id)).IsSuccessful);
            Assert.False((await Authz.GetResourceAsync(server.ClientUuid, server.Invoice.Id)).IsSuccessful);
            Assert.True((await Authz.DeleteScopeAsync(server.ClientUuid, server.Approve.Id)).IsSuccessful);
            Assert.Equal(["view"], (await Authz.GetScopesAsync(server.ClientUuid)).Response.Select(s => s.Name).ToList());
        }
        finally
        {
            await Client.DeleteClientAsync(server.ClientUuid);
        }
    }

    [Fact]
    public async Task ScopePermissions_GrantViewToRoleAndApproveToGroup_AndAreEvaluated()
    {
        var run = Guid.NewGuid().ToString("N");
        var server = await CreateResourceServerAsync(run);
        var accountantRole = $"accountant-{run}";
        var managersGroup = $"managers-{run}";
        string? groupId = null;
        RealmRoleResponseDto? role = null;
        try
        {
            Assert.True((await Role.CreateRealmRoleAsync(new CreateRealmRoleRequestDto { Name = accountantRole })).IsSuccessful);
            role = (await Role.GetRealmRoleAsync(accountantRole)).Response;
            Assert.True((await Group.CreateGroupAsync(new CreateGroupRequestDto { Name = managersGroup })).IsSuccessful);
            groupId = (await Group.GetGroupByPathAsync(managersGroup)).Response.Id;

            // Policies
            var accountants = await Authz.CreateRolePolicyAsync(server.ClientUuid, new CreateRolePolicyRequestDto
            {
                Name  = "accountants",
                Roles = [new PolicyRole(role.Id, Required: true)]
            });
            Assert.True(accountants.IsSuccessful, accountants.ErrorMessage);
            var managers = await Authz.CreateGroupPolicyAsync(server.ClientUuid, new CreateGroupPolicyRequestDto
            {
                Name   = "managers",
                Groups = [new PolicyGroup(groupId, ExtendChildren: true)]
            });
            Assert.True(managers.IsSuccessful, managers.ErrorMessage);
            var onlyTestUser = await Authz.CreateUserPolicyAsync(server.ClientUuid, new CreateUserPolicyRequestDto
            {
                Name = "test-user", Users = [fixture.TestUserId], Logic = PolicyLogic.Negative
            });
            Assert.True(onlyTestUser.IsSuccessful, onlyTestUser.ErrorMessage);

            var rolePolicies = await Authz.GetPoliciesAsync(server.ClientUuid, new GetPoliciesQueryDto { Type = "role" });
            Assert.True(rolePolicies.IsSuccessful, rolePolicies.ErrorMessage);
            Assert.Contains(rolePolicies.Response, p => p.Id == accountants.Response.Id);
            Assert.Equal("group", (await Authz.GetPolicyAsync(server.ClientUuid, managers.Response.Id)).Response.Type);

            // Permissions
            var viewPermission = await Authz.CreateScopePermissionAsync(server.ClientUuid, new CreateScopePermissionRequestDto
            {
                Name      = "view-invoices",
                Resources = [server.Invoice.Id],
                Scopes    = [server.View.Id],
                Policies  = [accountants.Response.Id]
            });
            Assert.True(viewPermission.IsSuccessful, viewPermission.ErrorMessage);
            var approvePermission = await Authz.CreateScopePermissionAsync(server.ClientUuid, new CreateScopePermissionRequestDto
            {
                Name      = "approve-invoices",
                Resources = [server.Invoice.Id],
                Scopes    = [server.Approve.Id],
                Policies  = [managers.Response.Id]
            });
            Assert.True(approvePermission.IsSuccessful, approvePermission.ErrorMessage);

            var invoicePermissions = await Authz.GetPermissionsAsync(server.ClientUuid, new GetPermissionsQueryDto { Resource = server.Invoice.Id });
            Assert.True(invoicePermissions.IsSuccessful, invoicePermissions.ErrorMessage);
            Assert.Equal(["approve-invoices", "view-invoices"], invoicePermissions.Response.Select(p => p.Name).Order().ToList());
            Assert.All(invoicePermissions.Response, p => Assert.Equal("scope", p.Type));

            var view    = new PermissionRequest("invoice", "view");
            var approve = new PermissionRequest(server.Invoice.Id, "approve");

            // Not an accountant yet: nothing granted
            var token = await SignInAsync();
            var denied = await Authz.CheckPermissionAsync(token, server.ClientId, view);
            Assert.True(denied.IsSuccessful, denied.ErrorMessage);
            Assert.False(denied.Response.Granted);

            // Accountant: may view, not approve
            Assert.True((await Role.AssignRealmRoleToUserAsync(fixture.TestUserId, role.Id, role.Name)).IsSuccessful);
            token = await SignInAsync();
            Assert.True((await Authz.CheckPermissionAsync(token, server.ClientId, view)).Response.Granted);
            Assert.False((await Authz.CheckPermissionAsync(token, server.ClientId, approve)).Response.Granted);

            var granted = await Authz.GetGrantedPermissionsAsync(token, server.ClientId, [new PermissionRequest("invoice")]);
            Assert.True(granted.IsSuccessful, granted.ErrorMessage);
            var invoiceGrant = Assert.Single(granted.Response);
            Assert.Equal(server.Invoice.Id, invoiceGrant.ResourceId);
            Assert.Equal("invoice", invoiceGrant.ResourceName);
            Assert.Equal(["view"], invoiceGrant.Scopes);

            var evaluation = await Authz.EvaluateAsync(server.ClientUuid, fixture.TestUserId, [server.Invoice.Id]);
            Assert.True(evaluation.IsSuccessful, evaluation.ErrorMessage);
            var invoiceResult = Assert.Single(evaluation.Response.Results, r => r.Resource?.Id == server.Invoice.Id);
            Assert.Equal("PERMIT", invoiceResult.Status);
            Assert.Equal(["view"], invoiceResult.AllowedScopes!.Select(s => s.Name).ToList());

            // Manager too: may approve; the RPT carries both scopes
            Assert.True((await Group.AddUserToGroupAsync(fixture.TestUserId, groupId)).IsSuccessful);
            token = await SignInAsync();
            Assert.True((await Authz.CheckPermissionAsync(token, server.ClientId, approve)).Response.Granted);

            var rpt = await Authz.GetRequestingPartyTokenAsync(token, server.ClientId, [new PermissionRequest("invoice")]);
            Assert.True(rpt.IsSuccessful, rpt.ErrorMessage);
            var authorization = new JsonWebToken(rpt.Response.AccessToken).GetPayloadValue<System.Text.Json.JsonElement>("authorization");
            var rptScopes = authorization.GetProperty("permissions")[0].GetProperty("scopes").EnumerateArray().Select(s => s.GetString()).Order().ToList();
            Assert.Equal(["approve", "view"], rptScopes);

            // Deleting the approve permission takes approval away again (enforcing mode)
            Assert.True((await Authz.DeletePermissionAsync(server.ClientUuid, approvePermission.Response.Id)).IsSuccessful);
            Assert.False((await Authz.CheckPermissionAsync(token, server.ClientId, approve)).Response.Granted);
        }
        finally
        {
            if (groupId is not null)
            {
                await Group.RemoveUserFromGroupAsync(fixture.TestUserId, groupId);
                await Group.DeleteGroupAsync(groupId);
            }
            if (role is not null)
                await Role.RemoveRealmRoleFromUserAsync(fixture.TestUserId, role.Id, role.Name);
            await Role.DeleteRealmRoleAsync(accountantRole);
            await Client.DeleteClientAsync(server.ClientUuid);
        }
    }

    [Fact]
    public async Task CheckPermissionAsync_InvalidToken_ReturnsFailure()
    {
        var server = await CreateResourceServerAsync(Guid.NewGuid().ToString("N"));
        try
        {
            var result = await Authz.CheckPermissionAsync("not-a-token", server.ClientId, new PermissionRequest("invoice", "view"));

            Assert.False(result.IsSuccessful);
        }
        finally
        {
            await Client.DeleteClientAsync(server.ClientUuid);
        }
    }
}
