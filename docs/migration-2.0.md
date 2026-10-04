# Upgrading to Keycloak.Net.Sdk 2.0

Version 2.0 includes the fixes and API changes made since the last NuGet release, 1.8.0.

## Authentication namespace

The misspelled `Keycloak.Net.Sdk.Athentications` namespace is now `Keycloak.Net.Sdk.Authentications`. Update your using statements, including references to the `Contracts` namespace.

## Async method names

These methods now have an `Async` suffix:

| Previous name | New name |
| --- | --- |
| `IClientManagement.GetClientScopes` | `IClientManagement.GetClientScopesAsync` |
| `IRoleManagement.GetClientRoles` | `IRoleManagement.GetClientRolesAsync` |
| `IRoleManagement.AssignClientRoleToUser` | `IRoleManagement.AssignClientRoleToUserAsync` |

The arguments and return types of these methods have not changed.

## RealmManagement constructor

`RealmManagement` now takes `IRealmAdminTokenProvider` instead of `IOptions<KeycloakConfiguration>`. The provider caches the master realm admin token and shares it across realm operations.

Applications that register the SDK with `AddKeycloak` need no DI changes. If you construct `RealmManagement` directly, pass a `RealmAdminTokenProvider` using the same HTTP client factory and configuration options.

## Token errors

If fetching a service-account token fails, `TokenProvider.GetTokenAsync` throws `KeycloakException`. The exception message includes the HTTP status and the error returned by Keycloak. A successful response without an access token also throws `KeycloakException`.

## Server URL

Client scope and role requests now respect the path in `ServerUrl`. Include a trailing slash when using a base path, for example `https://example.com/auth/`.

The Aspire packages keep their own version numbers. This release updates the core SDK; it does not publish new versions of the Aspire packages.
