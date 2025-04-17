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
        var options = configuration.GetSection("Keycloak").Get<KeycloakConfiguration>();

        // Register TokenCache
        services.AddSingleton<ITokenProvider, TokenProvider>();

        // Register DelegatingHandler
        services.AddTransient<KeycloakAuthHandler>();

        // Register HttpClient with Polly + DelegatingHandler
        services.AddHttpClient("Keycloak", client => { client.BaseAddress = new Uri(options.ServerUrl); })
            .AddPolicyHandler(GetRetryPolicy(configuration))
            .AddHttpMessageHandler<KeycloakAuthHandler>();

        // Register managers
        services.AddSingleton<IKeycloakAdminClient, KeycloakAdminClient>();
        services.AddSingleton<IUserManagement, UserManagement>();
        services.AddSingleton<ITokenManagement, TokenManagement>();
        services.AddSingleton<IRoleManagement, RoleManagement>();
        services.AddSingleton<IRealmManagement, RealmManagement>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IConfiguration configuration )
    {
        var keycloakOptions =  configuration.GetSection("Keycloak").Get<KeycloakConfiguration>()!;
        
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: keycloakOptions.NumberOfRetries, // Number of retries
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(keycloakOptions.DelayBetweenRetryRequestsInSeconds)); // Delay between each retry
    }
}