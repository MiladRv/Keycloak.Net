using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Keycloak.Net.Sdk.Aspire.Hosting;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class AspireHostingTests
{
    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            DisableDashboard = true
        });

    [Fact]
    public void AddKeycloak_AddsContainerResource_WithDefaultImageAndHttpEndpoint()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var keycloak = builder.AddKeycloak("keycloak");

        // Assert
        var resource = Assert.Single(builder.Resources.OfType<KeycloakResource>());
        Assert.Same(keycloak.Resource, resource);
        Assert.Equal("keycloak", resource.Name);

        var image = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("quay.io/keycloak/keycloak", image.Image);
        Assert.Equal("latest", image.Tag);

        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal("http", endpoint.Name);
        Assert.Equal(8080, endpoint.TargetPort);
        Assert.Null(endpoint.Port);
        Assert.True(endpoint.IsExternal);
    }

    [Fact]
    public void AddKeycloak_WithPortAndTag_UsesThem()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var keycloak = builder.AddKeycloak("keycloak", port: 8081, tag: "26.0");

        // Assert
        var image = Assert.Single(keycloak.Resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("26.0", image.Tag);

        var endpoint = Assert.Single(keycloak.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(8081, endpoint.Port);
    }

    [Fact]
    public async Task AddKeycloak_SetsAdminEnvironmentAndStartDevArgs()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var keycloak = builder.AddKeycloak("keycloak", adminUsername: "root", adminPassword: "secret");

        // Assert
        var env = await GetEnvironmentAsync(keycloak.Resource);
        Assert.Equal("root", env["KEYCLOAK_ADMIN"]);
        Assert.Equal("secret", env["KEYCLOAK_ADMIN_PASSWORD"]);

        var args = await GetArgsAsync(keycloak.Resource);
        Assert.Equal(["start-dev"], args);
    }

    [Fact]
    public async Task AddKeycloak_DefaultsAdminCredentials()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var keycloak = builder.AddKeycloak("keycloak");

        // Assert
        var env = await GetEnvironmentAsync(keycloak.Resource);
        Assert.Equal("admin", env["KEYCLOAK_ADMIN"]);
        Assert.Equal("admin", env["KEYCLOAK_ADMIN_PASSWORD"]);
    }

    [Fact]
    public async Task WithRealmImport_MountsRealmFileReadOnly_AndAddsImportArg()
    {
        // Arrange
        var builder = CreateBuilder();
        var realmFile = Path.Combine(Path.GetTempPath(), "realm-export.json");

        // Act
        var keycloak = builder.AddKeycloak("keycloak").WithRealmImport(realmFile);

        // Assert
        var mount = Assert.Single(keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.Equal(ContainerMountType.BindMount, mount.Type);
        Assert.Equal(realmFile, mount.Source);
        Assert.Equal("/opt/keycloak/data/import/realm.json", mount.Target);
        Assert.True(mount.IsReadOnly);

        var args = await GetArgsAsync(keycloak.Resource);
        Assert.Equal(["start-dev", "--import-realm"], args);
    }

    [Fact]
    public void ConnectionStringExpression_ReferencesHttpEndpointUrl()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var keycloak = builder.AddKeycloak("keycloak");

        // Assert
        var resource = keycloak.Resource;
        Assert.IsAssignableFrom<IResourceWithConnectionString>(resource);
        Assert.Equal("http", resource.PrimaryEndpoint.EndpointName);
        Assert.Equal("{keycloak.bindings.http.url}", resource.ConnectionStringExpression.ValueExpression);
    }

    private static async Task<Dictionary<string, object>> GetEnvironmentAsync(IResource resource)
    {
        var context = new EnvironmentCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));
        foreach (var callback in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await callback.Callback(context);
        return context.EnvironmentVariables;
    }

    private static async Task<List<object>> GetArgsAsync(IResource resource)
    {
        var args = new List<object>();
        var context = new CommandLineArgsCallbackContext(args);
        foreach (var callback in resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
            await callback.Callback(context);
        return args;
    }
}
