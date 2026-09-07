# User Management

Inject `IUserManagement`:

```csharp
public class MyService(IUserManagement users)
```

## Sign Up

```csharp
var result = await users.SignupAsync(new SignupRequestDto("john.doe", "Secret@123")
{
    Email     = "john@example.com",
    Firstname = "John",
    Lastname  = "Doe"
});

if (result.IsSuccessful)
{
    // Look the new user up by username to get its id - SignupAsync itself
    // doesn't return the created user's representation.
    var created = await users.GetUserByUsernameAsync("john.doe");
    string userId = created.Response[0].Id;
}
```

## Sign In

```csharp
var token = await users.SigninAsync("john.doe", "Secret@123");
```

## Get User(s)

```csharp
var user = await users.GetUserAsync(userId);

var byUsername = await users.GetUserByUsernameAsync("john.doe");
var byEmail    = await users.GetUsersByEmailAsync("john@example.com");

// Paginated / filtered search across the realm
var page = await users.GetUsersAsync(new GetUsersQueryDto
{
    First  = 0,
    Max    = 20,
    Search = "john"
});
```

## Update

```csharp
await users.UpdateUserAsync(userId, new UpdateUserRequestDto
{
    Email     = "new@example.com",
    FirstName = "John",
    LastName  = "Doe"
});
```

## Enable / Disable

```csharp
await users.EnableUserAsync(userId);
await users.DisableUserAsync(userId);
```

Under the hood these (and `SetUserAttributeAsync`) fetch the user's current
representation first and only flip the field that changed before sending it
back. Keycloak's `PUT` on a user replaces the whole record, so this round
trip is what keeps the rest of the user (email, attributes, required
actions, etc.) intact instead of it getting wiped out.

## Set Password

```csharp
await users.SetUserPasswordAsync(userId, "NewPass@456", temporary: false);
```

## Attributes

```csharp
await users.SetUserAttributeAsync(userId, "department", "engineering");

var attributes = await users.GetUserAttributesAsync(userId);
// attributes.Response["department"][0] == "engineering"
```

## Delete

```csharp
await users.DeleteUserAsync(userId);
```

## Credentials

```csharp
var credentials = await users.GetUserCredentialsAsync(userId);

await users.DeleteUserCredentialAsync(userId, credentials.Response[0].Id);
```
