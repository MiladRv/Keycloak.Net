using Keycloak.Net.Sdk.Athentications.Contracts;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;

namespace Keycloak.Net.Sdk.Contracts;

public interface IKeycloakManagement
{
    public IUserManagement UserManagement { get; init; }
    public IRoleManagement RoleManagement { get; init; }
    public ITokenManagement TokenManagement { get; init; }
    public IRealmManagement RealmManagement { get; init; }
    public IClientManagement ClientManagement { get; init; }
}