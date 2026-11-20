using Keycloak.Net.Sdk.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

[Collection(nameof(KeycloakCollection))]
public class NamedRealmIntegrationTests(KeycloakFixture fixture)
{
    [Fact]
    public async Task NamedConnections_SameServerAndClientId_ManageTheCorrectRealm()
    {
        using var scope = fixture.Services.CreateScope();
        var first = scope.ServiceProvider.GetRequiredKeyedService<IKeycloakManagement>("primary");
        var second = scope.ServiceProvider.GetRequiredKeyedService<IKeycloakManagement>("secondary");

        var firstUsers = await first.UserManagement.GetUserByUsernameAsync(KeycloakFixture.TestUsername);
        var secondUsers = await second.UserManagement.GetUserByUsernameAsync(KeycloakFixture.TestUsername);

        Assert.True(firstUsers.IsSuccessful, firstUsers.ErrorMessage);
        Assert.True(secondUsers.IsSuccessful, secondUsers.ErrorMessage);
        Assert.Equal(fixture.TestUserId, Assert.Single(firstUsers.Response).Id);
        Assert.Equal(fixture.SecondaryTestUserId, Assert.Single(secondUsers.Response).Id);
        Assert.NotEqual(firstUsers.Response[0].Id, secondUsers.Response[0].Id);

        var wrongRealm = await second.UserManagement.GetUserAsync(fixture.TestUserId);
        Assert.False(wrongRealm.IsSuccessful);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, wrongRealm.StatusCode);
    }
}
