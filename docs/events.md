# Events (Audit Trail)

Keycloak can store two kinds of events for a realm:

- **User events**: logins, failed logins, logouts, password and profile changes, token refreshes, ...
- **Admin events**: every change made through the admin API or console (who changed what, and when)

Inject `IEventManagement`:

```csharp
public class AuditService(IEventManagement events)
```

## Turn events on

Events are only stored after they are enabled for the realm:

```csharp
await events.UpdateEventsConfigAsync(new UpdateEventsConfigRequestDto
{
    EventsEnabled             = true,
    EventsExpiration          = 90 * 24 * 3600,   // delete user events after 90 days (seconds)
    AdminEventsEnabled        = true,
    AdminEventsDetailsEnabled = true              // include the request body in admin events
});

var config = await events.GetEventsConfigAsync();
```

`UpdateEventsConfigAsync` reads the current configuration and changes only the fields you set.
`EventsListeners` and `EnabledEventTypes` replace the existing lists when set.

## User events

```csharp
var failedLogins = await events.GetEventsAsync(new GetEventsQueryDto
{
    Types    = ["LOGIN_ERROR"],
    User     = userId,
    DateFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
    Max      = 100
});

foreach (var e in failedLogins.Response)
    Console.WriteLine($"{e.Timestamp:u} {e.Type} {e.Error} from {e.IpAddress} via {e.ClientId}");
```

Filters: `Types`, `Client` (clientId), `User` (user id), `IpAddress`, `DateFrom`/`DateTo` (whole days),
`First`/`Max`. Results are newest first. `Details` holds event-specific values such as `username`.

## Admin events

```csharp
var roleChanges = await events.GetAdminEventsAsync(new GetAdminEventsQueryDto
{
    ResourceTypes  = ["REALM_ROLE_MAPPING", "CLIENT_ROLE_MAPPING"],
    OperationTypes = ["CREATE", "DELETE"],
    ResourcePath   = $"users/{userId}/*",
    Max            = 50
});

foreach (var e in roleChanges.Response)
    Console.WriteLine($"{e.Timestamp:u} {e.AuthDetails?.UserId} {e.OperationType} {e.ResourcePath}");
```

Filters: `OperationTypes` (`CREATE`, `UPDATE`, `DELETE`, `ACTION`), `ResourceTypes` (`USER`, `GROUP`,
`REALM_ROLE_MAPPING`, ...), `ResourcePath` (supports `*`), `AuthRealm`/`AuthClient`/`AuthUser`/`AuthIpAddress`
(who made the change), `DateFrom`/`DateTo`, `First`/`Max`. `Representation` is filled only when admin
event details are enabled.

## Clear events

```csharp
await events.ClearEventsAsync();       // all user events of the realm
await events.ClearAdminEventsAsync();  // all admin events of the realm
```

## Notes

- Registered by `AddKeycloak` and named connections; not part of `IKeycloakManagement`.
- Reading events needs `view-events`; changing the configuration and clearing need `manage-events`
  (both included in `realm-admin`).
- Keycloak's built-in store is meant for recent history. For long-term auditing, use an event
  listener SPI or export events regularly.
