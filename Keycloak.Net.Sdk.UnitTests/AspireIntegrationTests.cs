using Keycloak.Net.Sdk.Aspire;
using Keycloak.Net.Sdk.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class AspireIntegrationTests
{
    private static HostApplicationBuilder CreateBuilder(Dictionary<string, string?> config)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(config);
        return builder;
    }

    [Fact]
    public void AddKeycloakSdk_UsesAspireConnectionString_AsServerUrl()
    {
        // Arrange
        var builder = CreateBuilder(new Dictionary<string, string?>
        {
            ["ConnectionStrings:keycloak"] = "http://localhost:8080",
            ["keycloak:RealmName"] = "my-realm",
            ["keycloak:ClientId"] = "my-client",
            ["keycloak:ClientSecret"] = "secret",
            ["keycloak:ClientUuid"] = "some-uuid",
            ["keycloak:AdminUsername"] = "admin",
            ["keycloak:AdminPassword"] = "admin"
        });

        // Act
        builder.AddKeycloakSdk();
        var host = builder.Build();

        // Assert
        var options = host.Services.GetRequiredService<IOptions<KeycloakConfiguration>>().Value;
        Assert.Equal("http://localhost:8080", options.ServerUrl);
        Assert.Equal("my-realm", options.RealmName);
    }

    [Fact]
    public void AddKeycloakSdk_WithCustomConnectionName_ReadsCorrectConnectionString()
    {
        // Arrange
        var builder = CreateBuilder(new Dictionary<string, string?>
        {
            ["ConnectionStrings:auth-server"] = "http://keycloak.internal:9090",
            ["keycloak:RealmName"] = "test",
            ["keycloak:ClientId"] = "c",
            ["keycloak:ClientSecret"] = "s",
            ["keycloak:ClientUuid"] = "u",
            ["keycloak:AdminUsername"] = "a",
            ["keycloak:AdminPassword"] = "p"
        });

        // Act
        builder.AddKeycloakSdk("auth-server");
        var host = builder.Build();

        // Assert
        var options = host.Services.GetRequiredService<IOptions<KeycloakConfiguration>>().Value;
        Assert.Equal("http://keycloak.internal:9090", options.ServerUrl);
    }

    [Fact]
    public void AddKeycloakSdk_ConfigureCallback_OverridesValues()
    {
        // Arrange
        var builder = CreateBuilder(new Dictionary<string, string?>
        {
            ["ConnectionStrings:keycloak"] = "http://localhost:8080",
            ["keycloak:RealmName"] = "original-realm",
            ["keycloak:ClientId"] = "c",
            ["keycloak:ClientSecret"] = "s",
            ["keycloak:ClientUuid"] = "u",
            ["keycloak:AdminUsername"] = "a",
            ["keycloak:AdminPassword"] = "p"
        });

        // Act
        builder.AddKeycloakSdk(configure: cfg =>
        {
            cfg.RealmName = "overridden-realm";
            cfg.NumberOfRetries = 5;
        });
        var host = builder.Build();

        // Assert
        var options = host.Services.GetRequiredService<IOptions<KeycloakConfiguration>>().Value;
        Assert.Equal("http://localhost:8080", options.ServerUrl);
        Assert.Equal("overridden-realm", options.RealmName);
        Assert.Equal(5, options.NumberOfRetries);
    }

    [Fact]
    public void AddKeycloakSdk_NoConnectionString_FallsBackToAppsettingsServerUrl()
    {
        // Arrange — no Aspire connection string, URL comes from appsettings section
        var builder = CreateBuilder(new Dictionary<string, string?>
        {
            ["keycloak:ServerUrl"] = "http://manual-url:8080",
            ["keycloak:RealmName"] = "r",
            ["keycloak:ClientId"] = "c",
            ["keycloak:ClientSecret"] = "s",
            ["keycloak:ClientUuid"] = "u",
            ["keycloak:AdminUsername"] = "a",
            ["keycloak:AdminPassword"] = "p"
        });

        // Act
        builder.AddKeycloakSdk();
        var host = builder.Build();

        // Assert
        var options = host.Services.GetRequiredService<IOptions<KeycloakConfiguration>>().Value;
        Assert.Equal("http://manual-url:8080", options.ServerUrl);
    }
}
