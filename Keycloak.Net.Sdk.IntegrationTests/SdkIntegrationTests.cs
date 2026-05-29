using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.Athentications.Contracts;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Realms;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

[Collection(nameof(KeycloakCollection))]
public class UserManagementIntegrationTests(KeycloakFixture fixture)
{
    private IUserManagement User => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IUserManagement>();

    [Fact]
    public async Task SigninAsync_WithValidCredentials_ReturnsTokens()
    {
        var result = await User.SigninAsync(KeycloakFixture.TestUsername, KeycloakFixture.TestPassword);

        Assert.True(result.IsSuccessful);
        Assert.NotEmpty(result.Response.AccessToken!);
        Assert.NotEmpty(result.Response.RefreshToken);
        Assert.True(result.Response.ExpiresIn > 0);
    }

    [Fact]
    public async Task SigninAsync_WithWrongPassword_ReturnsFailure()
    {
        var result = await User.SigninAsync(KeycloakFixture.TestUsername, "wrong-password");

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task GetUserAsync_WithValidId_ReturnsCorrectUser()
    {
        var result = await User.GetUserAsync(fixture.TestUserId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(fixture.TestUserId, result.Response.Id);
        Assert.Equal(KeycloakFixture.TestUsername, result.Response.Username);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidUsername_ReturnsUser()
    {
        var result = await User.GetUserByUsernameAsync(KeycloakFixture.TestUsername);

        Assert.True(result.IsSuccessful);
        Assert.NotEmpty(result.Response);
        Assert.Contains(result.Response, u => u.Username == KeycloakFixture.TestUsername);
    }

    [Fact]
    public async Task SignupAsync_WithNewUser_CreatesUser()
    {
        var username = $"newuser-{Guid.NewGuid():N}";
        var request  = new SignupRequestDto(username, "NewUser@123");

        var result = await User.SignupAsync(request);

        Assert.True(result.IsSuccessful);

        // Verify the user was actually created
        var found = await User.GetUserByUsernameAsync(username);
        Assert.True(found.IsSuccessful);
        Assert.Single(found.Response);
    }

    [Fact]
    public async Task EnableDisableUserAsync_TogglesUserStatus()
    {
        // Disable
        await User.DisableUserAsync(fixture.TestUserId);
        var disabled = await User.GetUserAsync(fixture.TestUserId);
        Assert.False(disabled.Response.Enabled);

        // Re-enable
        await User.EnableUserAsync(fixture.TestUserId);
        var enabled = await User.GetUserAsync(fixture.TestUserId);
        Assert.True(enabled.Response.Enabled);
    }

    [Fact]
    public async Task SetUserPasswordAsync_ChangesPassword()
    {
        const string newPassword = "Updated@5678";
        await User.SetUserPasswordAsync(fixture.TestUserId, newPassword);

        // Verify new password works
        var result = await User.SigninAsync(KeycloakFixture.TestUsername, newPassword);
        Assert.True(result.IsSuccessful);

        // Restore original password so other tests aren't affected
        await User.SetUserPasswordAsync(fixture.TestUserId, KeycloakFixture.TestPassword);
    }
}

[Collection(nameof(KeycloakCollection))]
public class TokenManagementIntegrationTests(KeycloakFixture fixture)
{
    private IUserManagement  User  => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IUserManagement>();
    private ITokenManagement Token => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<ITokenManagement>();

    [Fact]
    public async Task RefreshTokenAsync_WithValidRefreshToken_ReturnsNewAccessToken()
    {
        var signin = await User.SigninAsync(KeycloakFixture.TestUsername, KeycloakFixture.TestPassword);
        Assert.True(signin.IsSuccessful);

        var result = await Token.RefreshTokenAsync(signin.Response.RefreshToken);

        Assert.True(result.IsSuccessful);
        Assert.NotEmpty(result.Response.AccessToken!);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidRefreshToken_RevokesSuccessfully()
    {
        var signin = await User.SigninAsync(KeycloakFixture.TestUsername, KeycloakFixture.TestPassword);
        Assert.True(signin.IsSuccessful);

        var result = await Token.RevokeTokenAsync(signin.Response.RefreshToken);

        Assert.True(result.IsSuccessful);

        // After revocation, the refresh token must no longer work
        var refresh = await Token.RefreshTokenAsync(signin.Response.RefreshToken);
        Assert.False(refresh.IsSuccessful);
    }
}

[Collection(nameof(KeycloakCollection))]
public class RoleManagementIntegrationTests(KeycloakFixture fixture)
{
    private IRoleManagement Role => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IRoleManagement>();

    [Fact]
    public async Task GetClientRoles_ReturnsCreatedTestRole()
    {
        var result = await Role.GetClientRoles();

        Assert.True(result.IsSuccessful);
        Assert.Contains(result.Response, r => r.Name == KeycloakFixture.TestRoleName);
    }

    [Fact]
    public async Task AssignAndRemoveClientRoleToUser_WorksRoundTrip()
    {
        // Assign
        var assign = await Role.AssignClientRoleToUser(
            fixture.TestUserId, fixture.TestRoleId, KeycloakFixture.TestRoleName);
        Assert.True(assign.IsSuccessful);

        // Remove
        var remove = await Role.RemoveClientRoleFromUserAsync(
            fixture.TestUserId, fixture.TestRoleId, KeycloakFixture.TestRoleName);
        Assert.True(remove.IsSuccessful);
    }
}

[Collection(nameof(KeycloakCollection))]
public class ClientManagementIntegrationTests(KeycloakFixture fixture)
{
    private IClientManagement Client => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IClientManagement>();

    [Fact]
    public async Task GetClientsAsync_ReturnsSdkClient()
    {
        var result = await Client.GetClientsAsync();

        Assert.True(result.IsSuccessful);
        Assert.Contains(result.Response, c => c.ClientId == KeycloakFixture.SdkClientId);
    }

    [Fact]
    public async Task GetClientScopes_ReturnsScopes()
    {
        var result = await Client.GetClientScopes();

        Assert.True(result.IsSuccessful);
        Assert.NotEmpty(result.Response);
    }

    [Fact]
    public async Task CreateAndDeleteClientAsync_WorksRoundTrip()
    {
        var newClientId = $"temp-client-{Guid.NewGuid():N}";
        var createRequest = new CreateClientRequestDto
        {
            ClientId = newClientId,
            Name     = "Temp Integration Test Client",
            Enabled  = true
        };

        // Create
        var created = await Client.CreateClientAsync(createRequest);
        Assert.True(created.IsSuccessful);

        // Find the UUID
        var clients = await Client.GetClientsAsync();
        var found   = clients.Response.FirstOrDefault(c => c.ClientId == newClientId);
        Assert.NotNull(found);

        // Delete
        var deleted = await Client.DeleteClientAsync(found.Id);
        Assert.True(deleted.IsSuccessful);
    }
}

[Collection(nameof(KeycloakCollection))]
public class GroupManagementIntegrationTests(KeycloakFixture fixture)
{
    private IGroupManagement Group => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IGroupManagement>();
    private IUserManagement  User  => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IUserManagement>();

    [Fact]
    public async Task GetGroupsAsync_ReturnsTestGroup()
    {
        var result = await Group.GetGroupsAsync();

        Assert.True(result.IsSuccessful);
        Assert.Contains(result.Response, g => g.Name == KeycloakFixture.TestGroupName);
    }

    [Fact]
    public async Task GetGroupAsync_WithValidId_ReturnsGroupInfo()
    {
        var result = await Group.GetGroupAsync(fixture.TestGroupId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(fixture.TestGroupId, result.Response.Id);
        Assert.Equal(KeycloakFixture.TestGroupName, result.Response.Name);
        Assert.NotEmpty(result.Response.Path);
    }

    [Fact]
    public async Task GetGroupAsync_WithInvalidId_ReturnsFailure()
    {
        var result = await Group.GetGroupAsync("00000000-0000-0000-0000-000000000000");

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task CreateAndDeleteGroupAsync_WorksRoundTrip()
    {
        var groupName = $"temp-group-{Guid.NewGuid():N}";

        // Create
        var created = await Group.CreateGroupAsync(new CreateGroupRequestDto { Name = groupName });
        Assert.True(created.IsSuccessful);

        // Verify group exists
        var groups = await Group.GetGroupsAsync();
        var found  = groups.Response.FirstOrDefault(g => g.Name == groupName);
        Assert.NotNull(found);

        // Delete
        var deleted = await Group.DeleteGroupAsync(found.Id);
        Assert.True(deleted.IsSuccessful);

        // Verify deleted
        var after = await Group.GetGroupsAsync();
        Assert.DoesNotContain(after.Response, g => g.Name == groupName);
    }

    [Fact]
    public async Task AddAndRemoveUserFromGroupAsync_WorksRoundTrip()
    {
        // Add user to group
        var add = await Group.AddUserToGroupAsync(fixture.TestUserId, fixture.TestGroupId);
        Assert.True(add.IsSuccessful);

        // Verify membership
        var userGroups = await Group.GetUserGroupsAsync(fixture.TestUserId);
        Assert.True(userGroups.IsSuccessful);
        Assert.Contains(userGroups.Response, g => g.Id == fixture.TestGroupId);

        // Remove user from group
        var remove = await Group.RemoveUserFromGroupAsync(fixture.TestUserId, fixture.TestGroupId);
        Assert.True(remove.IsSuccessful);

        // Verify removal
        var after = await Group.GetUserGroupsAsync(fixture.TestUserId);
        Assert.DoesNotContain(after.Response, g => g.Id == fixture.TestGroupId);
    }

    [Fact]
    public async Task GetUserGroupsAsync_WhenUserHasNoGroups_ReturnsEmptyList()
    {
        // Ensure user is not in the group
        await Group.RemoveUserFromGroupAsync(fixture.TestUserId, fixture.TestGroupId);

        var result = await Group.GetUserGroupsAsync(fixture.TestUserId);

        Assert.True(result.IsSuccessful);
        Assert.DoesNotContain(result.Response, g => g.Id == fixture.TestGroupId);
    }
}

[Collection(nameof(KeycloakCollection))]
public class RealmRoleManagementIntegrationTests(KeycloakFixture fixture)
{
    private IRoleManagement  Role  => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IRoleManagement>();
    private IGroupManagement Group => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IGroupManagement>();

    // ── Read ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRealmRolesAsync_ReturnsCreatedTestRole()
    {
        var result = await Role.GetRealmRolesAsync();

        Assert.True(result.IsSuccessful);
        Assert.Contains(result.Response, r => r.Name == KeycloakFixture.TestRealmRoleName);
        Assert.All(result.Response, r => Assert.False(r.ClientRole));
    }

    [Fact]
    public async Task GetRealmRoleAsync_WithValidName_ReturnsSingleRole()
    {
        var result = await Role.GetRealmRoleAsync(KeycloakFixture.TestRealmRoleName);

        Assert.True(result.IsSuccessful);
        Assert.Equal(fixture.TestRealmRoleId, result.Response.Id);
        Assert.Equal(KeycloakFixture.TestRealmRoleName, result.Response.Name);
        Assert.False(result.Response.ClientRole);
    }

    [Fact]
    public async Task GetRealmRoleAsync_WithInvalidName_ReturnsFailure()
    {
        var result = await Role.GetRealmRoleAsync("non-existent-role");

        Assert.False(result.IsSuccessful);
    }

    // ── Create / Delete round-trip ────────────────────────────────────────────

    [Fact]
    public async Task CreateAndDeleteRealmRoleAsync_WorksRoundTrip()
    {
        var roleName = $"temp-realm-role-{Guid.NewGuid():N}";

        // Create
        var created = await Role.CreateRealmRoleAsync(new CreateRealmRoleRequestDto
        {
            Name        = roleName,
            Description = "Temporary integration test role"
        });
        Assert.True(created.IsSuccessful);

        // Verify it exists
        var fetched = await Role.GetRealmRoleAsync(roleName);
        Assert.True(fetched.IsSuccessful);
        Assert.Equal(roleName, fetched.Response.Name);

        // Delete
        var deleted = await Role.DeleteRealmRoleAsync(roleName);
        Assert.True(deleted.IsSuccessful);

        // Verify it no longer exists
        var afterDelete = await Role.GetRealmRoleAsync(roleName);
        Assert.False(afterDelete.IsSuccessful);
    }

    // ── Realm Role ↔ User ─────────────────────────────────────────────────────

    [Fact]
    public async Task AssignAndRemoveRealmRoleToUser_WorksRoundTrip()
    {
        // Assign
        var assign = await Role.AssignRealmRoleToUserAsync(
            fixture.TestUserId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName);
        Assert.True(assign.IsSuccessful);

        // Verify assignment
        var userRoles = await Role.GetUserRealmRolesAsync(fixture.TestUserId);
        Assert.True(userRoles.IsSuccessful);
        Assert.Contains(userRoles.Response, r => r.Id == fixture.TestRealmRoleId);

        // Remove
        var remove = await Role.RemoveRealmRoleFromUserAsync(
            fixture.TestUserId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName);
        Assert.True(remove.IsSuccessful);

        // Verify removal
        var after = await Role.GetUserRealmRolesAsync(fixture.TestUserId);
        Assert.DoesNotContain(after.Response, r => r.Id == fixture.TestRealmRoleId);
    }

    [Fact]
    public async Task GetUserRealmRolesAsync_WhenNoRolesAssigned_DoesNotContainTestRole()
    {
        // Ensure clean state
        await Role.RemoveRealmRoleFromUserAsync(
            fixture.TestUserId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName);

        var result = await Role.GetUserRealmRolesAsync(fixture.TestUserId);

        Assert.True(result.IsSuccessful);
        Assert.DoesNotContain(result.Response, r => r.Id == fixture.TestRealmRoleId);
    }

    // ── Realm Role ↔ Group ────────────────────────────────────────────────────

    [Fact]
    public async Task AssignAndRemoveRealmRoleToGroup_WorksRoundTrip()
    {
        // Assign
        var assign = await Role.AssignRealmRoleToGroupAsync(
            fixture.TestGroupId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName);
        Assert.True(assign.IsSuccessful);

        // Verify assignment
        var groupRoles = await Role.GetGroupRealmRolesAsync(fixture.TestGroupId);
        Assert.True(groupRoles.IsSuccessful);
        Assert.Contains(groupRoles.Response, r => r.Id == fixture.TestRealmRoleId);

        // Remove
        var remove = await Role.RemoveRealmRoleFromGroupAsync(
            fixture.TestGroupId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName);
        Assert.True(remove.IsSuccessful);

        // Verify removal
        var after = await Role.GetGroupRealmRolesAsync(fixture.TestGroupId);
        Assert.DoesNotContain(after.Response, r => r.Id == fixture.TestRealmRoleId);
    }

    [Fact]
    public async Task GetGroupRealmRolesAsync_WhenNoRolesAssigned_DoesNotContainTestRole()
    {
        // Ensure clean state
        await Role.RemoveRealmRoleFromGroupAsync(
            fixture.TestGroupId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName);

        var result = await Role.GetGroupRealmRolesAsync(fixture.TestGroupId);

        Assert.True(result.IsSuccessful);
        Assert.DoesNotContain(result.Response, r => r.Id == fixture.TestRealmRoleId);
    }
}
