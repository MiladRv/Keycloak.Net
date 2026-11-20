using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Keycloak.Net.Sdk.UserFederation.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// User federation against a real OpenLDAP server on Keycloak's Docker network: Keycloak writes a
/// new user to LDAP, and the user comes back through a full sync after the imported copy is removed.
/// </summary>
[Collection(nameof(KeycloakCollection))]
public class UserFederationIntegrationTests(KeycloakFixture fixture) : IAsyncLifetime
{
    private const string LdapAlias = "ldap";
    private IContainer _ldap = null!;

    private IServiceProvider Scoped => fixture.Services.CreateScope().ServiceProvider;
    private IUserFederationManagement Federation => Scoped.GetRequiredService<IUserFederationManagement>();
    private IUserManagement User => Scoped.GetRequiredService<IUserManagement>();

    public async Task InitializeAsync()
    {
        _ldap = new ContainerBuilder("osixia/openldap:1.5.0")
            .WithNetwork(fixture.Network)
            .WithNetworkAliases(LdapAlias)
            .WithEnvironment("LDAP_ORGANISATION", "Example")
            .WithEnvironment("LDAP_DOMAIN", "example.org")
            .WithEnvironment("LDAP_ADMIN_PASSWORD", "admin")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("slapd starting"))
            .Build();
        await _ldap.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_ldap is not null) await _ldap.DisposeAsync();
    }

    private static CreateUserStorageProviderRequestDto Ldap(string name, string connectionUrl) => new()
    {
        Name       = name,
        ProviderId = "ldap",
        Config     = new()
        {
            ["enabled"]               = ["true"],
            ["vendor"]                = ["other"],
            ["connectionUrl"]         = [connectionUrl],
            ["authType"]              = ["simple"],
            ["bindDn"]                = ["cn=admin,dc=example,dc=org"],
            ["bindCredential"]        = ["admin"],
            ["usersDn"]               = ["dc=example,dc=org"],
            ["searchScope"]           = ["1"],
            ["editMode"]              = ["WRITABLE"],
            ["syncRegistrations"]     = ["true"],
            ["importEnabled"]         = ["true"],
            ["usernameLDAPAttribute"] = ["uid"],
            ["rdnLDAPAttribute"]      = ["uid"],
            ["uuidLDAPAttribute"]     = ["entryUUID"],
            ["userObjectClasses"]     = ["inetOrgPerson, organizationalPerson"],
            ["pagination"]            = ["false"],
            ["fullSyncPeriod"]        = ["-1"],
            ["changedSyncPeriod"]     = ["-1"]
        }
    };

    [Fact]
    public async Task LdapProvider_WritesUsers_AndSyncImportsThemBack()
    {
        var name = $"corp-ldap-{Guid.NewGuid():N}";
        var created = await Federation.CreateProviderAsync(Ldap(name, $"ldap://{LdapAlias}:389"));
        Assert.True(created.IsSuccessful, created.ErrorMessage);
        var provider = created.Response;
        string? userId = null;
        try
        {
            Assert.Equal(name, provider.Name);
            Assert.Equal("ldap", provider.ProviderId);
            Assert.Equal("org.keycloak.storage.UserStorageProvider", provider.ProviderType);
            Assert.Equal(["ldap://ldap:389"], provider.Config!["connectionUrl"]);
            Assert.NotEqual(["admin"], provider.Config["bindCredential"]); // masked

            Assert.Contains((await Federation.GetProvidersAsync()).Response, p => p.Id == provider.Id);
            Assert.Contains((await Federation.GetProviderMappersAsync(provider.Id)).Response, m => m.Name == "username");

            // Update keeps the stored bind password (sent back masked) - the sync below proves it
            var update = await Federation.UpdateProviderAsync(provider.Id, new UpdateUserStorageProviderRequestDto
            {
                Name   = name + "-renamed",
                Config = new() { ["connectionTimeout"] = ["5000"] }
            });
            Assert.True(update.IsSuccessful, update.ErrorMessage);
            var updated = (await Federation.GetProviderAsync(provider.Id)).Response;
            Assert.Equal(name + "-renamed", updated.Name);
            Assert.Equal(["5000"], updated.Config!["connectionTimeout"]);
            Assert.Equal(["WRITABLE"], updated.Config["editMode"]);

            var connection = await Federation.TestLdapConnectionAsync(provider.Id);
            Assert.True(connection.IsSuccessful, connection.ErrorMessage);

            // A user registered in Keycloak is written to LDAP
            var username = $"ldap-user-{Guid.NewGuid():N}"[..20];
            var signup = await User.SignupAsync(new SignupRequestDto(username, "Ldap@1234"));
            Assert.True(signup.IsSuccessful, signup.ErrorMessage);
            userId = (await User.GetUserByUsernameAsync(username)).Response.Single(u => u.Username == username).Id;

            // Remove the imported copy; a full sync imports it again from LDAP
            Assert.True((await Federation.RemoveImportedUsersAsync(provider.Id)).IsSuccessful);
            var sync = await Federation.SyncUsersAsync(provider.Id);
            Assert.True(sync.IsSuccessful, sync.ErrorMessage);
            Assert.False(sync.Response.Ignored);
            Assert.Equal(1, sync.Response.Added);
            Assert.Equal(0, sync.Response.Failed);

            var changed = await Federation.SyncUsersAsync(provider.Id, changedOnly: true);
            Assert.True(changed.IsSuccessful, changed.ErrorMessage);

            // Unlinking turns the user into a regular Keycloak user that survives deleting the provider
            userId = (await User.GetUserByUsernameAsync(username)).Response.Single(u => u.Username == username).Id;
            Assert.True((await Federation.UnlinkUsersAsync(provider.Id)).IsSuccessful);
            Assert.True((await Federation.DeleteProviderAsync(provider.Id)).IsSuccessful);
            provider = null;

            var survivor = await User.GetUserAsync(userId);
            Assert.True(survivor.IsSuccessful, survivor.ErrorMessage);
            Assert.Equal(username, survivor.Response.Username);
        }
        finally
        {
            if (provider is not null) await Federation.DeleteProviderAsync(provider.Id);
            if (userId is not null) await User.DeleteUserAsync(userId);
        }
    }

    [Fact]
    public async Task UnreachableLdap_SyncReportsNothing_ButConnectionTestFails()
    {
        var created = await Federation.CreateProviderAsync(Ldap($"dead-ldap-{Guid.NewGuid():N}", "ldap://ldap.invalid:389"));
        Assert.True(created.IsSuccessful, created.ErrorMessage);
        try
        {
            // Keycloak only logs the connection error and reports an empty, successful sync
            var sync = await Federation.SyncUsersAsync(created.Response.Id);
            Assert.True(sync.IsSuccessful, sync.ErrorMessage);
            Assert.Equal(0, sync.Response.Added);

            Assert.False((await Federation.TestLdapConnectionAsync(created.Response.Id, authenticate: false)).IsSuccessful);
        }
        finally
        {
            await Federation.DeleteProviderAsync(created.Response.Id);
        }
    }

    [Fact]
    public async Task GetProviderAsync_Unknown_ReturnsFailure()
    {
        Assert.False((await Federation.GetProviderAsync(Guid.NewGuid().ToString())).IsSuccessful);
    }
}
