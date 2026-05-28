using Keycloak.Net.Sdk.Athentications;
using Keycloak.Net.Sdk.Athentications.Contracts;
using Keycloak.Net.Sdk.Clients;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Groups;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.Roles;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.Users;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keycloak.Net.Sdk.Configurations;

public static class ServiceRegistrations
{
    public static IServiceCollection AddKeycloak(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind options
        services.Configure<KeycloakConfiguration>(configuration.GetSection("keycloak"));
        var options = configuration.GetSection("keycloak").Get<KeycloakConfiguration>()
            ?? throw new InvalidOperationException("Keycloak configuration section is missing. Add a 'keycloak' section to appsettings.json.");

        // Register TokenCache
        services.AddSingleton<ITokenProvider, TokenProvider>();

        // Register DelegatingHandler
        services.AddTransient<KeycloakAuthHandler>();

        // Register HttpClient with Polly + DelegatingHandler
        services.AddHttpClient("keycloak", client => { client.BaseAddress = new Uri(options.ServerUrl); })
            .AddPolicyHandler(PollyExtensions.GetRetryPolicy(configuration))
            .AddHttpMessageHandler<KeycloakAuthHandler>();

        // Register HttpClient for RealmManagement (no auth handler  uses master realm admin credentials)
        services.AddHttpClient("keycloak-admin", client => { client.BaseAddress = new Uri(options.ServerUrl); })
            .AddPolicyHandler(PollyExtensions.GetRetryPolicy(configuration));

        // Register HttpClient for TokenProvider (no auth handler  used to fetch service-account tokens)
        services.AddHttpClient("keycloak-token", client => { client.BaseAddress = new Uri(options.ServerUrl); })
            .AddPolicyHandler(PollyExtensions.GetRetryPolicy(configuration));

        // Register managers
        services.AddScoped<IKeycloakManagement, KeycloakManagement>();
        services.AddScoped<IUserManagement, UserManagement>();
        services.AddScoped<ITokenManagement, TokenManagement>();
        services.AddScoped<IRoleManagement, RoleManagement>();
        services.AddScoped<IRealmManagement, RealmManagement>();
        services.AddScoped<IClientManagement, ClientManagement>();
        services.AddScoped<IGroupManagement, GroupManagement>();

        return services;
    }

  
}