# Changelog

All notable changes to `Keycloak.Net.Sdk` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Changed
- **Breaking:** renamed the `Athentications` namespace/folder (a long-standing typo) to `Authentications`. `Keycloak.Net.Sdk.Athentications.*` types (`ITokenProvider`, `ITokenManagement`, `TokenProvider`, `TokenManagement`, `KeycloakAuthHandler`, `SigninResponseDto`, ...) now live under `Keycloak.Net.Sdk.Authentications.*`. Update any `using` statements referencing the old namespace.

### Added
- `GetClientAsync` and `UpdateClientAsync` on `IClientManagement` for fetching and updating a single client by id
- `UpdateGroupAsync` on `IGroupManagement` for renaming/updating a single group

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
