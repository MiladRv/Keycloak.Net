using Aspire.Hosting.ApplicationModel;

namespace Keycloak.Net.Sdk.Aspire.Hosting;

/// <summary>
/// Represents a Keycloak container resource in a .NET Aspire AppHost.
/// </summary>
public sealed class KeycloakResource : ContainerResource, IResourceWithConnectionString
{
    internal const string DefaultContainerImage = "quay.io/keycloak/keycloak";
    internal const string DefaultTag = "latest";
    internal const int DefaultHttpPort = 8080;

    /// <summary>
    /// The primary HTTP endpoint exposed by Keycloak.
    /// </summary>
    public EndpointReference PrimaryEndpoint => new(this, "http");

    /// <summary>
    /// The connection string exposed to dependent projects — the Keycloak server URL.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{PrimaryEndpoint.Property(EndpointProperty.Url)}");

    public KeycloakResource(string name) : base(name) { }
}
