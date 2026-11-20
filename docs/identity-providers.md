# Identity Providers

Identity brokering lets users sign in to the realm with another login: a corporate OIDC or SAML
server, Active Directory Federation Services, Google, Microsoft, and so on.

Inject `IIdentityProviderManagement`:

```csharp
public class SsoSetup(IIdentityProviderManagement idp)
```

## Create, read, update, delete

```csharp
await idp.CreateIdentityProviderAsync(new CreateIdentityProviderRequestDto
{
    Alias       = "corporate",
    ProviderId  = "oidc",                       // or keycloak-oidc, saml, google, microsoft, ...
    DisplayName = "Corporate login",
    TrustEmail  = true,
    Config      = new()
    {
        ["authorizationUrl"] = "https://login.corp.example/authorize",
        ["tokenUrl"]         = "https://login.corp.example/token",
        ["clientId"]         = "keycloak-broker",
        ["clientSecret"]     = brokerSecret,
        ["clientAuthMethod"] = "client_secret_post",
        ["defaultScope"]     = "openid email profile"
    }
});

var all       = await idp.GetIdentityProvidersAsync();
var corporate = await idp.GetIdentityProviderAsync("corporate");

await idp.UpdateIdentityProviderAsync("corporate", new UpdateIdentityProviderRequestDto
{
    DisplayName = "Corporate SSO",
    HideOnLogin = true,
    Config      = new() { ["defaultScope"] = "openid email" }
});

await idp.DeleteIdentityProviderAsync("corporate");
```

`UpdateIdentityProviderAsync` reads the provider and changes only the fields you set. `Config`
entries are merged into the existing config. Secrets come back masked (`**********`) and are sent
back masked; Keycloak treats the masked value as "keep the stored secret". To change a secret, set
its key in `Config`. After changing secrets, test a login through the provider.

Other options: `StoreToken` (keep the provider's tokens), `LinkOnly` (only link existing accounts),
`FirstBrokerLoginFlowAlias`.

## Mappers

Mappers copy data from the external login onto the Keycloak user, for example an attribute or a role:

```csharp
await idp.CreateMapperAsync("corporate", new CreateIdentityProviderMapperRequestDto
{
    Name                   = "company",
    IdentityProviderMapper = "hardcoded-attribute-idp-mapper",
    Config                 = new() { ["syncMode"] = "INHERIT", ["attribute"] = "company_id", ["attribute.value"] = "1" }
});

var mappers = await idp.GetMappersAsync("corporate");
await idp.DeleteMapperAsync("corporate", mappers.Response[0].Id);
```

## Linking users to external accounts

```csharp
// For example after migrating users: link an existing Keycloak user to their corporate account
await idp.LinkUserAsync(userId, "corporate", externalUserId: "00u1abc", externalUserName: "alice@corp.example");

var links = await idp.GetUserFederatedIdentitiesAsync(userId);
await idp.UnlinkUserAsync(userId, "corporate");
```

## Notes

- Registered by `AddKeycloak` and named connections; not part of `IKeycloakManagement`.
- Requires `manage-identity-providers` (included in `realm-admin`).
