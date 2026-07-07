# Realm Management

Inject `IRealmManagement`:

```csharp
public class MyService(IRealmManagement realms)
```

Realm-level operations authenticate with the Keycloak **admin username/password**
(`AdminUsername` / `AdminPassword` in configuration) against the `master` realm,
rather than the SDK's configured client credentials. This is because these
endpoints manage realms themselves, including realms that may not exist yet or
that the configured client has no access to.

## Create / Delete Realms

```csharp
await realms.CreateRealmAsync("my-new-realm");

await realms.DeleteRealmAsync("my-new-realm");
```

## Get Realms

```csharp
// List all realms
var allRealms = await realms.GetRealmsAsync();

// Get a single realm by name
var realm = await realms.GetRealmAsync("my-new-realm");
```

## Update a Realm

```csharp
await realms.UpdateRealmAsync("my-new-realm", new UpdateRealmRequestDto
{
    DisplayName = "My New Realm",
    Enabled = true,
    RegistrationAllowed = true,
    LoginWithEmailAllowed = true
});
```

`UpdateRealmAsync` performs a full replace of the fields defined on
`UpdateRealmRequestDto` (a PUT to the Keycloak Admin API), so any field left at
its default will be written as that default. Fetch the current realm first with
`GetRealmAsync` if you need to preserve existing settings you're not explicitly
changing.
