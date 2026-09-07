using Keycloak.Net.Sdk.Authentications;
using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Clients;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Extensions;
using Keycloak.Net.Sdk.Groups;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Realms;
using Keycloak.Net.Sdk.Realms.Contracts;
using Keycloak.Net.Sdk.Roles;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.UserSessions;
using Keycloak.Net.Sdk.UserSessions.Contracts;
using Keycloak.Net.Sdk.Users;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keycloak.Net.Sdk.Configurations;

public static class ServiceRegistrations
{
    public static IServiceCollection AddKeycloak(this IServiceCollection services, IConfiguration configuration, Action<KeycloakConfiguration>? configure = null)
    {
        // Bind options
        services.Configure<KeycloakConfiguration>(configuration.GetSection("keycloak"));
        if (configure is not null)
            services.PostConfigure<KeycloakConfiguration>(configure);

        var options = configuration.GetSection("keycloak").Get<KeycloakConfiguration>()
            ?? new KeycloakConfiguration();
        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.ServerUrl))
            throw new InvalidOperationException("Keycloak ServerUrl is not configured. Provide it via 'keycloak' config section or the configure callback.");

        // Register TokenCache
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddSingleton<IRealmAdminTokenProvider, RealmAdminTokenProvider>();

        // Register DelegatingHandler
        services.AddTransient<KeycloakAuthHandler>();

        // Register HttpClient with resilience + DelegatingHandler
        services.AddHttpClient("keycloak", client => { client.BaseAddress = new Uri(options.ServerUrl); })
            .AddKeycloakResilienceHandler(options)
            .AddHttpMessageHandler<KeycloakAuthHandler>();

        // Register HttpClient for RealmManagement (no auth handler - uses master realm admin credentials)
        services.AddHttpClient("keycloak-admin", client => { client.BaseAddress = new Uri(options.ServerUrl); })
            .AddKeycloakResilienceHandler(options);

        // Register HttpClient for TokenProvider (no auth handler - used to fetch service-account tokens)
        services.AddHttpClient("keycloak-token", client => { client.BaseAddress = new Uri(options.ServerUrl); })
            .AddKeycloakResilienceHandler(options);

        // Register managers
        services.AddScoped<IKeycloakManagement, KeycloakManagement>();
        services.AddScoped<IUserManagement, UserManagement>();
        services.AddScoped<ITokenManagement, TokenManagement>();
        services.AddScoped<IRoleManagement, RoleManagement>();
        services.AddScoped<IRealmManagement, RealmManagement>();
        services.AddScoped<IClientManagement, ClientManagement>();
        services.AddScoped<IGroupManagement, GroupManagement>();
        services.AddScoped<IUserSessionManagement, UserSessionManagement>();

        return services;
    }

  
}