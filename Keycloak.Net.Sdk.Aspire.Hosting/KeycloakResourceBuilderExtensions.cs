using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Keycloak.Net.Sdk.Aspire.Hosting;

/// <summary>
/// Extension methods for adding Keycloak to a .NET Aspire AppHost.
/// </summary>
public static class KeycloakResourceBuilderExtensions
{
    /// <summary>
    /// Adds a Keycloak container resource to the AppHost.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">Resource name — also used as the connection string key in dependent projects.</param>
    /// <param name="port">Host port to map to Keycloak's HTTP port (8080). Null lets Aspire assign a random port.</param>
    /// <param name="adminUsername">Keycloak admin username. Defaults to "admin".</param>
    /// <param name="adminPassword">Keycloak admin password. Defaults to "admin".</param>
    /// <param name="tag">Container image tag. Defaults to "latest".</param>
    /// <remarks>
    /// The Keycloak server URL is automatically injected into dependent projects via
    /// <c>ConnectionStrings__&lt;name&gt;</c>. Use <c>.WithReference(keycloak)</c> on each
    /// project that needs it, then call <c>builder.AddKeycloakSdk()</c> in that project's
    /// <c>Program.cs</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var keycloak = builder.AddKeycloak("keycloak");
    /// builder.AddProject&lt;Projects.MyApi&gt;("api")
    ///        .WithReference(keycloak);
    /// </code>
    /// </example>
    public static IResourceBuilder<KeycloakResource> AddKeycloak(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        string? adminUsername = null,
        string? adminPassword = null,
        string? tag = null)
    {
        var resource = new KeycloakResource(name);

        return builder.AddResource(resource)
            .WithImage(KeycloakResource.DefaultContainerImage, tag ?? KeycloakResource.DefaultTag)
            .WithEnvironment("KEYCLOAK_ADMIN", adminUsername ?? "admin")
            .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", adminPassword ?? "admin")
            .WithArgs("start-dev")
            .WithHttpEndpoint(port: port, targetPort: KeycloakResource.DefaultHttpPort, name: "http")
            .WithExternalHttpEndpoints();
    }

    /// <summary>
    /// Mounts a realm JSON file into the container so Keycloak imports it on startup.
    /// </summary>
    /// <param name="builder">The Keycloak resource builder.</param>
    /// <param name="realmFilePath">Absolute path to the realm export JSON file on the host.</param>
    public static IResourceBuilder<KeycloakResource> WithRealmImport(
        this IResourceBuilder<KeycloakResource> builder,
        string realmFilePath)
    {
        return builder
            .WithBindMount(realmFilePath, "/opt/keycloak/data/import/realm.json", isReadOnly: true)
            .WithArgs("--import-realm");
    }
}
