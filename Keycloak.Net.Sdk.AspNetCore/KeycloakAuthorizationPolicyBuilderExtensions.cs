using Keycloak.Net.Sdk.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Authorization;

public static class KeycloakAuthorizationPolicyBuilderExtensions
{
    /// <summary>Requires any one of these Keycloak realm roles (read from <c>realm_access</c>).</summary>
    public static AuthorizationPolicyBuilder RequireRealmRole(this AuthorizationPolicyBuilder builder, params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(builder);
        EnsureRoles(roles);
        return builder.RequireAuthenticatedUser()
            .RequireAssertion(context => roles.Any(context.User.HasRealmRole));
    }

    /// <summary>Requires any one of these roles on the given client (read from <c>resource_access</c>).</summary>
    public static AuthorizationPolicyBuilder RequireClientRole(this AuthorizationPolicyBuilder builder, string clientId, params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        EnsureRoles(roles);
        return builder.RequireAuthenticatedUser()
            .RequireAssertion(context => roles.Any(role => context.User.HasClientRole(clientId, role)));
    }

    private static void EnsureRoles(string[] roles)
    {
        if (roles is null || roles.Length == 0 || roles.Any(string.IsNullOrEmpty))
            throw new ArgumentException("At least one non-empty role is required.", nameof(roles));
    }
}
