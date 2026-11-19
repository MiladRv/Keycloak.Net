using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Roles.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// Client-role management against a client other than the one configured for the SDK,
/// e.g. an ERP with separate "finance" and "inventory" clients.
/// </summary>
[Collection(nameof(KeycloakCollection))]
public class ClientRoleIntegrationTests(KeycloakFixture fixture)
{
    private IRoleManagement Role => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IRoleManagement>();
    private IClientManagement Client => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IClientManagement>();

    private async Task<string> CreateTempClientAsync()
    {
        var clientId = $"erp-module-{Guid.NewGuid():N}";
        var created = await Client.CreateClientAsync(new CreateClientRequestDto { ClientId = clientId, Name = clientId });
        Assert.True(created.IsSuccessful);

        var clients = await Client.GetClientsAsync();
        return clients.Response.Single(c => c.ClientId == clientId).Id;
    }

    [Fact]
    public async Task ClientRoleCrud_OnAnotherClient_WorksRoundTrip()
    {
        var clientUuid = await CreateTempClientAsync();
        try
        {
            // Create
            var created = await Role.CreateClientRoleAsync(clientUuid, new CreateClientRoleRequestDto
            {
                Name        = "invoice-approve",
                Description = "Approve invoices",
                Attributes  = new() { ["module"] = ["finance"] }
            });
            Assert.True(created.IsSuccessful);

            // Read
            var fetched = await Role.GetClientRoleAsync(clientUuid, "invoice-approve");
            Assert.True(fetched.IsSuccessful);
            Assert.Equal("Approve invoices", fetched.Response.Description);
            Assert.Equal(clientUuid, fetched.Response.ContainerId);
            Assert.Equal(["finance"], fetched.Response.Attributes!["module"]);

            // The role lives on the other client only
            var otherRoles = await Role.GetClientRolesAsync(clientUuid);
            Assert.Contains(otherRoles.Response, r => r.Name == "invoice-approve");
            var configuredRoles = await Role.GetClientRolesAsync();
            Assert.DoesNotContain(configuredRoles.Response, r => r.Name == "invoice-approve");

            // Update without attributes keeps them
            var updated = await Role.UpdateClientRoleAsync(clientUuid, "invoice-approve",
                new UpdateClientRoleRequestDto { Name = "invoice-approve", Description = "Approve any invoice" });
            Assert.True(updated.IsSuccessful);
            var afterUpdate = await Role.GetClientRoleAsync(clientUuid, "invoice-approve");
            Assert.Equal("Approve any invoice", afterUpdate.Response.Description);
            Assert.Equal(["finance"], afterUpdate.Response.Attributes!["module"]);

            // Delete
            var deleted = await Role.DeleteClientRoleAsync(clientUuid, "invoice-approve");
            Assert.True(deleted.IsSuccessful);
            var missing = await Role.GetClientRoleAsync(clientUuid, "invoice-approve");
            Assert.False(missing.IsSuccessful);
        }
        finally
        {
            await Client.DeleteClientAsync(clientUuid);
        }
    }

    [Fact]
    public async Task AssignClientRole_OnAnotherClient_IsVisibleFromUserAndRole()
    {
        var clientUuid = await CreateTempClientAsync();
        try
        {
            Assert.True((await Role.CreateClientRoleAsync(clientUuid, new CreateClientRoleRequestDto { Name = "stock-view" })).IsSuccessful);
            var role = (await Role.GetClientRoleAsync(clientUuid, "stock-view")).Response;

            // Assign
            var assign = await Role.AssignClientRoleToUserAsync(clientUuid, fixture.TestUserId, role.Id, role.Name);
            Assert.True(assign.IsSuccessful);

            var userRoles = await Role.GetUserClientRolesAsync(clientUuid, fixture.TestUserId);
            Assert.Contains(userRoles.Response, r => r.Name == "stock-view");

            var roleUsers = await Role.GetClientRoleUsersAsync(clientUuid, "stock-view", first: 0, max: 10);
            Assert.Contains(roleUsers.Response, u => u.Id == fixture.TestUserId);

            // The mapping is not on the configured client
            var configuredUserRoles = await Role.GetUserClientRolesAsync(fixture.TestUserId);
            Assert.DoesNotContain(configuredUserRoles.Response, r => r.Name == "stock-view");

            // Remove
            var remove = await Role.RemoveClientRoleFromUserAsync(clientUuid, fixture.TestUserId, role.Id, role.Name);
            Assert.True(remove.IsSuccessful);
            var afterRemove = await Role.GetUserClientRolesAsync(clientUuid, fixture.TestUserId);
            Assert.DoesNotContain(afterRemove.Response, r => r.Name == "stock-view");
        }
        finally
        {
            await Client.DeleteClientAsync(clientUuid);
        }
    }

    [Fact]
    public async Task ClientRoleComposites_OnAnotherClient_WorkRoundTrip()
    {
        var clientUuid = await CreateTempClientAsync();
        try
        {
            Assert.True((await Role.CreateClientRoleAsync(clientUuid, new CreateClientRoleRequestDto { Name = "senior-accountant" })).IsSuccessful);
            Assert.True((await Role.CreateClientRoleAsync(clientUuid, new CreateClientRoleRequestDto { Name = "journal-post" })).IsSuccessful);
            var child = (await Role.GetClientRoleAsync(clientUuid, "journal-post")).Response;
            var composites = new List<CompositeRoleRequestDto> { new() { Id = child.Id, Name = child.Name } };

            var added = await Role.AddClientRoleCompositesAsync(clientUuid, "senior-accountant", composites);
            Assert.True(added.IsSuccessful);
            var afterAdd = await Role.GetClientRoleCompositesAsync(clientUuid, "senior-accountant");
            Assert.Contains(afterAdd.Response, r => r.Name == "journal-post");

            var removed = await Role.RemoveClientRoleCompositesAsync(clientUuid, "senior-accountant", composites);
            Assert.True(removed.IsSuccessful);
            var afterRemove = await Role.GetClientRoleCompositesAsync(clientUuid, "senior-accountant");
            Assert.DoesNotContain(afterRemove.Response, r => r.Name == "journal-post");
        }
        finally
        {
            await Client.DeleteClientAsync(clientUuid);
        }
    }

    [Fact]
    public async Task CreateUpdateDeleteClientRole_OnConfiguredClient_WorksRoundTrip()
    {
        var roleName = $"temp-role-{Guid.NewGuid():N}";

        Assert.True((await Role.CreateClientRoleAsync(new CreateClientRoleRequestDto { Name = roleName })).IsSuccessful);
        var fetched = await Role.GetClientRoleAsync(roleName);
        Assert.Equal(fixture.SdkClientUuid, fetched.Response.ContainerId);

        Assert.True((await Role.UpdateClientRoleAsync(roleName,
            new UpdateClientRoleRequestDto { Name = roleName, Description = "updated" })).IsSuccessful);
        Assert.Equal("updated", (await Role.GetClientRoleAsync(roleName)).Response.Description);

        Assert.True((await Role.DeleteClientRoleAsync(roleName)).IsSuccessful);
        Assert.False((await Role.GetClientRoleAsync(roleName)).IsSuccessful);
    }
}
