# Role Management

Inject `IRoleManagement`:

```csharp
public class MyService(IRoleManagement roles)
```

## Client Roles

`GetClientRoles` and the assign/remove methods below operate on the SDK's
configured client (`ClientUuid` in configuration).

```csharp
// Get all roles on the configured client
var clientRoles = await roles.GetClientRolesAsync();

// Assign a client role to a user
await roles.AssignClientRoleToUserAsync(userId, roleId, roleName);

// Remove a client role from a user
await roles.RemoveClientRoleFromUserAsync(userId, roleId, roleName);
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

// Delete a realm role
await roles.DeleteRealmRoleAsync(roleName);
```

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
