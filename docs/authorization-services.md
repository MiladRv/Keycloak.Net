# Authorization Services

Keycloak Authorization Services decide **what** a user may do, not only who they are: resources
(for example `invoice`), scopes (actions such as `view` and `approve`), policies (who: a role, a
group, specific users) and permissions (which policies grant which scopes on which resources).
Rules live in Keycloak and can change without redeploying the application.

Inject `IAuthorizationManagement`:

```csharp
public class MyService(IAuthorizationManagement authz)
```

Management methods take the resource server's internal client id (`clientUuid`, i.e.
`ClientResponseDto.Id`). Evaluation methods take the user's access token and the resource
server's `clientId`.

> Roles and groups are often enough. Use Authorization Services when rules are per action and
> change often, or when non-developers should manage them in Keycloak.

## Set up a resource server

```csharp
// The client must be confidential; this also enables service accounts
await authz.EnableAuthorizationAsync(erpApiUuid);

// ENFORCING (default) denies resources that have no permission; PERMISSIVE allows them
await authz.UpdateResourceServerAsync(erpApiUuid, new UpdateResourceServerRequestDto
{
    PolicyEnforcementMode = PolicyEnforcementModes.Enforcing,
    DecisionStrategy      = DecisionStrategies.Unanimous
});

var settings = await authz.GetResourceServerAsync(erpApiUuid);
```

Keycloak may add a *Default Resource*, *Default Policy* and *Default Permission* when authorization
is enabled, which grant everyone access to that default resource. List them with
`GetResourcesAsync` / `GetPoliciesAsync` and delete them if you do not want them.

## Scopes and resources

```csharp
var view    = await authz.CreateScopeAsync(erpApiUuid, new CreateAuthorizationScopeRequestDto { Name = "view" });
var approve = await authz.CreateScopeAsync(erpApiUuid, new CreateAuthorizationScopeRequestDto { Name = "approve" });

var invoice = await authz.CreateResourceAsync(erpApiUuid, new CreateResourceRequestDto
{
    Name       = "invoice",
    Type       = "urn:erp:resources:invoice",
    Uris       = ["/invoices/*"],
    Scopes     = ["view", "approve"],               // by name
    Attributes = new() { ["module"] = ["finance"] }
});

var found = await authz.GetResourcesAsync(erpApiUuid, new GetResourcesQueryDto { Name = "invoice", ExactName = true });
await authz.UpdateResourceAsync(erpApiUuid, invoice.Response.Id, new UpdateResourceRequestDto { DisplayName = "Invoices" });
await authz.DeleteResourceAsync(erpApiUuid, invoice.Response.Id);

var scopes = await authz.GetScopesAsync(erpApiUuid);
await authz.DeleteScopeAsync(erpApiUuid, approve.Response.Id);
```

`UpdateResourceAsync` reads the resource and changes only the fields you set. `Scopes` and
`Attributes` replace the existing ones when set.

## Policies

```csharp
// Users holding a role (realm or client role id). Required = the user must have this role.
var accountants = await authz.CreateRolePolicyAsync(erpApiUuid, new CreateRolePolicyRequestDto
{
    Name  = "accountants",
    Roles = [new PolicyRole(accountantRoleId, Required: true)]
});

// Members of a group, and with ExtendChildren of its child groups
var managers = await authz.CreateGroupPolicyAsync(erpApiUuid, new CreateGroupPolicyRequestDto
{
    Name   = "managers",
    Groups = [new PolicyGroup(managersGroupId, ExtendChildren: true)]
});

// Specific users; Logic = Negative turns the result around
var notAuditor = await authz.CreateUserPolicyAsync(erpApiUuid, new CreateUserPolicyRequestDto
{
    Name  = "not-the-auditor",
    Users = [auditorUserId],
    Logic = PolicyLogic.Negative
});

var rolePolicies = await authz.GetPoliciesAsync(erpApiUuid, new GetPoliciesQueryDto { Type = "role" });
var policy       = await authz.GetPolicyAsync(erpApiUuid, accountants.Response.Id);
await authz.DeletePolicyAsync(erpApiUuid, notAuditor.Response.Id);
```

Type-specific settings of listed policies are in `PolicyResponseDto.Config`, as Keycloak returns them.

## Permissions

```csharp
// Scope permission: accountants may view invoices
await authz.CreateScopePermissionAsync(erpApiUuid, new CreateScopePermissionRequestDto
{
    Name      = "view-invoices",
    Resources = [invoiceId],
    Scopes    = [viewScopeId],
    Policies  = [accountants.Response.Id]
});

// Scope permission: managers may approve
await authz.CreateScopePermissionAsync(erpApiUuid, new CreateScopePermissionRequestDto
{
    Name      = "approve-invoices",
    Resources = [invoiceId],
    Scopes    = [approveScopeId],
    Policies  = [managers.Response.Id]
});

// Resource permission for every resource of a type
await authz.CreateResourcePermissionAsync(erpApiUuid, new CreateResourcePermissionRequestDto
{
    Name         = "all-invoices-for-admins",
    ResourceType = "urn:erp:resources:invoice",
    Policies     = [adminsPolicyId]
});

var invoicePermissions = await authz.GetPermissionsAsync(erpApiUuid, new GetPermissionsQueryDto { Resource = invoiceId });
await authz.DeletePermissionAsync(erpApiUuid, permissionId);
```

`DecisionStrategy` on a permission decides how its policies combine: `Unanimous` (all must grant,
the default), `Affirmative` (any one) or `Consensus` (majority).

## Checking permissions

With the user's access token (for example from the API's incoming request) and the resource
server's `clientId`:

```csharp
var decision = await authz.CheckPermissionAsync(userAccessToken, "erp-api",
    new PermissionRequest("invoice", "approve"));

if (decision.IsSuccessful && decision.Response.Granted) { ... }
```

A denial is a **successful** response with `Granted = false`. `IsSuccessful = false` means the check
itself failed (invalid token, unknown audience, Keycloak unreachable). Treat both as "no".

```csharp
// Everything the user may do, or only on some resources
var granted = await authz.GetGrantedPermissionsAsync(userAccessToken, "erp-api",
    [new PermissionRequest("invoice")]);
// granted.Response: [{ ResourceName = "invoice", Scopes = ["view"] }]

// A requesting party token (RPT) whose "authorization" claim lists the granted permissions
var rpt = await authz.GetRequestingPartyTokenAsync(userAccessToken, "erp-api");
```

`PermissionRequest` takes a resource id or name, and optionally scopes. `new PermissionRequest(null, "approve")`
asks for a scope on any resource.

### Evaluate without the user's token

For admin screens ("what can this user do?"), evaluate as an administrator:

```csharp
var result = await authz.EvaluateAsync(erpApiUuid, userId, [invoiceId]);

foreach (var item in result.Response.Results)
    Console.WriteLine($"{item.Resource?.Name}: {item.Status} {string.Join(",", item.AllowedScopes?.Select(s => s.Name) ?? [])}");
```

## Notes

- `IAuthorizationManagement` is registered by `AddKeycloak` and by named connections
  (`GetRequiredKeyedService<IAuthorizationManagement>("name")`). It is not part of `IKeycloakManagement`.
- The SDK's service account needs `manage-authorization` (included in `realm-admin`) for the management methods.
- Evaluation calls go to the token endpoint with the user's token, not the SDK's service-account token.
