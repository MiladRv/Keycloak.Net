# Role Management

Inject `IRoleManagement`:

```csharp
public class MyService(IRoleManagement roles)
```

## Client Roles

Every client-role method has two overloads:

- **Without `clientUuid`**: operates on the SDK's configured client (`ClientUuid` in configuration).
- **With `clientUuid` as the first parameter**: operates on any client in the realm. This is the
  client's internal id (`ClientResponseDto.Id`), not its `clientId`.

This lets one application manage roles for several clients, for example an ERP with separate
`finance` and `inventory` clients:

```csharp
var clients    = await clientManagement.GetClientsAsync();
var financeUuid = clients.Response.Single(c => c.ClientId == "finance").Id;

await roles.CreateClientRoleAsync(financeUuid, new CreateClientRoleRequestDto { Name = "invoice-approve" });
```

### Reading and editing client roles

```csharp
// Get all roles on the configured client / on another client
var clientRoles  = await roles.GetClientRolesAsync();
var financeRoles = await roles.GetClientRolesAsync(financeUuid);

// Get one role, including its attributes
var role = await roles.GetClientRoleAsync(financeUuid, "invoice-approve");

// Create a role (description and attributes are optional)
await roles.CreateClientRoleAsync(financeUuid, new CreateClientRoleRequestDto
{
    Name        = "invoice-approve",
    Description = "Approve invoices",
    Attributes  = new() { ["module"] = ["finance"] }
});

// Update a role. Name and description are replaced. Attributes are replaced only
// when Attributes is not null; pass an empty dictionary to clear them.
await roles.UpdateClientRoleAsync(financeUuid, "invoice-approve", new UpdateClientRoleRequestDto
{
    Name        = "invoice-approve",
    Description = "Approve any invoice"
});

// Delete a role
await roles.DeleteClientRoleAsync(financeUuid, "invoice-approve");

// List the users that hold a role directly (first/max are optional)
var holders = await roles.GetClientRoleUsersAsync(financeUuid, "invoice-approve", first: 0, max: 50);
```

`ClientRoleResponseDto.Attributes` is filled by `GetClientRoleAsync`. Keycloak leaves it out of
list responses, so it is `null` there.

### Client Roles ↔ Users

```csharp
// Get a user's roles on a client
var userRoles = await roles.GetUserClientRolesAsync(financeUuid, userId);

// Assign a client role to a user
await roles.AssignClientRoleToUserAsync(financeUuid, userId, roleId, roleName);

// Remove a client role from a user
await roles.RemoveClientRoleFromUserAsync(financeUuid, userId, roleId, roleName);

// The same calls without financeUuid use the configured client
await roles.AssignClientRoleToUserAsync(userId, roleId, roleName);
```

### Composite Client Roles

A composite role bundles other roles so that assigning it grants all of its
composites too.

```csharp
// Get the composite roles that make up a client role
var composites = await roles.GetClientRoleCompositesAsync(roleName);

// Add composites to a client role
await roles.AddClientRoleCompositesAsync(roleName,
[
    new CompositeRoleRequestDto { Id = otherRoleId, Name = otherRoleName }
]);

// Remove composites from a client role
await roles.RemoveClientRoleCompositesAsync(roleName,
[
    new CompositeRoleRequestDto { Id = otherRoleId, Name = otherRoleName }
]);

// Each composite method also accepts a clientUuid first
var financeComposites = await roles.GetClientRoleCompositesAsync(financeUuid, "senior-accountant");
```

## Realm Roles

```csharp
// Get all realm roles
var realmRoles = await roles.GetRealmRolesAsync();

// Get a single realm role by name
var realmRole = await roles.GetRealmRoleAsync(roleName);

// Create a realm role
await roles.CreateRealmRoleAsync(new CreateRealmRoleRequestDto
{
    Name        = "admin",
    Description = "Full access role"
});

// Update a realm role. Name and description are replaced. Attributes are replaced only
// when Attributes is not null; pass an empty dictionary to clear them.
await roles.UpdateRealmRoleAsync(roleName, new UpdateRealmRoleRequestDto
{
    Name        = roleName,
    Description = "Full access role",
    Attributes  = new() { ["scope_level"] = ["company"] }
});

// Delete a realm role
await roles.DeleteRealmRoleAsync(roleName);
```

`CreateRealmRoleRequestDto` also accepts `Attributes`. `RealmRoleResponseDto.Attributes` is
filled by `GetRealmRoleAsync`; Keycloak leaves it out of list responses.

### Composite Realm Roles

A composite realm role can bundle realm roles and client roles from any client. A common pattern
is a job-title role (for example `accountant`) made of fine-grained permission roles, so that
assigning the job title grants all of them.

```csharp
// Add realm roles and client roles in one call (by id and name)
await roles.AddRealmRoleCompositesAsync("accountant",
[
    new CompositeRoleRequestDto { Id = reportViewer.Id,  Name = reportViewer.Name },  // realm role
    new CompositeRoleRequestDto { Id = invoiceApprove.Id, Name = invoiceApprove.Name } // client role
]);

// Read the realm roles, and one client's roles, that a composite contains
var realmComposites  = await roles.GetRealmRoleCompositesAsync("accountant");
var clientComposites = await roles.GetRealmRoleClientCompositesAsync(financeUuid, "accountant");

// Remove composites
await roles.RemoveRealmRoleCompositesAsync("accountant",
[
    new CompositeRoleRequestDto { Id = invoiceApprove.Id, Name = invoiceApprove.Name }
]);
```

Users holding the composite see its contents in their [effective roles](#effective-roles).

### Realm Roles ↔ Users

```csharp
// Get the realm roles assigned to a user
var userRoles = await roles.GetUserRealmRolesAsync(userId);

// Assign a realm role to a user
await roles.AssignRealmRoleToUserAsync(userId, roleId, roleName);

// Remove a realm role from a user
await roles.RemoveRealmRoleFromUserAsync(userId, roleId, roleName);
```

### Realm Roles ↔ Groups

```csharp
// Get the realm roles assigned to a group
var groupRoles = await roles.GetGroupRealmRolesAsync(groupId);

// Assign a realm role to a group
await roles.AssignRealmRoleToGroupAsync(groupId, roleId, roleName);

// Remove a realm role from a group
await roles.RemoveRealmRoleFromGroupAsync(groupId, roleId, roleName);
```

## Client Roles ↔ Groups

Like the other client-role methods, each one has an overload without `clientUuid` that uses the
configured client.

```csharp
// Get a group's roles on a client
var groupClientRoles = await roles.GetGroupClientRolesAsync(financeUuid, groupId);

// Assign a client role to a group: every member of the group (and of its child groups) gets it
await roles.AssignClientRoleToGroupAsync(financeUuid, groupId, roleId, roleName);

// Remove a client role from a group
await roles.RemoveClientRoleFromGroupAsync(financeUuid, groupId, roleId, roleName);
```

## Effective Roles

The methods above return roles mapped **directly**. Effective roles also include roles that come
from group membership and from composite roles, which is what ends up in the user's token.

```csharp
// Everything a user effectively has
var realmRoles  = await roles.GetUserEffectiveRealmRolesAsync(userId);
var clientRoles = await roles.GetUserEffectiveClientRolesAsync(financeUuid, userId);

// What a group effectively grants
var groupRealmRoles  = await roles.GetGroupEffectiveRealmRolesAsync(groupId);
var groupClientRoles = await roles.GetGroupEffectiveClientRolesAsync(financeUuid, groupId);
```

| Method | Direct mappings | Via groups | Via parent groups | Via composites |
|---|---|---|---|---|
| `GetUserEffectiveRealmRolesAsync` | ✓ | ✓ | ✓ | ✓ |
| `GetUserEffectiveClientRolesAsync` | ✓ | ✓ | ✓ | ✓ |
| `GetGroupEffectiveRealmRolesAsync` | ✓ | – | ✓ | ✓ |
| `GetGroupEffectiveClientRolesAsync` | ✓ | – | ✗ | ✓ |

Keycloak's group-level client-role composite endpoint does not include roles from parent groups,
while the realm-level one does. To answer "what does a member of this group get on this client?",
check a member with `GetUserEffectiveClientRolesAsync`.
