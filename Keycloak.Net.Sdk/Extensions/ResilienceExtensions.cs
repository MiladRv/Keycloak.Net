using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Keycloak.Net.Sdk.Extensions;

internal static class ResilienceExtensions
{
    internal static IHttpClientBuilder AddKeycloakResilienceHandler(
        this IHttpClientBuilder builder,
        KeycloakConfiguration options)
    {
        builder.AddResilienceHandler("keycloak-retry", resilienceBuilder =>
        {
            resilienceBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.NumberOfRetries,
                Delay = TimeSpan.FromSeconds(options.DelayBetweenRetryRequestsInSeconds),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException ||
                    args.Outcome.Result is
                    {
                        StatusCode: HttpStatusCode.RequestTimeout      // 408
                                 or HttpStatusCode.InternalServerError // 500
                                 or HttpStatusCode.BadGateway          // 502
                                 or HttpStatusCode.ServiceUnavailable  // 503
                                 or HttpStatusCode.GatewayTimeout      // 504
                    })
            });
        });

        return builder;
    }
}
