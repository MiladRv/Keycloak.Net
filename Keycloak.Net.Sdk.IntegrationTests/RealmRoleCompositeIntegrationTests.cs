using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Roles.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// A job-title realm role (e.g. "accountant") that bundles realm and client permission roles.
/// </summary>
[Collection(nameof(KeycloakCollection))]
public class RealmRoleCompositeIntegrationTests(KeycloakFixture fixture)
{
    private IRoleManagement Role => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IRoleManagement>();
    private IClientManagement Client => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IClientManagement>();

    private async Task<RealmRoleResponseDto> CreateRealmRoleAsync(string name)
    {
        Assert.True((await Role.CreateRealmRoleAsync(new CreateRealmRoleRequestDto { Name = name })).IsSuccessful);
        return (await Role.GetRealmRoleAsync(name)).Response;
    }

    [Fact]
    public async Task RealmRoleComposites_AddReadRemove_AndGrantEffectiveRoles()
    {
        var run = Guid.NewGuid().ToString("N");
        var jobTitle = $"accountant-{run}";
        var viewer   = $"report-viewer-{run}";
        var clientId = $"erp-finance-{run}";
        Assert.True((await Client.CreateClientAsync(new CreateClientRequestDto { ClientId = clientId, Name = clientId })).IsSuccessful);
        var clientUuid = (await Client.GetClientsAsync()).Response.Single(c => c.ClientId == clientId).Id;
        try
        {
            var jobRole    = await CreateRealmRoleAsync(jobTitle);
            var viewerRole = await CreateRealmRoleAsync(viewer);
            Assert.True((await Role.CreateClientRoleAsync(clientUuid, new CreateClientRoleRequestDto { Name = "invoice-approve" })).IsSuccessful);
            var invoiceRole = (await Role.GetClientRoleAsync(clientUuid, "invoice-approve")).Response;

            // A realm role and a client role in one call
            Assert.True((await Role.AddRealmRoleCompositesAsync(jobTitle,
            [
                new CompositeRoleRequestDto { Id = viewerRole.Id,  Name = viewerRole.Name },
                new CompositeRoleRequestDto { Id = invoiceRole.Id, Name = invoiceRole.Name }
            ])).IsSuccessful);

            Assert.True((await Role.GetRealmRoleAsync(jobTitle)).Response.Composite);
            var realmComposites = await Role.GetRealmRoleCompositesAsync(jobTitle);
            Assert.True(realmComposites.IsSuccessful, realmComposites.ErrorMessage);
            Assert.Equal([viewer], realmComposites.Response.Select(r => r.Name).ToList());
            var clientComposites = await Role.GetRealmRoleClientCompositesAsync(clientUuid, jobTitle);
            Assert.True(clientComposites.IsSuccessful, clientComposites.ErrorMessage);
            Assert.Equal(["invoice-approve"], clientComposites.Response.Select(r => r.Name).ToList());

            // A user given only the job title effectively holds both bundled roles
            Assert.True((await Role.AssignRealmRoleToUserAsync(fixture.TestUserId, jobRole.Id, jobRole.Name)).IsSuccessful);
            try
            {
                Assert.Contains((await Role.GetUserEffectiveRealmRolesAsync(fixture.TestUserId)).Response, r => r.Name == viewer);
                Assert.Contains((await Role.GetUserEffectiveClientRolesAsync(clientUuid, fixture.TestUserId)).Response, r => r.Name == "invoice-approve");

                // Removing a composite removes it from the user's effective roles
                Assert.True((await Role.RemoveRealmRoleCompositesAsync(jobTitle,
                    [new CompositeRoleRequestDto { Id = invoiceRole.Id, Name = invoiceRole.Name }])).IsSuccessful);
                Assert.Empty((await Role.GetRealmRoleClientCompositesAsync(clientUuid, jobTitle)).Response);
                Assert.DoesNotContain((await Role.GetUserEffectiveClientRolesAsync(clientUuid, fixture.TestUserId)).Response, r => r.Name == "invoice-approve");
                Assert.Contains((await Role.GetUserEffectiveRealmRolesAsync(fixture.TestUserId)).Response, r => r.Name == viewer);
            }
            finally
            {
                await Role.RemoveRealmRoleFromUserAsync(fixture.TestUserId, jobRole.Id, jobRole.Name);
            }
        }
        finally
        {
            await Role.DeleteRealmRoleAsync(jobTitle);
            await Role.DeleteRealmRoleAsync(viewer);
            await Client.DeleteClientAsync(clientUuid);
        }
    }

    [Fact]
    public async Task GetRealmRoleCompositesAsync_UnknownRole_ReturnsFailure()
    {
        var result = await Role.GetRealmRoleCompositesAsync($"no-such-role-{Guid.NewGuid():N}");

        Assert.False(result.IsSuccessful);
    }
}
