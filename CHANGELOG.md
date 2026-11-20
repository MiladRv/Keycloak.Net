# Changelog

All notable changes to `Keycloak.Net.Sdk` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Aspire packages 1.1.0 (`aspire/v1.1.0`)

`Keycloak.Net.Sdk.Aspire` and `Keycloak.Net.Sdk.Aspire.Hosting` are released together.

#### Security
- `Keycloak.Net.Sdk.Aspire.Hosting` now depends on `Aspire.Hosting` 13.6.5 instead of 9.0.0. `Aspire.Hosting` 9.0.0 has a high-severity elevation-of-privilege advisory ([GHSA-f7vh-h5g3-7jrh](https://github.com/advisories/GHSA-f7vh-h5g3-7jrh), fixed in 13.6.5) and brought in `KubernetesClient` 15.0.1 ([GHSA-w7r3-mgwf-4mqq](https://github.com/advisories/GHSA-w7r3-mgwf-4mqq)); 13.6.5 brings in `KubernetesClient` 19.0.2. Packing the Aspire packages no longer reports NU1902/NU1903 warnings.

#### Changed
- **AppHosts must use Aspire 13.** Projects on Aspire 9 must upgrade the Aspire SDK and hosting packages before taking `Keycloak.Net.Sdk.Aspire.Hosting` 1.1.0. `Aspire.Hosting` 13 also brings in `Microsoft.Extensions.*` 10.x packages, on .NET 8 too.
- No public API changes: `AddKeycloak()`, `WithRealmImport()` and `AddKeycloakSdk()` keep their signatures and behaviour. Both packages still target .NET 8, .NET 9 and .NET 10.
- `Keycloak.Net.Sdk.Aspire` has no Aspire dependency and only gets the version bump.

#### Tests and build
- Unit tests for `Keycloak.Net.Sdk.Aspire.Hosting`: container image and tag, HTTP endpoint and port, admin environment variables, `start-dev` / `--import-realm` arguments, the realm bind mount, and the connection string expression.
- `release-aspire.yml` validates the tag the same way as `release-sdk.yml`: the tagged commit must be on `main` and the tag must be `aspire/v<Version>` from the csproj, then unit and integration tests run before publishing. It used to gate publishing on `github.event.base_ref`, which is often empty for tag pushes, so publishing could be skipped.

## [2.1.0] - 2026-11-20

### Added

**Roles**
- Client-role operations can target any client in the realm: every client-role method in `IRoleManagement` has an overload taking `clientUuid` as its first parameter. The existing overloads keep using the configured `ClientUuid`.
- Client role CRUD: `GetClientRoleAsync`, `CreateClientRoleAsync`, `UpdateClientRoleAsync`, `DeleteClientRoleAsync`, with role attributes; `ClientRoleResponseDto.Attributes`, populated by single-role lookups.
- `GetUserClientRolesAsync` (a user's client-role mappings) and `GetClientRoleUsersAsync` (users holding a client role, with `first`/`max` pagination).
- Realm role attributes (`CreateRealmRoleRequestDto.Attributes`, `RealmRoleResponseDto.Attributes`) and `UpdateRealmRoleAsync`.
- Realm role composites: `GetRealmRoleCompositesAsync`, `GetRealmRoleClientCompositesAsync`, `AddRealmRoleCompositesAsync` and `RemoveRealmRoleCompositesAsync`. A composite realm role can contain realm and client roles.
- Client roles on groups: `GetGroupClientRolesAsync`, `AssignClientRoleToGroupAsync`, `RemoveClientRoleFromGroupAsync`, each with a `clientUuid` overload.
- Effective roles: `GetUserEffectiveRealmRolesAsync`, `GetUserEffectiveClientRolesAsync`, `GetGroupEffectiveRealmRolesAsync`, `GetGroupEffectiveClientRolesAsync`.

**Groups**
- Group hierarchy: `CreateChildGroupAsync`, `GetChildGroupsAsync` (paginated, Keycloak 23+) and `GetGroupByPathAsync`.
- `GetGroupMembersAsync` with `first`/`max` pagination.
- Group attributes: `CreateGroupRequestDto.Attributes`, `UpdateGroupRequestDto.Attributes` and `GroupResponseDto.Attributes`; `GroupResponseDto` also exposes `ParentId` and `SubGroupCount`.

**Users**
- User search by attribute: `GetUsersQueryDto.Attributes` (sent as Keycloak's `q` parameter; terms with spaces or colons are quoted). Also `Exact` and `EmailVerified` filters.
- `GetUsersCountAsync` returns the number of users matching a query, as `CountResponseDto`.
- `GetAllUsersAsync` streams users across pages with filters, a starting offset, a total result limit, and cancellation support. Failed pages throw `KeycloakException` instead of silently ending enumeration.
- User email operations: `SendVerificationEmailAsync`, `SendPasswordResetEmailAsync`, and `ExecuteActionsEmailAsync`, with optional client, redirect URI, and link lifetime settings. Required action constants and support for custom action IDs.

**New services** (registered by `AddKeycloak` and named connections; not part of `IKeycloakManagement`)
- Authorization Services: `IAuthorizationManagement` to enable authorization on a client and configure its resource server; manage scopes, resources, role/group/user policies and resource/scope permissions; check a user's permission (`CheckPermissionAsync`), list granted permissions, get a requesting party token, and evaluate a user's permissions as an administrator (`EvaluateAsync`).
- Events: `IEventManagement` to read and update the realm's events configuration (merged into the existing one), query user events and admin events with filters, and clear them.
- Identity providers: `IIdentityProviderManagement` for identity provider CRUD (update merges fields and config), mappers, and linking/unlinking users to external accounts.
- User federation: `IUserFederationManagement` for LDAP and other user storage providers: CRUD (create fills in realm id and provider type and returns the stored provider), mappers, `TestLdapConnectionAsync`, full and changed-users sync, removing and unlinking imported users. Keycloak reports a successful, empty sync when LDAP is unreachable; use `TestLdapConnectionAsync` to check.

**Configuration**
- Named Keycloak connections using keyed services, with separate configuration, HTTP clients, and token caches for each realm.

**Tests and build**
- Integration tests run against Keycloak 26.7.2 instead of the Testcontainers default (21.1). The test realm enables unmanaged user attributes and the test user has a complete profile, as Keycloak 24+ requires.
- Integration tests capture Keycloak emails in a local Mailpit container, and start an OpenLDAP container on Keycloak's network for the federation tests (`KeycloakFixture.Network`).
- Tests for named connection isolation and two realms sharing the same server and client ID.
- A develop test workflow that builds all targets and runs unit and integration tests without publishing packages.

### Changed
- Role names and client UUIDs are now URL-escaped in request paths, so names containing `/`, `#`, `?` or spaces address the right role.
- Custom implementations of `IUserManagement`, `IRoleManagement` and `IGroupManagement` (for example hand-written fakes) must implement the new members. Applications that use the SDK's registered implementations need no changes.

### Other packages
- **`Keycloak.Net.Sdk.AspNetCore` 1.0.0** (new) for APIs that accept Keycloak access tokens: `AddKeycloakJwtBearer`, realm and client roles as role claims, `RequireRealmRole` / `RequireClientRole` policies, `ClaimsPrincipal` helpers (`GetRealmRoles`, `GetClientRoles`, `GetAttributeValues`, ...), and `DataScope` with `WhereInScope` for filtering queries by token attributes. Released with `aspnetcore/v*` tags.

## [2.0.0] - 2026-10-04

### Changed
- **Breaking:** renamed `IClientManagement.GetClientScopes`, `IRoleManagement.GetClientRoles`, and `IRoleManagement.AssignClientRoleToUser` to `GetClientScopesAsync`, `GetClientRolesAsync`, and `AssignClientRoleToUserAsync` respectively, for consistency with every other async method in the SDK.
- `IRealmManagement` operations used to fetch a fresh admin access token (via the admin username/password) on every single call. Added `IRealmAdminTokenProvider`, registered as a singleton, which caches that token in memory for its lifetime and shares it across all realm operations - mirroring the existing `ITokenProvider` pattern used for the SDK's regular client. `RealmManagement`'s constructor no longer takes `IOptions<KeycloakConfiguration>` directly; if you constructed it manually instead of through `AddKeycloak`, pass an `IRealmAdminTokenProvider` instead.

### Fixed
- Service-account token failures now throw `KeycloakException` with the HTTP status and error body instead of `NullReferenceException`. A successful response without an access token is also rejected.
- Client scope and role requests preserve the path in `ServerUrl`, so installations hosted under a path such as `/auth/` work consistently.
- `GetUserByUsernameAsync` escapes the username before adding it to the query string, including `&`, `#`, `+`, and spaces.
- The README signup example now uses the actual `SignupRequestDto` constructor and property names.
- SDK releases no longer depend on the tag push event's `base_ref`. The workflow verifies that the tag belongs to `main` and matches the package version before testing and publishing to NuGet.
- A successful response with an empty body (e.g. a `204 No Content`, or a `200` with nothing written to it) used to throw an unhandled `JsonException` when the SDK tried to deserialize it. It now returns a default, empty value for that response type instead.
- `RevokeTokenAsync` duplicated the shared response-handling logic by hand instead of reusing `HandleResponseAsync`, and used the raw response body as the error message on failure while every other method used the HTTP reason phrase. Both `HandleResponseAsync` overloads now prefer the response body for the error message (falling back to the reason phrase when the body is empty), and `RevokeTokenAsync` goes through the shared helper like everything else.
- Fixed mangled comment separators (`──`, `↔`) in a few files left over from the `Athentications` → `Authentications` rename in 1.9.0 - a text-encoding mistake on my part, comments only, no functional impact.

### Tests
- Added regression coverage for failed token requests, missing access tokens, server base paths, and usernames containing special characters.
- Added Keycloak integration tests to the SDK release workflow. The updated README signup example was also compiled against the SDK.
- Added test coverage for `KeycloakManagement` (the `IKeycloakManagement` facade) and `ServiceRegistrations` (`AddKeycloak` DI wiring), previously untested: missing `ServerUrl` validation, that every manager interface resolves, that `IKeycloakManagement`'s properties come from the same container as the individually-resolved managers, configuration binding, the `configure` callback override, and the three named `HttpClient`s Keycloak calls go through.

## [1.9.0] - 2026-09-07

### Changed
- **Breaking:** renamed the `Athentications` namespace/folder (a long-standing typo) to `Authentications`. `Keycloak.Net.Sdk.Athentications.*` types (`ITokenProvider`, `ITokenManagement`, `TokenProvider`, `TokenManagement`, `KeycloakAuthHandler`, `SigninResponseDto`, ...) now live under `Keycloak.Net.Sdk.Authentications.*`. Update any `using` statements referencing the old namespace.

### Added
- `GetClientAsync` and `UpdateClientAsync` on `IClientManagement` for fetching and updating a single client by id
- `UpdateGroupAsync` on `IGroupManagement` for renaming/updating a single group

### Documentation
- Fixed several docs examples that didn't match the actual SDK surface and wouldn't have compiled: `docs/user-management.md` (`SignupAsync`/`SigninAsync`/`SetUserPasswordAsync` signatures, a non-existent `SigninRequestDto`) and `docs/role-management.md` (`GetClientRoles` naming, missing `roleName` argument on the assign/remove calls). Also documented previously-undocumented methods (`GetGroupAsync`, `UpdateUserAsync`, `GetUsersAsync`, attributes, `GetRealmRoleAsync`, `GetUserRealmRolesAsync`, `GetGroupRealmRolesAsync`).

### Fixed
- `EnableServiceAccountAsync`, `EnableUserAsync`/`DisableUserAsync`, and `SetUserAttributeAsync` used to `PUT` a partial payload (e.g. just `{ "enabled": true }`) straight to Keycloak's client/user representation endpoint. Since that endpoint replaces the whole record, this could silently wipe out everything else on the client or user (redirect URIs, secret, protocol mappers, other attributes, ...). These methods now fetch the current representation first and merge the change into it before sending it back.
- `UpdateClientAsync` and `UpdateGroupAsync` follow the same fetch-then-merge pattern from the start, so introducing them doesn't reopen the same bug.

## [1.8.0] - 2026-08-21

### Added
- .NET 9 support alongside the existing .NET 8 and .NET 10 targets

## [1.7.0] - 2026-08-20

### Added
- Client protocol mapper CRUD (get, create, update, delete)
- Default and optional client scope assignment (get, add, remove) for clients
- Client role composites (get, add, remove)
- User credential management: list and delete a user's credentials

### Changed
- `EnableUserAsync`, `DisableUserAsync`, `SetUserPasswordAsync`, `DeleteUserAsync`, `UpdateUserAsync` and `SetUserAttributeAsync` on `IUserManagement` now return `KeycloakBaseResponse` instead of a plain `Task`, matching the rest of the SDK. Failures are returned as a failed response instead of throwing.

## [1.6.0] - 2026-07-07

### Added
- Get, update and delete operations on `IRealmManagement`

## [1.5.0] - [1.5.1] - 2026-06-10 to 2026-06-20

### Added
- `Keycloak.Net.Sdk.Aspire` and `Keycloak.Net.Sdk.Aspire.Hosting` packages for .NET Aspire integration
- Full client scope CRUD (get by id, create, update, delete)
- User session management (get active sessions, revoke, logout all)
- Realm role management
- Group management support
- `GetUsersAsync` with pagination and filtering
- `UpdateUserAsync`, `GetUserAttributesAsync`, `SetUserAttributeAsync`, `GetUsersByEmailAsync`

### Fixed
- Various SDK bug fixes, expanded test coverage, and documentation improvements

## [1.2.0] - 2026-05-29

### Added
- Group management support and realm role management

## [1.1.0] - [1.1.1] - 2025-04-22

### Added
- Full role management service
- Additional user management operations
- Dependency injection support

## [1.0.0] - [1.0.3] - 2025-03-24 to 2025-04-14

### Added
- Initial `KeyCloakAdminClient` implementation
- Client separation by business/service
