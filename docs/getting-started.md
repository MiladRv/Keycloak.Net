# Getting Started

## Requirements

- .NET 8, .NET 9, or .NET 10
- A running Keycloak server (v21+)
- A confidential client with **Service Accounts Enabled**

## Installation

```bash
dotnet add package Keycloak.Net.Sdk
```

## Configuration

Add a `keycloak` section to `appsettings.json`:

```json
"keycloak": {
  "ServerUrl": "https://your-keycloak-host/",
  "RealmName": "your-realm",
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret",
  "ClientUuid": "your-client-uuid",
  "AdminUsername": "admin",
  "AdminPassword": "admin-password",
  "NumberOfRetries": 3,
  "DelayBetweenRetryRequestsInSeconds": 2
}
```

| Field | Description |
|-------|-------------|
| `ServerUrl` | Keycloak base URL (include trailing slash) |
| `RealmName` | The realm your client belongs to |
| `ClientId` | Client ID (used for service-account token requests) |
| `ClientSecret` | Client secret |
| `ClientUuid` | Client UUID (used in Admin API calls) |
| `AdminUsername` | Master realm admin username (for realm management) |
| `AdminPassword` | Master realm admin password |
| `NumberOfRetries` | Retry count (default: 3) |
| `DelayBetweenRetryRequestsInSeconds` | Delay between retries in seconds (default: 2) |

## Register Services

```csharp
builder.Services.AddKeycloak(builder.Configuration);
```

That's it — all managers are now available via dependency injection.

## Available Interfaces

| Interface | Responsibilities |
|-----------|-----------------|
| `IUserManagement` | Sign up, sign in, get, enable/disable, set password, delete |
| `IRoleManagement` | Client roles & realm roles — get, create, assign/remove |
| `IClientManagement` | Get, create, delete clients; enable service accounts |
| `IRealmManagement` | Create, get, update, delete realms |
| `ITokenManagement` | Get service-account token, revoke token |
| `IGroupManagement` | Create/delete groups, add/remove users |
| `IUserSessionManagement` | Get active sessions, revoke, logout all |
