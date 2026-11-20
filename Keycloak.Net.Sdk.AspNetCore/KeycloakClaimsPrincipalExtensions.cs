using System.Security.Claims;
using System.Text.Json;

namespace Keycloak.Net.Sdk.AspNetCore;

/// <summary>Reads Keycloak-specific information from a validated access token.</summary>
public static class KeycloakClaimsPrincipalExtensions
{
    internal const string RealmAccessClaim    = "realm_access";
    internal const string ResourceAccessClaim = "resource_access";

    /// <summary>The Keycloak user id (<c>sub</c>), or null.</summary>
    public static string? GetKeycloakUserId(this ClaimsPrincipal principal) =>
        principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>The user's realm roles from <c>realm_access.roles</c>.</summary>
    public static IReadOnlyList<string> GetRealmRoles(this ClaimsPrincipal principal)
    {
        using var document = ParseObject(principal.FindFirst(RealmAccessClaim)?.Value);
        return document is null ? [] : ReadRoles(document.RootElement);
    }

    /// <summary>The user's roles on one client from <c>resource_access.{clientId}.roles</c>.</summary>
    public static IReadOnlyList<string> GetClientRoles(this ClaimsPrincipal principal, string clientId)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        using var document = ParseObject(principal.FindFirst(ResourceAccessClaim)?.Value);
        return document is not null && document.RootElement.TryGetProperty(clientId, out var client)
            ? ReadRoles(client)
            : [];
    }

    /// <summary>The user's roles on every client in <c>resource_access</c>, keyed by client id.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetAllClientRoles(this ClaimsPrincipal principal)
    {
        using var document = ParseObject(principal.FindFirst(ResourceAccessClaim)?.Value);
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        if (document is null) return result;
        foreach (var client in document.RootElement.EnumerateObject())
            result[client.Name] = ReadRoles(client.Value);
        return result;
    }

    public static bool HasRealmRole(this ClaimsPrincipal principal, string role) =>
        principal.GetRealmRoles().Contains(role, StringComparer.Ordinal);

    public static bool HasClientRole(this ClaimsPrincipal principal, string clientId, string role) =>
        principal.GetClientRoles(clientId).Contains(role, StringComparer.Ordinal);

    /// <summary>
    /// The values of a claim added by a Keycloak mapper, such as a user-attribute mapper for
    /// <c>branch_id</c>. Multivalued claims (JSON arrays, or the claim repeated) are flattened.
    /// </summary>
    public static IReadOnlyList<string> GetAttributeValues(this ClaimsPrincipal principal, string claimType)
    {
        ArgumentException.ThrowIfNullOrEmpty(claimType);
        var values = new List<string>();
        foreach (var claim in principal.FindAll(claimType))
        {
            var value = claim.Value;
            if (value.TrimStart().StartsWith('[') && TryReadArray(value, values))
                continue;
            values.Add(value);
        }
        return values.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The data scope for one attribute. Users holding any of <paramref name="unrestrictedRoles"/>
    /// (as a role claim or a realm role) get <see cref="DataScope.Unrestricted"/>; everyone else is
    /// limited to the values of <paramref name="claimType"/> in their token, which may be none.
    /// </summary>
    public static DataScope GetDataScope(this ClaimsPrincipal principal, string claimType, params string[] unrestrictedRoles)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return DataScope.None;
        if (unrestrictedRoles.Any(role => principal.IsInRole(role) || principal.HasRealmRole(role)))
            return DataScope.Unrestricted;
        return DataScope.Of(principal.GetAttributeValues(claimType));
    }

    private static JsonDocument? ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object) return document;
            document.Dispose();
        }
        catch (JsonException)
        {
        }
        return null;
    }

    private static IReadOnlyList<string> ReadRoles(JsonElement container)
    {
        if (container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty("roles", out var roles) ||
            roles.ValueKind != JsonValueKind.Array)
            return [];

        return roles.EnumerateArray()
            .Where(r => r.ValueKind == JsonValueKind.String)
            .Select(r => r.GetString()!)
            .ToList();
    }

    private static bool TryReadArray(string json, List<string> values)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return false;
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String) values.Add(item.GetString()!);
                else if (item.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)) values.Add(item.GetRawText());
            }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
