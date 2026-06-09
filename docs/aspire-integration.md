# .NET Aspire Integration

Two packages provide first-class [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) support:

| Package | Purpose |
|---------|---------|
| `Keycloak.Net.Sdk.Aspire.Hosting` | AppHost — adds Keycloak as a container resource |
| `Keycloak.Net.Sdk.Aspire` | Dependent projects — wires the SDK using the Aspire-injected URL |

## AppHost Setup

```bash
dotnet add package Keycloak.Net.Sdk.Aspire.Hosting
```

```csharp
// AppHost/Program.cs
var keycloak = builder.AddKeycloak("keycloak");

builder.AddProject<Projects.MyApi>("api")
       .WithReference(keycloak)
       .WaitFor(keycloak);
```

### AddKeycloak Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `name` | — | Resource name; also the connection string key in dependent projects |
| `port` | random | Host port mapped to Keycloak's 8080 |
| `adminUsername` | `"admin"` | Keycloak admin username |
| `adminPassword` | `"admin"` | Keycloak admin password |
| `tag` | `"latest"` | Container image tag |

### Realm Import

Mount a realm export JSON so Keycloak imports it on startup:

```csharp
var keycloak = builder.AddKeycloak("keycloak")
                      .WithRealmImport("./realms/my-realm.json");
```

## Dependent Project Setup

```bash
dotnet add package Keycloak.Net.Sdk.Aspire
```

```csharp
// MyApi/Program.cs
builder.AddKeycloakSdk();
```

`AddKeycloakSdk` reads `ConnectionStrings__keycloak` (injected by Aspire via `WithReference`) and uses it as `ServerUrl`. All other settings come from `appsettings.json` as usual:

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

// Override settings programmatically
builder.AddKeycloakSdk(configure: cfg => cfg.NumberOfRetries = 5);
```
