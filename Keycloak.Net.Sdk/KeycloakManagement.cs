using Keycloak.Net.Sdk.Contracts;

namespace Keycloak.Net.Sdk;

public sealed class KeycloakManagement(
    IUserManagement userManagement,
    IRoleManagement roleManagement,
    ITokenManagement tokenManagement,
    IRealmManagement realmManagement,
    IClientManagement clientManagement)
    : IKeycloakManagement
{
    public IUserManagement UserManagement { get; init; } = userManagement;
    public IRoleManagement RoleManagement { get; init; } = roleManagement;
    public ITokenManagement TokenManagement { get; init; } = tokenManagement;
    public IRealmManagement RealmManagement { get; init; } = realmManagement;
    public IClientManagement ClientManagement { get; init; } = clientManagement;
}