using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Roles.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// An ERP-style organisation tree (company → branch groups) carrying data-scope attributes,
/// with roles granted through groups and read back as effective roles.
/// </summary>
[Collection(nameof(KeycloakCollection))]
public class GroupHierarchyIntegrationTests(KeycloakFixture fixture)
{
    private IGroupManagement Group => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IGroupManagement>();
    private IRoleManagement Role => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IRoleManagement>();
    private IClientManagement Client => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IClientManagement>();

    private async Task<(GroupResponseDto Company, GroupResponseDto Branch)> CreateCompanyWithBranchAsync()
    {
        var companyName = $"acme-{Guid.NewGuid():N}";
        Assert.True((await Group.CreateGroupAsync(new CreateGroupRequestDto
        {
            Name       = companyName,
            Attributes = new() { ["company_id"] = ["1"] }
        })).IsSuccessful);
        var company = (await Group.GetGroupByPathAsync($"/{companyName}")).Response;

        Assert.True((await Group.CreateChildGroupAsync(company.Id, new CreateGroupRequestDto
        {
            Name       = "tehran branch",
            Attributes = new() { ["branch_id"] = ["12"] }
        })).IsSuccessful);
        var branch = await Group.GetGroupByPathAsync($"/{companyName}/tehran branch");
        Assert.True(branch.IsSuccessful);

        return (company, branch.Response);
    }

    [Fact]
    public async Task GroupTree_WithAttributes_CanBeCreatedNavigatedAndUpdated()
    {
        var (company, branch) = await CreateCompanyWithBranchAsync();
        try
        {
            Assert.Equal(["1"], company.Attributes!["company_id"]);
            Assert.Equal(["12"], branch.Attributes!["branch_id"]);
            Assert.EndsWith("/tehran branch", branch.Path);

            var children = await Group.GetChildGroupsAsync(company.Id, first: 0, max: 10);
            Assert.True(children.IsSuccessful, $"GET children failed: {children.StatusCode}");
            Assert.Contains(children.Response, g => g.Id == branch.Id);

            // Update attributes only; name stays
            Assert.True((await Group.UpdateGroupAsync(branch.Id, new UpdateGroupRequestDto
            {
                Attributes = new() { ["branch_id"] = ["13"], ["region"] = ["north"] }
            })).IsSuccessful);
            var updated = (await Group.GetGroupAsync(branch.Id)).Response;
            Assert.Equal("tehran branch", updated.Name);
            Assert.Equal(["13"], updated.Attributes!["branch_id"]);
            Assert.Equal(["north"], updated.Attributes["region"]);

            // Rename only; attributes stay
            Assert.True((await Group.UpdateGroupAsync(branch.Id, new UpdateGroupRequestDto { Name = "tehran" })).IsSuccessful);
            var renamed = (await Group.GetGroupAsync(branch.Id)).Response;
            Assert.Equal("tehran", renamed.Name);
            Assert.Equal(["13"], renamed.Attributes!["branch_id"]);
        }
        finally
        {
            await Group.DeleteGroupAsync(company.Id);
        }
    }

    [Fact]
    public async Task GetGroupByPathAsync_UnknownPath_ReturnsFailure()
    {
        var result = await Group.GetGroupByPathAsync($"/no-such-group-{Guid.NewGuid():N}");

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task GetGroupMembersAsync_ListsUsersInGroup()
    {
        var (company, branch) = await CreateCompanyWithBranchAsync();
        try
        {
            Assert.True((await Group.AddUserToGroupAsync(fixture.TestUserId, branch.Id)).IsSuccessful);

            var members = await Group.GetGroupMembersAsync(branch.Id, first: 0, max: 10);
            Assert.True(members.IsSuccessful);
            Assert.Contains(members.Response, u => u.Id == fixture.TestUserId);

            var parentMembers = await Group.GetGroupMembersAsync(company.Id);
            Assert.DoesNotContain(parentMembers.Response, u => u.Id == fixture.TestUserId);
        }
        finally
        {
            await Group.RemoveUserFromGroupAsync(fixture.TestUserId, branch.Id);
            await Group.DeleteGroupAsync(company.Id);
        }
    }

    [Fact]
    public async Task ClientRoleOnParentGroup_IsEffectiveForMemberOfChildGroup()
    {
        var (company, branch) = await CreateCompanyWithBranchAsync();
        var clientId = $"erp-finance-{Guid.NewGuid():N}";
        Assert.True((await Client.CreateClientAsync(new CreateClientRequestDto { ClientId = clientId, Name = clientId })).IsSuccessful);
        var clientUuid = (await Client.GetClientsAsync()).Response.Single(c => c.ClientId == clientId).Id;
        try
        {
            Assert.True((await Role.CreateClientRoleAsync(clientUuid, new CreateClientRoleRequestDto { Name = "invoice-view" })).IsSuccessful);
            var role = (await Role.GetClientRoleAsync(clientUuid, "invoice-view")).Response;

            // Grant on the company group
            Assert.True((await Role.AssignClientRoleToGroupAsync(clientUuid, company.Id, role.Id, role.Name)).IsSuccessful);
            Assert.Contains((await Role.GetGroupClientRolesAsync(clientUuid, company.Id)).Response, r => r.Name == "invoice-view");

            Assert.Contains((await Role.GetGroupEffectiveClientRolesAsync(clientUuid, company.Id)).Response, r => r.Name == "invoice-view");
            Assert.Empty((await Role.GetGroupClientRolesAsync(clientUuid, branch.Id)).Response);

            // Keycloak's group-level client composite endpoint does not include parent-group roles
            // (the realm-level one does), but user effective roles do - which is what ends up in the token.
            Assert.DoesNotContain((await Role.GetGroupEffectiveClientRolesAsync(clientUuid, branch.Id)).Response, r => r.Name == "invoice-view");

            // A member of the branch has it effectively, not directly
            Assert.True((await Group.AddUserToGroupAsync(fixture.TestUserId, branch.Id)).IsSuccessful);
            Assert.DoesNotContain((await Role.GetUserClientRolesAsync(clientUuid, fixture.TestUserId)).Response, r => r.Name == "invoice-view");
            Assert.Contains((await Role.GetUserEffectiveClientRolesAsync(clientUuid, fixture.TestUserId)).Response, r => r.Name == "invoice-view");

            // Removing the grant removes the effective role
            Assert.True((await Role.RemoveClientRoleFromGroupAsync(clientUuid, company.Id, role.Id, role.Name)).IsSuccessful);
            Assert.DoesNotContain((await Role.GetUserEffectiveClientRolesAsync(clientUuid, fixture.TestUserId)).Response, r => r.Name == "invoice-view");
        }
        finally
        {
            await Group.RemoveUserFromGroupAsync(fixture.TestUserId, branch.Id);
            await Group.DeleteGroupAsync(company.Id);
            await Client.DeleteClientAsync(clientUuid);
        }
    }

    [Fact]
    public async Task ClientRoleOnGroup_ConfiguredClientOverloads_WorkRoundTrip()
    {
        var groupName = $"temp-group-{Guid.NewGuid():N}";
        Assert.True((await Group.CreateGroupAsync(new CreateGroupRequestDto { Name = groupName })).IsSuccessful);
        var group = (await Group.GetGroupByPathAsync(groupName)).Response;
        try
        {
            Assert.True((await Role.AssignClientRoleToGroupAsync(group.Id, fixture.TestRoleId, KeycloakFixture.TestRoleName)).IsSuccessful);
            Assert.Contains((await Role.GetGroupClientRolesAsync(group.Id)).Response, r => r.Name == KeycloakFixture.TestRoleName);
            Assert.Contains((await Role.GetGroupEffectiveClientRolesAsync(group.Id)).Response, r => r.Name == KeycloakFixture.TestRoleName);

            Assert.True((await Role.RemoveClientRoleFromGroupAsync(group.Id, fixture.TestRoleId, KeycloakFixture.TestRoleName)).IsSuccessful);
            Assert.Empty((await Role.GetGroupClientRolesAsync(group.Id)).Response);
        }
        finally
        {
            await Group.DeleteGroupAsync(group.Id);
        }
    }

    [Fact]
    public async Task RealmRole_WithAttributes_ViaGroupAndComposite_IsEffective()
    {
        var (company, branch) = await CreateCompanyWithBranchAsync();
        var roleName = $"branch-manager-{Guid.NewGuid():N}";
        try
        {
            Assert.True((await Role.CreateRealmRoleAsync(new CreateRealmRoleRequestDto
            {
                Name       = roleName,
                Attributes = new() { ["scope_level"] = ["branch"] }
            })).IsSuccessful);
            var role = (await Role.GetRealmRoleAsync(roleName)).Response;
            Assert.Equal(["branch"], role.Attributes!["scope_level"]);

            // Update description only; attributes stay
            Assert.True((await Role.UpdateRealmRoleAsync(roleName,
                new UpdateRealmRoleRequestDto { Name = roleName, Description = "Manages one branch" })).IsSuccessful);
            var updated = (await Role.GetRealmRoleAsync(roleName)).Response;
            Assert.Equal("Manages one branch", updated.Description);
            Assert.Equal(["branch"], updated.Attributes!["scope_level"]);

            // Grant on the company; effective on the branch and its member
            Assert.True((await Role.AssignRealmRoleToGroupAsync(company.Id, role.Id, role.Name)).IsSuccessful);
            Assert.Contains((await Role.GetGroupEffectiveRealmRolesAsync(branch.Id)).Response, r => r.Name == roleName);

            Assert.True((await Group.AddUserToGroupAsync(fixture.TestUserId, branch.Id)).IsSuccessful);
            Assert.DoesNotContain((await Role.GetUserRealmRolesAsync(fixture.TestUserId)).Response, r => r.Name == roleName);
            Assert.Contains((await Role.GetUserEffectiveRealmRolesAsync(fixture.TestUserId)).Response, r => r.Name == roleName);
        }
        finally
        {
            await Group.RemoveUserFromGroupAsync(fixture.TestUserId, branch.Id);
            await Group.DeleteGroupAsync(company.Id);
            await Role.DeleteRealmRoleAsync(roleName);
        }
    }
}
