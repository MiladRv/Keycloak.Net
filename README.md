# Keycloak.Net.Sdk

[![NuGet](https://img.shields.io/nuget/v/Keycloak.Net.Sdk?label=Keycloak.Net.Sdk)](https://www.nuget.org/packages/Keycloak.Net.Sdk)
[![NuGet](https://img.shields.io/nuget/v/Keycloak.Net.Sdk.Aspire?label=Keycloak.Net.Sdk.Aspire)](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire)
[![NuGet](https://img.shields.io/nuget/v/Keycloak.Net.Sdk.Aspire.Hosting?label=Keycloak.Net.Sdk.Aspire.Hosting)](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire.Hosting)
[![Release SDK](https://github.com/MiladRv/Keycloak.Net/actions/workflows/release-sdk.yml/badge.svg)](https://github.com/MiladRv/Keycloak.Net/actions/workflows/release-sdk.yml)
[![Release Aspire](https://github.com/MiladRv/Keycloak.Net/actions/workflows/release-aspire.yml/badge.svg)](https://github.com/MiladRv/Keycloak.Net/actions/workflows/release-aspire.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A modular .NET SDK for the **Keycloak Admin REST API**: typed interfaces, built-in retry, auto-attached Bearer tokens, and first-class **.NET Aspire** support. Targets **.NET 8**, **.NET 9**, and **.NET 10**.

---

## Why this SDK?

Working with the Keycloak Admin API from .NET means writing boilerplate: managing service-account tokens, attaching `Authorization` headers, handling retries, and wiring everything into the DI container. This SDK takes care of all of that so you can call `IUserManagement`, `IRoleManagement`, etc. directly from your services - no plumbing required.

If you're on **.NET Aspire**, two additional packages wire the SDK automatically from the Aspire connection string with a single `AddKeycloakSdk()` call.

---

## Packages

| Package | Description |
|---------|-------------|
| [`Keycloak.Net.Sdk`](https://www.nuget.org/packages/Keycloak.Net.Sdk) | Core SDK: all managers, token handler, retry |
| [`Keycloak.Net.Sdk.Aspire`](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire) | Client integration: `AddKeycloakSdk()` reads Aspire connection string |
| [`Keycloak.Net.Sdk.Aspire.Hosting`](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire.Hosting) | AppHost integration: `AddKeycloak()` adds Keycloak as a container resource |

---

## Quick Start: Standalone

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
// Inject anywhere via DI
public class MyService(IUserManagement users, IRoleManagement roles)
{
    public async Task CreateUser()
    {
        var result = await users.SignupAsync(new SignupRequestDto
        {
            Username  = "john.doe",
            Email     = "john@example.com",
            FirstName = "John",
            LastName  = "Doe",
            Password  = "Secret@123"
        });
    }
}
```

---

## Quick Start: .NET Aspire

```bash
# AppHost project
dotnet add package Keycloak.Net.Sdk.Aspire.Hosting

# API / service project
dotnet add package Keycloak.Net.Sdk.Aspire
```

```csharp
// AppHost/Program.cs
var keycloak = builder.AddKeycloak("keycloak");

builder.AddProject<Projects.MyApi>("api")
       .WithReference(keycloak)
       .WaitFor(keycloak);
```

```csharp
// MyApi/Program.cs - reads ServerUrl from Aspire connection string automatically
builder.AddKeycloakSdk();
```

```json
// MyApi/appsettings.json - only realm-level settings needed
"keycloak": {
  "RealmName": "my-realm",
  "ClientId": "my-client",
  "ClientSecret": "my-secret",
  "ClientUuid": "my-uuid",
  "AdminUsername": "admin",
  "AdminPassword": "admin"
}
```

Or use `IGroupManagement` to organize users into groups:

## Features

- **Authentication**: sign up / sign in users, service-account token management
- **User management**: get, enable/disable, set password, delete; list/delete credentials
- **Role management**: client roles & realm roles (get, create, assign/remove to users and groups); composite client roles
- **Client management**: get, create, delete clients; enable service accounts; manage client scopes, default/optional scope assignment, and protocol mappers
- **Realm management**: create, get, update, delete realms
- **Group management**: create/delete groups, add/remove users
- **Session management**: get active sessions, revoke a session, logout all devices
- **Resilience**: built-in retry policy via `Microsoft.Extensions.Http.Resilience`
- **Auto Bearer**: `DelegatingHandler` that attaches tokens transparently
- **Full DI**: `IHttpClientFactory`-based, all interfaces injectable

---

## Available Interfaces

| Interface | Responsibilities |
|-----------|-----------------|
| `IUserManagement` | Sign up, sign in, get, enable/disable, set password, delete; list/delete credentials |
| `IRoleManagement` | Client & realm roles: get, create, assign/remove; composite client roles |
| `IClientManagement` | Get, create, delete clients; enable service accounts; client scopes & protocol mappers CRUD; default/optional client scope assignment |
| `IRealmManagement` | Create realms |
| `ITokenManagement` | Get service-account token, revoke |
| `IGroupManagement` | Create/delete groups, add/remove users |
| `IUserSessionManagement` | Get active sessions, revoke, logout all |

---

## Documentation

- [Getting Started](docs/getting-started.md)
- [User Management](docs/user-management.md)
- [Role Management](docs/role-management.md)
- [Client Management](docs/client-management.md)
- [Realm Management](docs/realm-management.md)
- [Group Management](docs/group-management.md)
- [Session Management](docs/session-management.md)
- [.NET Aspire Integration](docs/aspire-integration.md)
- [Changelog](CHANGELOG.md)

---

## Running Tests

### Unit Tests

Unit tests use a fake `HttpMessageHandler`  no external dependencies required.

```bash
# Unit tests - no external dependencies
dotnet test Keycloak.Net.Sdk.UnitTests/Keycloak.Net.Sdk.UnitTests.csproj
```

### Integration Tests

# Integration tests - requires Docker (Testcontainers.Keycloak)
dotnet test Keycloak.Net.Sdk.IntegrationTests/Keycloak.Net.Sdk.IntegrationTests.csproj
```

The fixture automatically handles the full setup sequence:
1. Starts a Keycloak container
2. Creates a dedicated test realm
3. Creates a confidential client with service accounts
4. Grants realm-admin role to the service account
5. Creates a test user, client role, realm role, and group

> The first run pulls the Keycloak Docker image (~500 MB). Subsequent runs reuse the cached image.

### All Tests

```bash
dotnet test
```

---

## Project Structure

```
Keycloak.Net.Sdk/                  # SDK source
├── Authentications/               # TokenProvider, TokenManagement, KeycloakAuthHandler
├── Clients/                       # ClientManagement + DTOs
├── Configurations/                # KeycloakConfiguration
├── Contracts/                     # Shared response types (KeycloakBaseResponse)
├── Extensions/                    # ServiceRegistrations, ExceptionHandler
├── Groups/                        # GroupManagement + DTOs
├── Realms/                        # RealmManagement
├── Roles/                         # RoleManagement + DTOs
├── UserSessions/                  # UserSessionManagement + DTOs
└── Users/                         # UserManagement + DTOs

Keycloak.Net.Sdk.UnitTests/        # Unit tests (Moq, FakeHttpMessageHandler)
Keycloak.Net.Sdk.IntegrationTests/ # Integration tests (Testcontainers.Keycloak)
```

---

## License

[MIT](LICENSE) - Copyright © 2024 Milad Rivandi

Questions or feedback: [miladrivandi73@gmail.com](mailto:miladrivandi73@gmail.com) · [Open an issue](https://github.com/MiladRv/Keycloak.Net/issues)