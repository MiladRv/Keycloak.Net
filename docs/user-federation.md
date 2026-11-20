# User Federation (LDAP / Active Directory)

User federation lets the realm use users stored elsewhere, typically an LDAP directory or Active
Directory. Keycloak can read users from it, write new users to it, and import copies into its own
database.

Inject `IUserFederationManagement`:

```csharp
public class DirectorySetup(IUserFederationManagement federation)
```

## Create an LDAP provider

```csharp
var created = await federation.CreateProviderAsync(new CreateUserStorageProviderRequestDto
{
    Name       = "corporate-ad",
    ProviderId = "ldap",
    Config     = new()
    {
        ["enabled"]               = ["true"],
        ["vendor"]                = ["ad"],                    // ad, other, rhds, tivoli, edirectory
        ["connectionUrl"]         = ["ldaps://ad.corp.example:636"],
        ["authType"]              = ["simple"],
        ["bindDn"]                = ["CN=keycloak,OU=Service Accounts,DC=corp,DC=example"],
        ["bindCredential"]        = [bindPassword],
        ["usersDn"]               = ["OU=Staff,DC=corp,DC=example"],
        ["searchScope"]           = ["2"],                     // 1 = one level, 2 = subtree
        ["editMode"]              = ["READ_ONLY"],             // READ_ONLY, WRITABLE, UNSYNCED
        ["importEnabled"]         = ["true"],
        ["usernameLDAPAttribute"] = ["sAMAccountName"],
        ["rdnLDAPAttribute"]      = ["cn"],
        ["uuidLDAPAttribute"]     = ["objectGUID"],
        ["userObjectClasses"]     = ["person, organizationalPerson, user"]
    }
});

var providerId = created.Response.Id;
```

Every config value is a list, as Keycloak stores component settings. `CreateProviderAsync` fills in
the realm id and provider type and returns the provider as Keycloak stored it, including its id.
Keycloak adds default mappers (username, email, first and last name, ...).

## Check the connection

```csharp
var ok = await federation.TestLdapConnectionAsync(providerId);                        // connect and bind
var reachable = await federation.TestLdapConnectionAsync(providerId, authenticate: false); // connect only
```

> **Check the connection before trusting a sync.** When the LDAP server cannot be reached,
> Keycloak still answers a sync with success and `0 imported users`; the error only appears in
> Keycloak's log. `TestLdapConnectionAsync` fails in that case.

## Synchronize users

```csharp
var full    = await federation.SyncUsersAsync(providerId);                     // all users
var changed = await federation.SyncUsersAsync(providerId, changedOnly: true);  // changed since the last sync

Console.WriteLine($"{full.Response.Added} added, {full.Response.Updated} updated, {full.Response.Failed} failed");
```

## Manage the provider

```csharp
var providers = await federation.GetProvidersAsync();
var provider  = await federation.GetProviderAsync(providerId);
var mappers   = await federation.GetProviderMappersAsync(providerId);

await federation.UpdateProviderAsync(providerId, new UpdateUserStorageProviderRequestDto
{
    Config = new() { ["connectionTimeout"] = ["5000"] }
});

await federation.RemoveImportedUsersAsync(providerId); // delete the imported copies from Keycloak
await federation.UnlinkUsersAsync(providerId);         // keep imported users as regular Keycloak users
await federation.DeleteProviderAsync(providerId);      // imported users that are still linked are removed
```

`UpdateProviderAsync` reads the provider and changes only what you set; `Config` keys are merged.
The bind password comes back masked and is sent back masked, so Keycloak keeps the stored one.

## Notes

- Registered by `AddKeycloak` and named connections; not part of `IKeycloakManagement`.
- Requires `manage-realm` / `manage-users` (included in `realm-admin`).
- Kerberos and custom user storage providers work the same way with their own `ProviderId` and config;
  `TestLdapConnectionAsync` is LDAP only.
