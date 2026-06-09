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

## Set Password

```csharp
await users.SetPasswordAsync(userId, "NewPass@456");
```

## Delete

```csharp
await users.DeleteUserAsync(userId);
```
