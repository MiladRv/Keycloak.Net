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
else on the group are left as they were. Fields you leave `null` are not changed.

## Attributes

Groups can carry attributes. A common use is describing the part of the organisation a group
stands for, for example a company or branch id that an application later uses to filter data.

```csharp
await groups.CreateGroupAsync(new CreateGroupRequestDto
{
    Name       = "acme",
    Attributes = new() { ["company_id"] = ["1"] }
});

// Replace all attributes (other fields stay as they are)
await groups.UpdateGroupAsync(groupId, new UpdateGroupRequestDto
{
    Attributes = new() { ["company_id"] = ["1"], ["region"] = ["north"] }
});

// Clear all attributes
await groups.UpdateGroupAsync(groupId, new UpdateGroupRequestDto { Attributes = [] });

var group = await groups.GetGroupAsync(groupId);
// group.Response.Attributes["company_id"][0] == "1"
```

## Hierarchy

```csharp
// Create a child group
await groups.CreateChildGroupAsync(companyGroupId, new CreateGroupRequestDto
{
    Name       = "tehran-branch",
    Attributes = new() { ["branch_id"] = ["12"] }
});

// List direct children (first/max are optional). Requires Keycloak 23 or later.
var branches = await groups.GetChildGroupsAsync(companyGroupId, first: 0, max: 50);

// Find a group by its full path; each segment is URL-escaped for you
var branch = await groups.GetGroupByPathAsync("/acme/tehran-branch");
```

`GroupResponseDto` also exposes `ParentId` and `SubGroupCount`, which Keycloak 23 and later return.
Since Keycloak 23, `GetGroupsAsync` no longer returns the full tree in `SubGroups`; use
`GetChildGroupsAsync` to walk it.

## Members

```csharp
await groups.AddUserToGroupAsync(userId, groupId);
await groups.RemoveUserFromGroupAsync(userId, groupId);

// Direct members of a group (first/max are optional)
var members = await groups.GetGroupMembersAsync(groupId, first: 0, max: 100);
```

`GetGroupMembersAsync` returns direct members only. Members of child groups are not included.

## Get Groups

```csharp
// All groups in the realm
var allGroups = await groups.GetGroupsAsync();

// A single group by id
var group = await groups.GetGroupAsync(groupId);

// Groups a specific user belongs to
var userGroups = await groups.GetUserGroupsAsync(userId);
```

## Roles on groups

Assigning roles to groups, and reading a user's effective roles inherited through groups,
is part of `IRoleManagement`. See [Role Management](role-management.md#client-roles--groups).
