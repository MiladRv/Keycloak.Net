## Project Context

I have an open-source .NET SDK called **Keycloak.Net.Sdk** (NuGet: `Keycloak.Net.Sdk`) that wraps the Keycloak Admin REST API.

**Current solution structure:**
- `Keycloak.Net.Sdk` — core SDK (targets .NET 8 & .NET 10)
- `Keycloak.Net.Sdk.UnitTests` — unit tests (Moq + fake HttpMessageHandler)
- `Keycloak.Net.Sdk.IntegrationTests` — integration tests (Testcontainers.Keycloak)

**Implemented features:**
- Authentication (sign up, sign in, Bearer token handler, service-account token management)
- User management (get, update, enable/disable, set password, delete, attributes, paginated queries)
- Role management (client roles & realm roles — assign/remove to users and groups)
- Client management (get, create, delete, enable service accounts)
- Realm management (create realms)
- Group management (create/delete, add/remove users)
- Session management (get active sessions, revoke, logout all)
- Built-in retry policy via `Microsoft.Extensions.Http.Resilience`
- Full DI + `IHttpClientFactory` support

---

## What I Want to Build Next

Add **.NET Aspire integration** as a new project inside the same solution: `Keycloak.Net.Sdk.Aspire`

It will be published as a **separate NuGet package** (`Keycloak.Net.Sdk.Aspire`) but live in the same repo — same pattern as Npgsql, MassTransit, etc.

**Goal:** Let developers add Keycloak to their Aspire AppHost with minimal config, so that URL and settings are automatically injected into dependent projects — no manual `appsettings.json` needed.

**Desired developer experience:**
```csharp
// AppHost
var keycloak = builder.AddKeycloak("keycloak");
builder.AddProject<Projects.MyApi>("api")
       .WithReference(keycloak);

// MyApi — Program.cs
builder.AddKeycloakSdk();
```
