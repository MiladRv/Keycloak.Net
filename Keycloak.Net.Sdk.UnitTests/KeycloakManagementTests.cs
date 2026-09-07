using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.UserSessions.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Moq;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class KeycloakManagementTests
{
    [Fact]
    public void Constructor_ExposesEachInjectedManagerThroughItsOwnProperty()
    {
        var userManagement = Mock.Of<IUserManagement>();
        var roleManagement = Mock.Of<IRoleManagement>();
        var tokenManagement = Mock.Of<ITokenManagement>();
        var realmManagement = Mock.Of<IRealmManagement>();
        var clientManagement = Mock.Of<IClientManagement>();
        var groupManagement = Mock.Of<IGroupManagement>();
        var userSessionManagement = Mock.Of<IUserSessionManagement>();

        var sut = new KeycloakManagement(
            userManagement,
            roleManagement,
            tokenManagement,
            realmManagement,
            clientManagement,
            groupManagement,
            userSessionManagement);

        Assert.Same(userManagement, sut.UserManagement);
        Assert.Same(roleManagement, sut.RoleManagement);
        Assert.Same(tokenManagement, sut.TokenManagement);
        Assert.Same(realmManagement, sut.RealmManagement);
        Assert.Same(clientManagement, sut.ClientManagement);
        Assert.Same(groupManagement, sut.GroupManagement);
        Assert.Same(userSessionManagement, sut.UserSessionManagement);
    }
}
