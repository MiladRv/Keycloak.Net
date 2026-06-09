using Keycloak.Net.Sdk.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Keycloak.Net.Sdk.Aspire;

/// <summary>
/// Extension methods for registering Keycloak.Net.Sdk in a .NET Aspire-connected application.
/// </summary>
public static class KeycloakSdkAspireExtensions
{
    /// <summary>
    /// Registers Keycloak.Net.Sdk services, automatically picking up the Keycloak server URL
    /// injected by .NET Aspire via <c>ConnectionStrings__&lt;connectionName&gt;</c>.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionName">
    /// The Aspire resource name used in the AppHost (e.g. "keycloak").
    /// Must match the name passed to <c>builder.AddKeycloak("keycloak")</c> in the AppHost.
    /// Defaults to "keycloak".
    /// </param>
    /// <param name="configure">
    /// Optional callback to override or supplement any <c>KeycloakConfiguration</c> values
    /// after the Aspire connection string and <c>appsettings.json</c> have been applied.
    /// </param>
    /// <remarks>
    /// The remaining settings (<c>RealmName</c>, <c>ClientId</c>, <c>ClientSecret</c>, etc.)
    /// are bound from the <c>keycloak</c> section in <c>appsettings.json</c> / environment
    /// variables as usual. Only <c>ServerUrl</c> is sourced from the Aspire connection string.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Program.cs in the API project
    /// builder.AddKeycloakSdk();
    ///
    /// // If the AppHost resource is named differently:
    /// builder.AddKeycloakSdk("my-keycloak");
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddKeycloakSdk(
        this IHostApplicationBuilder builder,
        string connectionName = "keycloak",
        Action<KeycloakConfiguration>? configure = null)
    {
        // Aspire injects the URL via ConnectionStrings__<name>
        var serverUrl = builder.Configuration.GetConnectionString(connectionName);

        builder.Services.AddKeycloak(builder.Configuration, settings =>
        {
            if (!string.IsNullOrWhiteSpace(serverUrl))
                settings.ServerUrl = serverUrl;

            configure?.Invoke(settings);
        });

        return builder;
    }
}
