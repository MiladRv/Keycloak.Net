# Keycloak.Net.Sdk

[![NuGet](https://img.shields.io/nuget/v/Keycloak.Net.Sdk?label=Keycloak.Net.Sdk)](https://www.nuget.org/packages/Keycloak.Net.Sdk)
[![NuGet](https://img.shields.io/nuget/v/Keycloak.Net.Sdk.Aspire?label=Keycloak.Net.Sdk.Aspire)](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire)
[![NuGet](https://img.shields.io/nuget/v/Keycloak.Net.Sdk.Aspire.Hosting?label=Keycloak.Net.Sdk.Aspire.Hosting)](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire.Hosting)
[![NuGet](https://img.shields.io/nuget/v/Keycloak.Net.Sdk.AspNetCore?label=Keycloak.Net.Sdk.AspNetCore)](https://www.nuget.org/packages/Keycloak.Net.Sdk.AspNetCore)
[![Release SDK](https://github.com/MiladRv/Keycloak.Net/actions/workflows/release-sdk.yml/badge.svg)](https://github.com/MiladRv/Keycloak.Net/actions/workflows/release-sdk.yml)
[![Release Aspire](https://github.com/MiladRv/Keycloak.Net/actions/workflows/release-aspire.yml/badge.svg)](https://github.com/MiladRv/Keycloak.Net/actions/workflows/release-aspire.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A modular .NET SDK for the **Keycloak Admin REST API**: typed interfaces, built-in retry, auto-attached Bearer tokens, and first-class **.NET Aspire** support. Targets **.NET 8**, **.NET 9**, and **.NET 10**.

---

## Upgrading to 2.0

Version 2.0 includes API changes. See the [migration guide](docs/migration-2.0.md) before upgrading from 1.x.

---

## Features on develop

These additions are available on `develop` and have not been released on NuGet yet:

- [User emails](docs/user-management.md#user-emails): email verification, password reset, and required action emails.
- [Multiple realms](docs/multiple-realms.md): named connections with separate configuration and token caches.
- [User streaming](docs/user-management.md#stream-users): `GetAllUsersAsync` with automatic pagination and cancellation.
- [Authorization Services](docs/authorization-services.md): `IAuthorizationManagement` for resources, scopes, policies and permissions, and permission checks.
- [ASP.NET Core package](docs/aspnetcore.md): `Keycloak.Net.Sdk.AspNetCore` validates Keycloak tokens in your API, maps Keycloak roles to role claims, and filters data by token attributes.

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
| [`Keycloak.Net.Sdk.AspNetCore`](https://www.nuget.org/packages/Keycloak.Net.Sdk.AspNetCore) | API side: `AddKeycloakJwtBearer()`, Keycloak roles as role claims, role policies, data scope (`WhereInScope`) |

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
        var result = await users.SignupAsync(new SignupRequestDto("john.doe", "Secret@123")
        {
            Email     = "john@example.com",
            Firstname = "John",
            Lastname  = "Doe"
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
- **User management**: get, search (including by attribute), count, enable/disable, set password, delete; list/delete credentials
- **Role management**: client roles on any client in the realm (CRUD, assign/remove to users and groups, list a role's users); realm roles (CRUD with attributes, assign/remove to users and groups); composite client and realm roles; effective (inherited) roles for users and groups
- **Client management**: get, create, delete clients; enable service accounts; manage client scopes, default/optional scope assignment, and protocol mappers
- **Realm management**: create, get, update, delete realms
- **Group management**: create/update/delete groups with attributes, child groups and lookup by path, add/remove and list members
- **Session management**: get active sessions, revoke a session, logout all devices
- **Identity providers**: external logins (OIDC, SAML, social) with CRUD, mappers, and linking users to external accounts
- **User federation**: LDAP / Active Directory providers: CRUD, connection test, full and changed-users sync, remove or unlink imported users
- **Events**: enable and configure the realm's audit trail; query user events (logins, failures, ...) and admin events (who changed what); clear events
- **Authorization Services**: resource servers, resources, scopes, role/group/user policies, resource and scope permissions; check a user's permissions (UMA), list granted permissions, get an RPT, admin-side evaluation
- **Resilience**: built-in retry policy via `Microsoft.Extensions.Http.Resilience`
- **Auto Bearer**: `DelegatingHandler` that attaches tokens transparently
- **Full DI**: `IHttpClientFactory`-based, all interfaces injectable

---

## Available Interfaces

| Interface | Responsibilities |
|-----------|-----------------|
| `IUserManagement` | Sign up, sign in, get, search by attribute, count, enable/disable, set password, delete; list/delete credentials |
| `IRoleManagement` | Client roles on any client (CRUD, user/group mappings, composites), realm roles (CRUD, user/group mappings, composites), effective roles |
| `IClientManagement` | Get, create, delete clients; enable service accounts; client scopes & protocol mappers CRUD; default/optional client scope assignment |
| `IRealmManagement` | Create realms |
| `ITokenManagement` | Get service-account token, revoke |
| `IGroupManagement` | Groups with attributes, hierarchy (children, by path), members |
| `IUserSessionManagement` | Get active sessions, revoke, logout all |
| `IIdentityProviderManagement` | Identity providers, their mappers, users' linked external accounts |
| `IUserFederationManagement` | LDAP / user storage providers, connection test, sync |
| `IEventManagement` | Events configuration, user events, admin events, clear |
| `IAuthorizationManagement` | Authorization Services: resources, scopes, policies, permissions, permission checks and evaluation |

---

## Documentation

- [Getting Started](docs/getting-started.md)
- [User Management](docs/user-management.md)
- [Multiple Realms](docs/multiple-realms.md)
- [Role Management](docs/role-management.md)
- [Client Management](docs/client-management.md)
- [Realm Management](docs/realm-management.md)
- [Group Management](docs/group-management.md)
- [Session Management](docs/session-management.md)
- [Authorization Services](docs/authorization-services.md)
- [Events](docs/events.md)
- [Identity Providers](docs/identity-providers.md)
- [User Federation](docs/user-federation.md)
- [.NET Aspire Integration](docs/aspire-integration.md)
- [ASP.NET Core Integration](docs/aspnetcore.md)
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

```bash
# Integration tests - requires Docker (Testcontainers.Keycloak)
dotnet test Keycloak.Net.Sdk.IntegrationTests/Keycloak.Net.Sdk.IntegrationTests.csproj
```

The fixture automatically handles the full setup sequence:
1. Starts Keycloak and Mailpit containers on an isolated Docker network
2. Creates two dedicated test realms and configures local SMTP delivery
3. Creates confidential clients with service accounts
4. Grants realm-admin roles to the service accounts
5. Creates test users, a client role, a realm role, and a group

Email tests capture messages in Mailpit. No emails are sent to external recipients.

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
├── IdentityProviders/             # IdentityProviderManagement + DTOs
├── UserFederation/                # UserFederationManagement (LDAP, sync) + DTOs
├── Events/                        # EventManagement (user and admin events) + DTOs
├── Authorization/                 # AuthorizationManagement (Authorization Services) + DTOs
├── Clients/                       # ClientManagement + DTOs
├── Configurations/                # KeycloakConfiguration
├── Contracts/                     # Shared response types (KeycloakBaseResponse)
├── Extensions/                    # ServiceRegistrations, ExceptionHandler
├── Groups/                        # GroupManagement + DTOs
├── Realms/                        # RealmManagement
├── Roles/                         # RoleManagement + DTOs
├── UserSessions/                  # UserSessionManagement + DTOs
└── Users/                         # UserManagement + DTOs

Keycloak.Net.Sdk.AspNetCore/       # API-side package: JWT bearer, role claims, data scope.Net.Sdk.UnitTests/        # Unit tests (Moq, FakeHttpMessageHandler, in-memory TestServer)
Keycloak.Net.Sdk.IntegrationTests/ # Integration tests (Testcontainers.Keycloak)
```

---

## License

[MIT](LICENSE) - Copyright © 2024 Milad Rivandi

Questions or feedback: [miladrivandi73@gmail.com](mailto:miladrivandi73@gmail.com) · [Open an issue](https://github.com/MiladRv/Keycloak.Net/issues)
