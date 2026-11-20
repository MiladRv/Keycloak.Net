using System.Security.Claims;
using Keycloak.Net.Sdk.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class KeycloakAuthenticationBuilderExtensions
{
    /// <summary>
    /// Validates Keycloak access tokens with JWT bearer authentication, binding
    /// <see cref="KeycloakJwtOptions"/> from <paramref name="configuration"/> (for example the
    /// <c>Keycloak</c> section also used by <c>AddKeycloak</c>).
    /// </summary>
    public static AuthenticationBuilder AddKeycloakJwtBearer(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string authenticationScheme = JwtBearerDefaults.AuthenticationScheme,
        Action<JwtBearerOptions>? configureJwtBearer = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return builder.AddKeycloakJwtBearer(configuration.Bind, authenticationScheme, configureJwtBearer);
    }

    /// <summary>Validates Keycloak access tokens with JWT bearer authentication.</summary>
    /// <param name="configureJwtBearer">
    /// Runs after the Keycloak defaults, so it can change any JWT bearer setting. Keycloak roles are
    /// still added to the principal if it replaces <see cref="JwtBearerOptions.Events"/>.
    /// </param>
    public static AuthenticationBuilder AddKeycloakJwtBearer(
        this AuthenticationBuilder builder,
        Action<KeycloakJwtOptions> configure,
        string authenticationScheme = JwtBearerDefaults.AuthenticationScheme,
        Action<JwtBearerOptions>? configureJwtBearer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentException.ThrowIfNullOrEmpty(authenticationScheme);

        builder.Services.AddOptions<KeycloakJwtOptions>(authenticationScheme).Configure(configure);

        builder.Services.AddOptions<JwtBearerOptions>(authenticationScheme)
            .Configure<IOptionsMonitor<KeycloakJwtOptions>>((jwt, keycloak) =>
                ApplyDefaults(jwt, keycloak.Get(authenticationScheme)));

        builder.AddJwtBearer(authenticationScheme, configureJwtBearer ?? (_ => { }));

        // Wrap whatever events are configured last, so role mapping survives a replaced Events object.
        builder.Services.AddOptions<JwtBearerOptions>(authenticationScheme)
            .PostConfigure<IOptionsMonitor<KeycloakJwtOptions>>((jwt, keycloak) =>
                AddRoleMapping(jwt, keycloak.Get(authenticationScheme)));

        return builder;
    }

    private static void ApplyDefaults(JwtBearerOptions jwt, KeycloakJwtOptions keycloak)
    {
        keycloak.Validate();

        jwt.Authority            = keycloak.Authority;
        jwt.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
        jwt.MapInboundClaims     = false;

        var parameters = jwt.TokenValidationParameters;
        // The issuer comes from the realm's discovery document, so it matches what Keycloak signs.
        parameters.ValidateIssuer   = true;
        parameters.ValidateAudience = keycloak.ValidateAudience;
        if (keycloak.EffectiveAudience is { } audience)
        {
            jwt.Audience = audience;
            parameters.ValidAudience = audience;
        }
        parameters.NameClaimType = "preferred_username";
        parameters.RoleClaimType = ClaimTypes.Role;
    }

    private static void AddRoleMapping(JwtBearerOptions jwt, KeycloakJwtOptions keycloak)
    {
        jwt.Events ??= new JwtBearerEvents();
        var next = jwt.Events.OnTokenValidated;
        jwt.Events.OnTokenValidated = async context =>
        {
            if (context.Principal is not null)
                KeycloakRoleClaims.Add(context.Principal, keycloak);
            if (next is not null)
                await next(context);
        };
    }
}
