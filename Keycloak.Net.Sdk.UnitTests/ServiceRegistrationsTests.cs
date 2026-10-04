using System.Net.Http;
using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.UserSessions.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class ServiceRegistrationsTests
{
    private static IConfiguration BuildConfiguration(string? serverUrl = "http://localhost:8080/") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["keycloak:ServerUrl"] = serverUrl,
                ["keycloak:RealmName"] = "test-realm",
                ["keycloak:ClientId"] = "test-client",
                ["keycloak:ClientSecret"] = "test-secret",
                ["keycloak:ClientUuid"] = "client-uuid-123"
            })
            .Build();

    [Fact]
    public void AddKeycloak_MissingServerUrl_Throws()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(serverUrl: null);

        Assert.Throws<InvalidOperationException>(() => services.AddKeycloak(configuration));
    }

    [Fact]
    public void AddKeycloak_RegistersEveryManagerInterface()
    {
        var services = new ServiceCollection();
        services.AddKeycloak(BuildConfiguration());
        var provider = services.BuildServiceProvider();

        Assert.IsType<KeycloakManagement>(provider.GetRequiredService<IKeycloakManagement>());
        Assert.NotNull(provider.GetRequiredService<IUserManagement>());
        Assert.NotNull(provider.GetRequiredService<ITokenManagement>());
        Assert.NotNull(provider.GetRequiredService<IRoleManagement>());
        Assert.NotNull(provider.GetRequiredService<IRealmManagement>());
        Assert.NotNull(provider.GetRequiredService<IClientManagement>());
        Assert.NotNull(provider.GetRequiredService<IGroupManagement>());
        Assert.NotNull(provider.GetRequiredService<IUserSessionManagement>());
    }

    [Fact]
    public void AddKeycloak_KeycloakManagement_AggregatesAllManagersFromTheSameContainer()
    {
        var services = new ServiceCollection();
        services.AddKeycloak(BuildConfiguration());
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var management = scope.ServiceProvider.GetRequiredService<IKeycloakManagement>();

        Assert.Same(scope.ServiceProvider.GetRequiredService<IUserManagement>(), management.UserManagement);
        Assert.Same(scope.ServiceProvider.GetRequiredService<IRoleManagement>(), management.RoleManagement);
        Assert.Same(scope.ServiceProvider.GetRequiredService<ITokenManagement>(), management.TokenManagement);
        Assert.Same(scope.ServiceProvider.GetRequiredService<IRealmManagement>(), management.RealmManagement);
        Assert.Same(scope.ServiceProvider.GetRequiredService<IClientManagement>(), management.ClientManagement);
        Assert.Same(scope.ServiceProvider.GetRequiredService<IGroupManagement>(), management.GroupManagement);
        Assert.Same(scope.ServiceProvider.GetRequiredService<IUserSessionManagement>(), management.UserSessionManagement);
    }

    [Fact]
    public void AddKeycloak_BindsConfigurationSectionIntoOptions()
    {
        var services = new ServiceCollection();
        services.AddKeycloak(BuildConfiguration());
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<KeycloakConfiguration>>().Value;

        Assert.Equal("http://localhost:8080/", options.ServerUrl);
        Assert.Equal("test-realm", options.RealmName);
        Assert.Equal("test-client", options.ClientId);
    }

    [Fact]
    public void AddKeycloak_ConfigureCallback_OverridesBoundConfiguration()
    {
        var services = new ServiceCollection();
        services.AddKeycloak(BuildConfiguration(), configure: cfg => cfg.NumberOfRetries = 7);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<KeycloakConfiguration>>().Value;

        Assert.Equal(7, options.NumberOfRetries);
        // The rest of the bound configuration is untouched by the callback
        Assert.Equal("test-realm", options.RealmName);
    }

    [Fact]
    public void AddKeycloak_RegistersDistinctNamedHttpClientsPointingAtTheServerUrl()
    {
        var services = new ServiceCollection();
        services.AddKeycloak(BuildConfiguration());
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        foreach (var name in new[] { "keycloak", "keycloak-admin", "keycloak-token" })
        {
            var client = factory.CreateClient(name);
            Assert.Equal("http://localhost:8080/", client.BaseAddress!.ToString());
        }
    }
}
