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
using Microsoft.Extensions.Options;

namespace Keycloak.Net.Sdk.Configurations;

public static partial class ServiceRegistrations
{
    /// <summary>Registers a named connection from a configuration section, using keyed services.</summary>
    public static IServiceCollection AddKeycloak(this IServiceCollection services, string name,
        IConfiguration configuration, Action<KeycloakConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var settings = configuration.Get<KeycloakConfiguration>() ?? new KeycloakConfiguration();
        configure?.Invoke(settings);
        return AddNamedKeycloak(services, name, settings);
    }

    /// <summary>Registers a named connection with its own configuration and token caches.</summary>
    public static IServiceCollection AddKeycloak(this IServiceCollection services, string name,
        Action<KeycloakConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var settings = new KeycloakConfiguration();
        configure(settings);
        return AddNamedKeycloak(services, name, settings);
    }

    private static IServiceCollection AddNamedKeycloak(IServiceCollection services, string name, KeycloakConfiguration settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (services.Any(service => service.ServiceType == typeof(IKeycloakManagement) &&
                                    service.IsKeyedService && Equals(service.ServiceKey, name)))
            throw new InvalidOperationException($"The Keycloak connection '{name}' is already registered.");

        if (!Uri.TryCreate(settings.ServerUrl, UriKind.Absolute, out var serverUri) ||
            (serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException($"Keycloak connection '{name}' requires an absolute HTTP or HTTPS ServerUrl.");

        // Keep a base path when resolving relative endpoint URLs.
        settings.ServerUrl = settings.ServerUrl.TrimEnd('/') + "/";
        services.AddKeyedSingleton<IOptions<KeycloakConfiguration>>(name, Options.Create(settings));
        services.AddKeyedSingleton<IHttpClientFactory>(name, (provider, _) =>
            new NamedKeycloakHttpClientFactory(provider.GetRequiredService<IHttpClientFactory>(), name));

        services.AddKeyedSingleton<ITokenProvider>(name, (provider, _) =>
            new TokenProvider(Factory(provider, name), Settings(provider, name)));
        services.AddKeyedSingleton<IRealmAdminTokenProvider>(name, (provider, _) =>
            new RealmAdminTokenProvider(Factory(provider, name), Settings(provider, name)));

        services.AddHttpClient(NamedKeycloakHttpClientFactory.GetClientName(name, "keycloak"),
                client => client.BaseAddress = new Uri(settings.ServerUrl))
            .AddKeycloakResilienceHandler(settings)
            .AddHttpMessageHandler(provider => new KeycloakAuthHandler(provider.GetRequiredKeyedService<ITokenProvider>(name)));
        foreach (var clientName in new[] { "keycloak-admin", "keycloak-token" })
            services.AddHttpClient(NamedKeycloakHttpClientFactory.GetClientName(name, clientName),
                    client => client.BaseAddress = new Uri(settings.ServerUrl))
                .AddKeycloakResilienceHandler(settings);

        services.AddKeyedScoped<IUserManagement>(name, (provider, _) => new UserManagement(Factory(provider, name), Settings(provider, name)));
        services.AddKeyedScoped<ITokenManagement>(name, (provider, _) => new TokenManagement(Factory(provider, name), Settings(provider, name)));
        services.AddKeyedScoped<IRoleManagement>(name, (provider, _) => new RoleManagement(Factory(provider, name), Settings(provider, name)));
        services.AddKeyedScoped<IRealmManagement>(name, (provider, _) => new RealmManagement(Factory(provider, name), provider.GetRequiredKeyedService<IRealmAdminTokenProvider>(name)));
        services.AddKeyedScoped<IClientManagement>(name, (provider, _) => new ClientManagement(Factory(provider, name), Settings(provider, name)));
        services.AddKeyedScoped<IGroupManagement>(name, (provider, _) => new GroupManagement(Factory(provider, name), Settings(provider, name)));
        services.AddKeyedScoped<IUserSessionManagement>(name, (provider, _) => new UserSessionManagement(Factory(provider, name), Settings(provider, name)));
        services.AddKeyedScoped<IKeycloakManagement>(name, (provider, _) => new KeycloakManagement(
            provider.GetRequiredKeyedService<IUserManagement>(name),
            provider.GetRequiredKeyedService<IRoleManagement>(name),
            provider.GetRequiredKeyedService<ITokenManagement>(name),
            provider.GetRequiredKeyedService<IRealmManagement>(name),
            provider.GetRequiredKeyedService<IClientManagement>(name),
            provider.GetRequiredKeyedService<IGroupManagement>(name),
            provider.GetRequiredKeyedService<IUserSessionManagement>(name)));

        return services;
    }

    private static IHttpClientFactory Factory(IServiceProvider provider, string name)
        => provider.GetRequiredKeyedService<IHttpClientFactory>(name);

    private static IOptions<KeycloakConfiguration> Settings(IServiceProvider provider, string name)
        => provider.GetRequiredKeyedService<IOptions<KeycloakConfiguration>>(name);
}
