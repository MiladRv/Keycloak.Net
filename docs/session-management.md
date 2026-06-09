# Session Management

Inject `IUserSessionManagement`:

```csharp
public class MyService(IUserSessionManagement sessions)
```

## Get Active Sessions

```csharp
var result = await sessions.GetUserSessionsAsync(userId);
// result.Response is List<UserSessionResponseDto>
```

## Revoke a Specific Session

```csharp
await sessions.RevokeSessionAsync(sessionId);
```

## Logout from All Devices

```csharp
await sessions.LogoutUserAsync(userId);
```
