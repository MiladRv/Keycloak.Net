using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.UserFederation.Contracts;

/// <summary>User federation: users stored in LDAP / Active Directory, Kerberos, or a custom user storage provider.</summary>
public interface IUserFederationManagement
{
    Task<KeycloakBaseResponse<List<UserStorageProviderResponseDto>>> GetProvidersAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<UserStorageProviderResponseDto>> GetProviderAsync(string providerId, CancellationToken cancellationToken = default);
    /// <summary>Creates the provider and returns it as Keycloak stored it.</summary>
    Task<KeycloakBaseResponse<UserStorageProviderResponseDto>> CreateProviderAsync(CreateUserStorageProviderRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateProviderAsync(string providerId, UpdateUserStorageProviderRequestDto requestDto, CancellationToken cancellationToken = default);
    /// <summary>Deletes the provider. Users imported from it are removed too.</summary>
    Task<KeycloakBaseResponse> DeleteProviderAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>Mappers of a provider (for LDAP: attribute, group and role mappers).</summary>
    Task<KeycloakBaseResponse<List<UserStorageProviderResponseDto>>> GetProviderMappersAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests an LDAP provider's stored settings: the connection, and with <paramref name="authenticate"/> also the bind
    /// credentials. Fails when the server cannot be reached or the bind is rejected.
    /// </summary>
    Task<KeycloakBaseResponse> TestLdapConnectionAsync(string providerId, bool authenticate = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports or updates users from the provider: all of them, or only those changed since the last sync.
    /// Keycloak reports success with zero users even when the LDAP server cannot be reached; use
    /// <see cref="TestLdapConnectionAsync"/> to check the connection.
    /// </summary>
    Task<KeycloakBaseResponse<SynchronizationResultDto>> SyncUsersAsync(string providerId, bool changedOnly = false, CancellationToken cancellationToken = default);
    /// <summary>Removes users imported from the provider from Keycloak's database.</summary>
    Task<KeycloakBaseResponse> RemoveImportedUsersAsync(string providerId, CancellationToken cancellationToken = default);
    /// <summary>Turns users imported from the provider into regular Keycloak users.</summary>
    Task<KeycloakBaseResponse> UnlinkUsersAsync(string providerId, CancellationToken cancellationToken = default);
}
