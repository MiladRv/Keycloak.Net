using Keycloak.Net.Sdk.Authorization;
using Keycloak.Net.Sdk.Authorization.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class AuthorizationRegistrationTests
{
    [Fact]
    public void AddKeycloak_RegistersAuthorizationManagement()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["keycloak:ServerUrl"] = "https://sso.example.test/",
            ["keycloak:RealmName"] = "erp"
        }).Build();
        var services = new ServiceCollection();
        services.AddKeycloak(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<AuthorizationManagement>(scope.ServiceProvider.GetRequiredService<IAuthorizationManagement>());
    }

    [Fact]
    public void NamedKeycloak_RegistersKeyedAuthorizationManagement()
    {
        var services = new ServiceCollection();
        services.AddKeycloak("erp", settings =>
        {
            settings.ServerUrl = "https://sso.example.test/";
            settings.RealmName = "erp";
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<AuthorizationManagement>(scope.ServiceProvider.GetRequiredKeyedService<IAuthorizationManagement>("erp"));
    }
}
