# Keycloak.Net.Sdk.Aspire.Hosting

.NET Aspire **AppHost** integration for [Keycloak.Net.Sdk](https://www.nuget.org/packages/Keycloak.Net.Sdk). Adds Keycloak as a container resource in your Aspire AppHost so dependent projects receive the server URL automatically.

---

## Installation

```bash
dotnet add package Keycloak.Net.Sdk.Aspire.Hosting
```

## Usage

```csharp
// AppHost/Program.cs
var keycloak = builder.AddKeycloak("keycloak");

builder.AddProject<Projects.MyApi>("api")
       .WithReference(keycloak)
       .WaitFor(keycloak);
```

### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `name` | — | Resource name; also the connection string key in dependent projects |
| `port` | random | Host port mapped to Keycloak's 8080 |
| `adminUsername` | `"admin"` | Keycloak admin username |
| `adminPassword` | `"admin"` | Keycloak admin password |
| `tag` | `"latest"` | Container image tag |

### Realm Import

```csharp
var keycloak = builder.AddKeycloak("keycloak")
                      .WithRealmImport("./realms/my-realm.json");
```

---

For the dependent project setup see [Keycloak.Net.Sdk.Aspire](https://www.nuget.org/packages/Keycloak.Net.Sdk.Aspire).  
Full documentation: [github.com/MiladRv/Keycloak.Net](https://github.com/MiladRv/Keycloak.Net/blob/main/docs/aspire-integration.md)
