using Keycloak.Net.Sdk.Contracts;

namespace Keycloak.Net.Sdk;

public sealed class KeycloakAdminClient : IKeycloakAdminClient
{
    public KeycloakAdminClient(IUserManagement userManagement,
        IRoleManagement roleManagement,
        ITokenManagement tokenManagement,
        IRealmManagement realmManagement,
        IClientManagement clientManagement)
    {
        UserManagement = userManagement;
        RoleManagement = roleManagement;
        TokenManagement = tokenManagement;
        RealmManagement = realmManagement;
        ClientManagement = clientManagement;
    }

    public IUserManagement UserManagement { get; init; }
    public IRoleManagement RoleManagement { get; init; }
    public ITokenManagement TokenManagement { get; init; }
    public IRealmManagement RealmManagement { get; init; }
    public IClientManagement ClientManagement { get; init; }
}