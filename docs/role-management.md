# Role Management

Inject `IRoleManagement`:

```csharp
public class MyService(IRoleManagement roles)
```

## Client Roles

```csharp
// Get all client roles
var clientRoles = await roles.GetClientRolesAsync();

// Assign a client role to a user
await roles.AssignClientRoleToUser(userId, roleId);

// Remove a client role from a user
await roles.RemoveClientRoleFromUser(userId, roleId);
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

// Create a realm role
await roles.CreateRealmRoleAsync(new CreateRealmRoleRequestDto
{
    Name        = "admin",
    Description = "Full access role"
});

// Delete a realm role
await roles.DeleteRealmRoleAsync(roleName);

// Assign a realm role to a user
await roles.AssignRealmRoleToUserAsync(userId, roleId, roleName);

// Remove a realm role from a user
await roles.RemoveRealmRoleFromUserAsync(userId, roleId, roleName);

// Assign a realm role to a group
await roles.AssignRealmRoleToGroupAsync(groupId, roleId, roleName);

// Remove a realm role from a group
await roles.RemoveRealmRoleFromGroupAsync(groupId, roleId, roleName);
```
