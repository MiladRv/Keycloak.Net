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
