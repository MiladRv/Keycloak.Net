using System.Net;
using Keycloak.Net.Sdk.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Keycloak.Net.Sdk;

public static class ServiceRegistrations
{
    public static IServiceCollection AddKeycloak(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind options
        services.Configure<KeycloakConfiguration>(configuration.GetSection("Keycloak"));
        var options = configuration.GetSection("keycloak").Get<KeycloakConfiguration>();

        // Register TokenCache
        services.AddSingleton<ITokenProvider, TokenProvider>();

        // Register DelegatingHandler
        services.AddTransient<KeycloakAuthHandler>();

        // Register HttpClient with Polly + DelegatingHandler
        services.AddHttpClient("keycloak", client => { client.BaseAddress = new Uri(options.ServerUrl); })
            .AddPolicyHandler(GetRetryPolicy(configuration))
            .AddHttpMessageHandler<KeycloakAuthHandler>();

        // Register managers
        services.AddScoped<IKeycloakManagement, KeycloakManagement>();
        services.AddScoped<IUserManagement, UserManagement>();
        services.AddScoped<ITokenManagement, TokenManagement>();
        services.AddScoped<IRoleManagement, RoleManagement>();
        services.AddScoped<IRealmManagement, RealmManagement>();
        services.AddScoped<IClientManagement, ClientManagement>();


        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IConfiguration configuration)
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