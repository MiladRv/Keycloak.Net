# Multiple Realms

Register a named connection for each realm. Connections can point to the same Keycloak server or different servers, and each has its own configuration and token caches.

```csharp
using Keycloak.Net.Sdk.Configurations;

builder.Services.AddKeycloak("store", builder.Configuration.GetSection("KeycloakRealms:Store"));
builder.Services.AddKeycloak("partners", builder.Configuration.GetSection("KeycloakRealms:Partners"));
```

Named registration binds the configuration section passed to it directly:

```json
{
  "KeycloakRealms": {
    "Store": {
      "ServerUrl": "https://keycloak.example.com/",
      "RealmName": "store",
      "ClientId": "store-admin",
      "ClientSecret": "store-secret"
    },
    "Partners": {
      "ServerUrl": "https://keycloak.example.com/",
      "RealmName": "partners",
      "ClientId": "partners-admin",
      "ClientSecret": "partners-secret"
    }
  }
}
```

You can also configure a named connection with a callback:

```csharp
builder.Services.AddKeycloak("store", settings =>
{
    settings.ServerUrl = "https://keycloak.example.com/";
    settings.RealmName = "store";
    settings.ClientId = "store-admin";
    settings.ClientSecret = builder.Configuration["StoreClientSecret"]!;
});
```

## Resolve a Connection

All manager interfaces and `IKeycloakManagement` are registered as keyed services. Use constructor injection for a fixed connection:

```csharp
using Keycloak.Net.Sdk.Users.Contracts;
using Keycloak.Net.Sdk.Contracts.Responses;
using Microsoft.Extensions.DependencyInjection;

public class StoreService([FromKeyedServices("store")] IUserManagement users)
{
    public Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetUsersAsync()
        => users.GetUsersAsync();
}
```

For a connection selected at runtime, resolve it inside a service scope:

```csharp
using var scope = serviceProvider.CreateScope();
var keycloak = scope.ServiceProvider.GetRequiredKeyedService<IKeycloakManagement>(connectionName);
var users = await keycloak.UserManagement.GetUsersAsync();
```

Connection names must be unique and registered at startup. Unknown names fail during service resolution. Manager lifetimes remain scoped, while each connection's token providers are singletons.

The existing unnamed `AddKeycloak(configuration)` registration can be used alongside named connections. Configure `ClientUuid` for client role operations and admin credentials for master realm management, as with an unnamed connection. Named configuration is read at registration time; changing it requires rebuilding the application's service registrations.
