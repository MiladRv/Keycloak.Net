# Keycloak.Net.Sdk

A modular .NET SDK for integrating with [Keycloak](https://www.keycloak.org/) via `IHttpClientFactory`, typed services, and built-in retry policies. Supports **.NET 8** and **.NET 10**.

📦 [Keycloak.Net.Sdk](https://www.nuget.org/packages/Keycloak.Net.Sdk) — core SDK  
📦 [Keycloak.Net.Sdk.Aspire.Hosting](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire.Hosting) — .NET Aspire AppHost integration  
📦 [Keycloak.Net.Sdk.Aspire](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire) — .NET Aspire client integration  

---

## Features

- Sign up / sign in users
- User management — get, enable/disable, set password, delete
- Role management — client roles & realm roles (assign/remove to users and groups)
- Client management — get, create, delete, enable service accounts
- Realm management — create realms
- Group management — create/delete, add/remove users
- Session management — get active sessions, revoke, logout all
- Token management — get service-account token, revoke
- Built-in retry policy via `Microsoft.Extensions.Http.Resilience`
- Auto-attaching Bearer token handler
- Full DI + `IHttpClientFactory` support
- .NET Aspire integration (AppHost + client)

---

## Quick Start

```bash
dotnet add package Keycloak.Net.Sdk
```

```json
// appsettings.json
"keycloak": {
  "ServerUrl": "https://your-keycloak-host/",
  "RealmName": "your-realm",
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret",
  "ClientUuid": "your-client-uuid",
  "AdminUsername": "admin",
  "AdminPassword": "admin-password"
}
```

```csharp
// Program.cs
builder.Services.AddKeycloak(builder.Configuration);
```

```csharp
// Inject and use
public class MyService(IUserManagement users, IRoleManagement roles) { }
```

---

## Documentation

- [Getting Started](docs/getting-started.md)
- [User Management](docs/user-management.md)
- [Role Management](docs/role-management.md)
- [Group Management](docs/group-management.md)
- [Session Management](docs/session-management.md)
- [.NET Aspire Integration](docs/aspire-integration.md)

---

## Running Tests

```bash
# Unit tests (no external dependencies)
dotnet test Keycloak.Net.Sdk.UnitTests/Keycloak.Net.Sdk.UnitTests.csproj

# Integration tests (requires Docker)
dotnet test Keycloak.Net.Sdk.IntegrationTests/Keycloak.Net.Sdk.IntegrationTests.csproj
```

---

## License

[MIT](LICENSE) — Copyright © 2024 Milad.Rv

Questions or feedback: [miladrivandi73@gmail.com](mailto:miladrivandi73@gmail.com) or open an issue.
