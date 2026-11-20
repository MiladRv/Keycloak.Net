# Keycloak.Net.Sdk.Aspire.Hosting

.NET Aspire **AppHost** integration for [Keycloak.Net.Sdk](https://www.nuget.org/packages/Keycloak.Net.Sdk). Adds Keycloak as a container resource in your Aspire AppHost so dependent projects receive the server URL automatically.

---

## Requirements

- An Aspire **13** AppHost. Since 1.1.0 this package depends on `Aspire.Hosting` 13.6.5 or later. Version 1.0.0 depended on `Aspire.Hosting` 9.0.0, which has the high-severity advisory [GHSA-f7vh-h5g3-7jrh](https://github.com/advisories/GHSA-f7vh-h5g3-7jrh). AppHosts still on Aspire 9 must upgrade the Aspire SDK and hosting packages to 13.x before taking this version.
- Targets .NET 8, .NET 9 and .NET 10; no target framework was dropped. `Aspire.Hosting` 13 brings in `Microsoft.Extensions.*` 10.x packages, also on .NET 8.

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
