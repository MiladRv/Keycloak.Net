# Group Management

Inject `IGroupManagement`:

```csharp
public class MyService(IGroupManagement groups)
```

## Create / Update / Delete

```csharp
await groups.CreateGroupAsync(new CreateGroupRequestDto { Name = "developers" });
await groups.UpdateGroupAsync(groupId, new UpdateGroupRequestDto { Name = "engineers" });
await groups.DeleteGroupAsync(groupId);
```

`UpdateGroupAsync` fetches the group first and merges your changes into it
before sending the full representation back, so sub-groups and anything
else on the group are left as they were.

## Add / Remove Users

```csharp
await groups.AddUserToGroupAsync(userId, groupId);
await groups.RemoveUserFromGroupAsync(userId, groupId);
```

## Get Groups

```csharp
// All groups in the realm
var allGroups = await groups.GetGroupsAsync();

// A single group by id
var group = await groups.GetGroupAsync(groupId);

// Groups a specific user belongs to
var userGroups = await groups.GetUserGroupsAsync(userId);
```
