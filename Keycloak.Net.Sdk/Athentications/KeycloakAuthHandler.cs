using System.Net.Http.Headers;
using Keycloak.Net.Sdk.Athentications.Contracts;

namespace Keycloak.Net.Sdk.Athentications;

public class KeycloakAuthHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}