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
