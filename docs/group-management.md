# Group Management

Inject `IGroupManagement`:

```csharp
public class MyService(IGroupManagement groups)
```

## Create / Delete

```csharp
await groups.CreateGroupAsync(new CreateGroupRequestDto { Name = "developers" });
await groups.DeleteGroupAsync(groupId);
```

## Add / Remove Users

```csharp
await groups.AddUserToGroupAsync(userId, groupId);
await groups.RemoveUserFromGroupAsync(userId, groupId);
```

## Get Groups

```csharp
// All groups in the realm
var allGroups = await groups.GetGroupsAsync();

// Groups a specific user belongs to
var userGroups = await groups.GetUserGroupsAsync(userId);
```
