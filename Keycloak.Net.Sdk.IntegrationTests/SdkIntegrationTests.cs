using Keycloak.Net.Sdk.Users.Contracts;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.Athentications.Contracts;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.Realms.Contracts;
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

    [Fact]
    public async Task GetUsersAsync_NoQuery_ReturnsAtLeastOneUser()
    {
        var result = await User.GetUsersAsync();

        Assert.True(result.IsSuccessful);
        Assert.NotEmpty(result.Response);
    }

    [Fact]
    public async Task GetUsersAsync_WithMaxOne_ReturnsExactlyOneUser()
    {
        var result = await User.GetUsersAsync(new GetUsersQueryDto { Max = 1 });

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
    }

    [Fact]
    public async Task GetUsersAsync_WithFirstBeyondTotal_ReturnsEmptyList()
    {
        var result = await User.GetUsersAsync(new GetUsersQueryDto { First = 10_000 });

        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Response);
    }

    [Fact]
    public async Task GetUsersAsync_WithUsernameFilter_ReturnsMatchingUser()
    {
        var result = await User.GetUsersAsync(new GetUsersQueryDto { Username = KeycloakFixture.TestUsername });

        Assert.True(result.IsSuccessful);
        Assert.Contains(result.Response, u => u.Username == KeycloakFixture.TestUsername);
    }

    [Fact]
    public async Task GetUsersAsync_WithEnabledFilter_ReturnsOnlyEnabledUsers()
    {
        var result = await User.GetUsersAsync(new GetUsersQueryDto { Enabled = true });

        Assert.True(result.IsSuccessful);
        Assert.All(result.Response, u => Assert.True(u.Enabled));
    }

    [Fact]
    public async Task UpdateUserAsync_ChangesProfileFields()
    {
        const string newFirstName = "Updated";
        const string newLastName  = "Name";
        const string newEmail     = "updated@example.com";

        await User.UpdateUserAsync(fixture.TestUserId, new UpdateUserRequestDto
        {
            FirstName = newFirstName,
            LastName  = newLastName,
            Email     = newEmail
        });

        var result = await User.GetUserAsync(fixture.TestUserId);
        Assert.True(result.IsSuccessful);
        Assert.Equal(newFirstName, result.Response.FirstName);
        Assert.Equal(newLastName, result.Response.LastName);
        Assert.Equal(newEmail, result.Response.Email);
    }

    [Fact]
    public async Task GetUserAttributesAsync_NoAttributes_ReturnsEmptyDictionary()
    {
        var result = await User.GetUserAttributesAsync(fixture.TestUserId);

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Response);
    }

    [Fact]
    public async Task SetAndGetUserAttributeAsync_WorksRoundTrip()
    {
        await User.SetUserAttributeAsync(fixture.TestUserId, "department", "engineering");

        var result = await User.GetUserAttributesAsync(fixture.TestUserId);

        Assert.True(result.IsSuccessful);
        Assert.True(result.Response.ContainsKey("department"));
        Assert.Equal("engineering", result.Response["department"][0]);
    }

    [Fact]
    public async Task SetUserAttributeAsync_OverwritesExistingValue()
    {
        await User.SetUserAttributeAsync(fixture.TestUserId, "tenantId", "tenant-v1");
        await User.SetUserAttributeAsync(fixture.TestUserId, "tenantId", "tenant-v2");

        var result = await User.GetUserAttributesAsync(fixture.TestUserId);

        Assert.True(result.IsSuccessful);
        Assert.Equal("tenant-v2", result.Response["tenantId"][0]);
    }

    [Fact]
    public async Task SetUserAttributeAsync_PreservesOtherAttributes()
    {
        await User.SetUserAttributeAsync(fixture.TestUserId, "attrA", "valueA");
        await User.SetUserAttributeAsync(fixture.TestUserId, "attrB", "valueB");

        var result = await User.GetUserAttributesAsync(fixture.TestUserId);

        Assert.True(result.IsSuccessful);
        Assert.True(result.Response.ContainsKey("attrA"));
        Assert.True(result.Response.ContainsKey("attrB"));
    }

    [Fact]
    public async Task GetUsersByEmailAsync_WithExactEmail_ReturnsMatchingUser()
    {
        // First set an email on the test user
        const string email = "findme@example.com";
        await User.UpdateUserAsync(fixture.TestUserId, new UpdateUserRequestDto { Email = email });

        var result = await User.GetUsersByEmailAsync(email);

        Assert.True(result.IsSuccessful);
        Assert.NotEmpty(result.Response);
        Assert.Contains(result.Response, u => u.Email == email);
    }

    [Fact]
    public async Task GetUsersByEmailAsync_WithNonExistentEmail_ReturnsEmptyList()
    {
        var result = await User.GetUsersByEmailAsync("nobody@nonexistent-domain-xyz.com");

        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Response);
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

    [Fact]
    public async Task ClientScopeCrud_WorksRoundTrip()
    {
        var scopeName = $"temp-scope-{Guid.NewGuid():N}";

        // Create
        var created = await Client.CreateClientScopeAsync(new CreateClientScopeRequestDto
        {
            Name        = scopeName,
            Description = "Temp integration test scope"
        });
        Assert.True(created.IsSuccessful);

        // Find the created scope's id
        var scopes = await Client.GetClientScopes();
        var found  = scopes.Response.FirstOrDefault(s => s.Name == scopeName);
        Assert.NotNull(found);

        // Get by id
        var fetched = await Client.GetClientScopeAsync(found.Id);
        Assert.True(fetched.IsSuccessful);
        Assert.Equal(scopeName, fetched.Response.Name);

        // Update
        var updated = await Client.UpdateClientScopeAsync(found.Id, new UpdateClientScopeRequestDto
        {
            Name        = scopeName,
            Description = "Updated description"
        });
        Assert.True(updated.IsSuccessful);

        var refetched = await Client.GetClientScopeAsync(found.Id);
        Assert.Equal("Updated description", refetched.Response.Description);

        // Delete
        var deleted = await Client.DeleteClientScopeAsync(found.Id);
        Assert.True(deleted.IsSuccessful);

        var afterDelete = await Client.GetClientScopeAsync(found.Id);
        Assert.False(afterDelete.IsSuccessful);
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

[Collection(nameof(KeycloakCollection))]
public class RealmManagementIntegrationTests(KeycloakFixture fixture)
{
    private IRealmManagement Realm => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IRealmManagement>();

    [Fact]
    public async Task GetRealmsAsync_ReturnsCreatedTestRealm()
    {
        var result = await Realm.GetRealmsAsync();

        Assert.True(result.IsSuccessful);
        Assert.Contains(result.Response, r => r.Realm == KeycloakFixture.Realm);
    }

    [Fact]
    public async Task GetRealmAsync_WithValidName_ReturnsRealmDetails()
    {
        var result = await Realm.GetRealmAsync(KeycloakFixture.Realm);

        Assert.True(result.IsSuccessful);
        Assert.Equal(KeycloakFixture.Realm, result.Response.Realm);
        Assert.True(result.Response.Enabled);
    }

    [Fact]
    public async Task GetRealmAsync_WithInvalidName_ReturnsFailure()
    {
        var result = await Realm.GetRealmAsync("non-existent-realm-xyz");

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task CreateGetUpdateDeleteRealmAsync_WorksRoundTrip()
    {
        var realmName = $"temp-realm-{Guid.NewGuid():N}";

        // Create
        var created = await Realm.CreateRealmAsync(realmName);
        Assert.True(created.IsSuccessful);

        // Get
        var fetched = await Realm.GetRealmAsync(realmName);
        Assert.True(fetched.IsSuccessful);
        Assert.Equal(realmName, fetched.Response.Realm);

        // Update
        var updated = await Realm.UpdateRealmAsync(realmName, new UpdateRealmRequestDto
        {
            DisplayName = "Temp Integration Test Realm",
            Enabled = true
        });
        Assert.True(updated.IsSuccessful);

        var refetched = await Realm.GetRealmAsync(realmName);
        Assert.True(refetched.IsSuccessful);
        Assert.Equal("Temp Integration Test Realm", refetched.Response.DisplayName);

        // Delete
        var deleted = await Realm.DeleteRealmAsync(realmName);
        Assert.True(deleted.IsSuccessful);

        // Verify deleted
        var afterDelete = await Realm.GetRealmAsync(realmName);
        Assert.False(afterDelete.IsSuccessful);
    }
}
