# User Management

Inject `IUserManagement`:

```csharp
public class MyService(IUserManagement users)
```

## Sign Up

```csharp
var result = await users.SignupAsync(new SignupRequestDto
{
    Username  = "john.doe",
    Email     = "john@example.com",
    FirstName = "John",
    LastName  = "Doe",
    Password  = "Secret@123"
});

string userId = result.Response.Id;
```

## Sign In

```csharp
var token = await users.SigninAsync(new SigninRequestDto
{
    Username = "john.doe",
    Password = "Secret@123"
});
```

## Get User

```csharp
var user = await users.GetUserAsync(userId);
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
await users.SetPasswordAsync(userId, "NewPass@456");
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
