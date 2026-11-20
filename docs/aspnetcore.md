# Keycloak.Net.Sdk.AspNetCore

ASP.NET Core integration for APIs that accept Keycloak access tokens:

- **JWT bearer validation** configured for a Keycloak realm (`AddKeycloakJwtBearer`)
- **Keycloak roles as role claims**, so `[Authorize(Roles = "...")]` and `User.IsInRole` work with realm roles and client roles
- **Role policies**: `RequireRealmRole`, `RequireClientRole`
- **Data scope**: limit what each user sees by attributes in their token (company, branch, ...), with full access for administrators, and filter `IQueryable`/EF Core queries with `WhereInScope`

It does not need admin credentials and does not depend on `Keycloak.Net.Sdk`, but it reads the same configuration keys.

## Install

```bash
dotnet add package Keycloak.Net.Sdk.AspNetCore
```

## Validate tokens

```json
{
  "Keycloak": {
    "ServerUrl": "https://sso.example.com/",
    "RealmName": "erp",
    "ClientId": "erp-api"
  }
}
```

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakJwtBearer(builder.Configuration.GetSection("Keycloak"));

builder.Services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();
```

| Option | Default | |
|---|---|---|
| `ServerUrl`, `RealmName` | required | The authority is `{ServerUrl}/realms/{RealmName}`; issuer and signing keys come from its discovery document |
| `ClientId` | | The API's client. Its client roles become role claims, and it is the default audience |
| `Audience` | `ClientId` | Value `aud` must contain |
| `ValidateAudience` | `true` | |
| `RequireHttpsMetadata` | `true` | Turn off only for local development over HTTP |
| `MapRealmRoles` | `true` | Add `realm_access.roles` as role claims |
| `MapClientRoles` | `true` | Add the roles of `ClientId` and `RoleClientIds` as role claims |
| `RoleClientIds` | empty | Other clients whose roles become role claims |

Pass a third argument to change any `JwtBearerOptions` setting after the Keycloak defaults. Pass
an `authenticationScheme` to register more than one realm.

**Audience.** Keycloak only adds a client to `aud` when the token is meant for it. Add an
*Audience* mapper to the client that users sign in with (or to a client scope) with
*Included Client Audience* set to your API's client id. With `Keycloak.Net.Sdk`:

```csharp
await clients.CreateProtocolMapperAsync(frontendClientUuid, new CreateProtocolMapperRequestDto
{
    Name           = "erp-api-audience",
    ProtocolMapper = "oidc-audience-mapper",
    Config         = new()
    {
        ["included.client.audience"] = "erp-api",
        ["access.token.claim"]       = "true"
    }
});
```

## Roles

Keycloak puts realm roles in `realm_access.roles` and client roles in
`resource_access.{client}.roles`. Realm roles and the roles of `ClientId` (plus `RoleClientIds`) are
added as standard role claims, so:

```csharp
[Authorize(Roles = "accountant")]
public IActionResult Invoices() => ...;

if (User.IsInRole("invoice-approve")) { ... }
```

Realm and client roles share one namespace once mapped. Use the policies or extensions below when
the source matters:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ErpAdmin",       p => p.RequireRealmRole("erp-admin"));
    options.AddPolicy("ApproveInvoice", p => p.RequireClientRole("finance", "invoice-approve", "finance-admin"));
});

User.GetKeycloakUserId();          // sub
User.GetRealmRoles();
User.GetClientRoles("finance");
User.GetAllClientRoles();          // client id -> roles
User.HasRealmRole("erp-admin");
User.HasClientRole("finance", "invoice-approve");
```

Each policy is satisfied by any one of the listed roles. Composite roles and roles inherited from
groups are already expanded by Keycloak in the token.

## Data scope

Put the data a user may see into their token, for example with a *User Attribute* mapper that
adds the user's `branch_id` attribute as a multivalued claim (or a group attribute through a
script or custom mapper). Then:

```csharp
// Users with the erp-admin role see every branch; everyone else sees their own branches only
var scope = User.GetDataScope("branch_id", unrestrictedRoles: "erp-admin");

var invoices = await db.Invoices
    .WhereInScope(scope, invoice => invoice.BranchId)   // BranchId may be string, int, long, Guid, ...
    .ToListAsync();
```

- `DataScope.IsUnrestricted` — the user holds one of the unrestricted roles; `WhereInScope` leaves the query unchanged
- `DataScope.Values` — the allowed values; `WhereInScope` becomes `WHERE BranchId IN (...)`
- No values (or an anonymous user) means **no rows**: access fails closed
- Values that do not parse as the key type are ignored
- `scope.Allows("12")` checks one value, for example before loading a single record

To apply a scope everywhere, call `GetDataScope` in a scoped service and use it in an EF Core global
query filter.

The claim must be in the **access token**. On Keycloak 24+ the user attribute must also be declared
in the realm's user profile (or unmanaged attributes enabled), otherwise Keycloak does not store it.
