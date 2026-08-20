# Client Management

Inject `IClientManagement`:

```csharp
public class MyService(IClientManagement clients)
```

## Create / Delete Clients

```csharp
await clients.CreateClientAsync(new CreateClientRequestDto
{
    ClientId = "my-app",
    Name     = "My App",
    Enabled  = true
});

await clients.DeleteClientAsync(clientUuid);
```

## Get Clients

```csharp
var allClients = await clients.GetClientsAsync();
```

## Enable Service Account

```csharp
await clients.EnableServiceAccountAsync(clientUuid);
```

## Client Scopes

Client scopes are realm-level resources that group protocol mappers and role
scope mappings, which can then be shared across multiple clients.

```csharp
// List all client scopes in the realm
var scopes = await clients.GetClientScopes();

// Get a single scope by id
var scope = await clients.GetClientScopeAsync(scopeId);

// Create a new scope
await clients.CreateClientScopeAsync(new CreateClientScopeRequestDto
{
    Name        = "custom-scope",
    Description = "Custom claims for partner integrations"
});

// Update an existing scope
await clients.UpdateClientScopeAsync(scopeId, new UpdateClientScopeRequestDto
{
    Name        = "custom-scope",
    Description = "Updated description"
});

// Delete a scope
await clients.DeleteClientScopeAsync(scopeId);
```

### Default / Optional Client Scopes

Assign a realm-level client scope to a specific client, either as a default
scope (always included) or an optional scope (must be requested).

```csharp
// Default scopes
var defaultScopes = await clients.GetDefaultClientScopesAsync(clientUuid);
await clients.AddDefaultClientScopeAsync(clientUuid, scopeId);
await clients.RemoveDefaultClientScopeAsync(clientUuid, scopeId);

// Optional scopes
var optionalScopes = await clients.GetOptionalClientScopesAsync(clientUuid);
await clients.AddOptionalClientScopeAsync(clientUuid, scopeId);
await clients.RemoveOptionalClientScopeAsync(clientUuid, scopeId);
```

## Protocol Mappers

Protocol mappers control which claims and assertions end up in a client's
tokens (e.g. mapping a user attribute to a custom claim).

```csharp
// List all protocol mappers on a client
var mappers = await clients.GetProtocolMappersAsync(clientUuid);

// Get a single mapper by id
var mapper = await clients.GetProtocolMapperAsync(clientUuid, mapperId);

// Create a new mapper
await clients.CreateProtocolMapperAsync(clientUuid, new CreateProtocolMapperRequestDto
{
    Name           = "department-claim",
    ProtocolMapper = "oidc-usermodel-attribute-mapper",
    Config = new Dictionary<string, string>
    {
        ["user.attribute"] = "department",
        ["claim.name"]     = "department",
        ["jsonType.label"] = "String"
    }
});

// Update an existing mapper
await clients.UpdateProtocolMapperAsync(clientUuid, mapperId, new UpdateProtocolMapperRequestDto
{
    Id             = mapperId,
    Name           = "department-claim",
    ProtocolMapper = "oidc-usermodel-attribute-mapper",
    Config = new Dictionary<string, string>
    {
        ["user.attribute"] = "team",
        ["claim.name"]     = "team",
        ["jsonType.label"] = "String"
    }
});

// Delete a mapper
await clients.DeleteProtocolMapperAsync(clientUuid, mapperId);
```
