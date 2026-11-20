namespace Keycloak.Net.Sdk.Extensions;

internal sealed class NamedKeycloakHttpClientFactory(IHttpClientFactory factory, string registrationName)
    : IHttpClientFactory
{
    internal static string GetClientName(string registrationName, string clientName)
        => $"keycloak:{registrationName}:{clientName}";

    public HttpClient CreateClient(string name) => factory.CreateClient(GetClientName(registrationName, name));
}
