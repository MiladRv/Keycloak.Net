using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Microsoft.Extensions.Configuration;
using Polly;

namespace Keycloak.Net.Sdk.Extensions;

public class PollyExtensions
{
    internal static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IConfiguration configuration)
    {
        var keycloakOptions = configuration.GetSection("Keycloak").Get<KeycloakConfiguration>()!;

        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r =>
                    r.StatusCode == HttpStatusCode.RequestTimeout || // 408
                    r.StatusCode == HttpStatusCode.InternalServerError || // 500
                    r.StatusCode == HttpStatusCode.BadGateway || // 502
                    r.StatusCode == HttpStatusCode.ServiceUnavailable || // 503
                    r.StatusCode == HttpStatusCode.GatewayTimeout // 504
            )
            .WaitAndRetryAsync(
                retryCount: keycloakOptions.NumberOfRetries, // Number of retries
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(keycloakOptions.DelayBetweenRetryRequestsInSeconds)); // Delay between each retry
    }
}