# Keycloak.Net.Sdk.Aspire

.NET Aspire **client** integration for [Keycloak.Net.Sdk](https://www.nuget.org/packages/Keycloak.Net.Sdk). Wires the SDK into a dependent project using the Keycloak server URL injected by Aspire — no manual `appsettings.json` URL needed.

---

## Installation

```bash
dotnet add package Keycloak.Net.Sdk.Aspire
```

## Usage

```csharp
// MyApi/Program.cs
builder.AddKeycloakSdk();
```

`AddKeycloakSdk` reads `ConnectionStrings__keycloak` (injected by Aspire via `WithReference`) and uses it as `ServerUrl`. The remaining settings come from `appsettings.json`:

```json
"keycloak": {
  "RealmName": "my-realm",
  "ClientId": "my-client",
  "ClientSecret": "my-secret",
  "ClientUuid": "my-uuid",
  "AdminUsername": "admin",
  "AdminPassword": "admin"
}
```

### Custom Resource Name or Overrides

```csharp
// If the AppHost resource is named differently
builder.AddKeycloakSdk("auth-server");

// Override any setting programmatically
builder.AddKeycloakSdk(configure: cfg => cfg.NumberOfRetries = 5);
```

---

For the AppHost setup see [Keycloak.Net.Sdk.Aspire.Hosting](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire.Hosting).  
Full documentation: [github.com/MiladRv/Keycloak.Net](https://github.com/MiladRv/Keycloak.Net/blob/main/docs/aspire-integration.md)
