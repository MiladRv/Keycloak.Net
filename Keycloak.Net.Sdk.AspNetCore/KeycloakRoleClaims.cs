using System.Security.Claims;

namespace Keycloak.Net.Sdk.AspNetCore;

internal static class KeycloakRoleClaims
{
    /// <summary>Adds Keycloak realm roles and selected client roles as role claims, without duplicates.</summary>
    public static void Add(ClaimsPrincipal principal, KeycloakJwtOptions options)
    {
        var identity = principal.Identities.FirstOrDefault(i => i.IsAuthenticated) ?? principal.Identities.FirstOrDefault();
        if (identity is null) return;

        var roles = new List<string>();
        if (options.MapRealmRoles)
            roles.AddRange(principal.GetRealmRoles());
        if (options.MapClientRoles)
            foreach (var clientId in options.ClientsWithMappedRoles)
                roles.AddRange(principal.GetClientRoles(clientId));

        foreach (var role in roles.Distinct(StringComparer.Ordinal))
            if (!identity.HasClaim(identity.RoleClaimType, role))
                identity.AddClaim(new Claim(identity.RoleClaimType, role, ClaimValueTypes.String, "keycloak"));
    }
}
