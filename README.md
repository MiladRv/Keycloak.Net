# Keycloak.Net.Sdk

A modular .NET 8 SDK for integrating with [Keycloak](https://www.keycloak.org/) using `IHttpClientFactory`, typed services, and built-in retry policies.

📦 [NuGet: Keycloak.Net.Sdk](https://www.nuget.org/packages/Keycloak.Net.Sdk)

---

## Features

- Sign up / sign in users
- Manage users (get, enable/disable, set password, delete)
- Manage roles (get client roles, assign/remove roles)
- Manage clients (get, create, delete, enable service accounts)
- Manage client scopes
- Manage realms
- Manage groups (create, delete, get, add/remove users)
- Token management (get service-account token, revoke token)
- Built-in retry policy via Polly
- Auth handler that automatically attaches Bearer tokens to requests
- Fully supports `IHttpClientFactory` and dependency injection

---

## Requirements

- .NET 8+
- A running Keycloak server (v21+)
- A confidential client with **Service Accounts Enabled**

---

## Installation

```bash
dotnet add package Keycloak.Net.Sdk
```

---

## Configuration

### 1. `appsettings.json`

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
| `NumberOfRetries` | Polly retry count (default: 3) |
| `DelayBetweenRetryRequestsInSeconds` | Delay between retries (default: 2) |

### 2. Register Services

```csharp
builder.Services.AddKeycloak(builder.Configuration);
```

---

## Usage

Inject the interface you need:

```csharp
public class MyService(IUserManagement users, IRoleManagement roles)
{
    public async Task CreateAndAssignAsync()
    {
        var signup = await users.SignupAsync(new SignupRequestDto
        {
            Username  = "john.doe",
            Email     = "john@example.com",
            FirstName = "John",
            LastName  = "Doe",
            Password  = "Secret@123"
        });

        await roles.AssignClientRoleToUser(userId: signup.Response.Id, roleId: "role-uuid");
    }
}
```

Or use `IGroupManagement` to organize users into groups:

```csharp
public class GroupService(IGroupManagement groups)
{
    public async Task AssignUserToGroupAsync(string userId, string groupId)
    {
        await groups.AddUserToGroupAsync(userId, groupId);
    }

    public async Task<List<GroupResponseDto>> GetUserGroupsAsync(string userId)
    {
        var result = await groups.GetUserGroupsAsync(userId);
        return result.Response;
    }
}
```

### Available Interfaces

| Interface | Responsibilities |
|-----------|-----------------|
| `IUserManagement` | Sign up, sign in, get user, enable/disable, set password, delete |
| `IRoleManagement` | Get client roles, assign/remove roles to users |
| `IClientManagement` | Get clients, get client scopes, create/delete client, enable service accounts |
| `IRealmManagement` | Create realm |
| `ITokenManagement` | Get service-account token, revoke token |
| `IGroupManagement` | Create/delete group, get groups, add/remove user from group, get user's groups |

---

## Running Tests

### Unit Tests

Unit tests use a fake `HttpMessageHandler` — no external dependencies required.

```bash
dotnet test Keycloak.Net.Sdk.UnitTests/Keycloak.Net.Sdk.UnitTests.csproj
```

### Integration Tests

Integration tests spin up a real Keycloak instance via [Testcontainers](https://dotnet.testcontainers.org/). **Docker must be running.**

```bash
dotnet test Keycloak.Net.Sdk.IntegrationTests/Keycloak.Net.Sdk.IntegrationTests.csproj
```

The fixture automatically handles the full setup sequence:
1. Starts a Keycloak container
2. Creates a dedicated test realm
3. Creates a confidential client with service accounts
4. Grants realm-admin role to the service account
5. Creates a test user, test role, and test group

> The first run pulls the Keycloak Docker image (~500 MB). Subsequent runs reuse the cached image.

### All Tests

```bash
dotnet test
```

---

## Project Structure

```
Keycloak.Net.Sdk/                  # SDK source
├── Athentications/                # TokenProvider, TokenManagement, KeycloakAuthHandler
├── Clients/                       # ClientManagement + DTOs
├── Configurations/                # KeycloakConfiguration
├── Contracts/                     # Shared response types (KeycloakBaseResponse)
├── Extensions/                    # ServiceRegistrations, ExceptionHandler
├── Groups/                        # GroupManagement + DTOs
├── Realms/                        # RealmManagement
├── Roles/                         # RoleManagement + DTOs
└── Users/                         # UserManagement + DTOs

Keycloak.Net.Sdk.UnitTests/        # Unit tests (Moq, FakeHttpMessageHandler)
Keycloak.Net.Sdk.IntegrationTests/ # Integration tests (Testcontainers.Keycloak)
```

---

## License

[MIT](LICENSE)

## Contact

Questions or feedback: [miladrivandi73@gmail.com](mailto:miladrivandi73@gmail.com) or open an issue.
