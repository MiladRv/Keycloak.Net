using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.IdentityProviders.Contracts;

/// <summary>Identity brokering: external logins and the links between users and their external accounts.</summary>
public interface IIdentityProviderManagement
{
    Task<KeycloakBaseResponse<List<IdentityProviderResponseDto>>> GetIdentityProvidersAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<IdentityProviderResponseDto>> GetIdentityProviderAsync(string alias, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateIdentityProviderAsync(CreateIdentityProviderRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateIdentityProviderAsync(string alias, UpdateIdentityProviderRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteIdentityProviderAsync(string alias, CancellationToken cancellationToken = default);

    // ── Mappers ───────────────────────────────────────────────────────────────
    Task<KeycloakBaseResponse<List<IdentityProviderMapperResponseDto>>> GetMappersAsync(string alias, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> CreateMapperAsync(string alias, CreateIdentityProviderMapperRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteMapperAsync(string alias, string mapperId, CancellationToken cancellationToken = default);

    // ── A user's linked external accounts ─────────────────────────────────────
    Task<KeycloakBaseResponse<List<FederatedIdentityDto>>> GetUserFederatedIdentitiesAsync(string userId, CancellationToken cancellationToken = default);
    /// <summary>Links a user to their account at an identity provider, for example after a migration.</summary>
    Task<KeycloakBaseResponse> LinkUserAsync(string userId, string alias, string externalUserId, string externalUserName, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UnlinkUserAsync(string userId, string alias, CancellationToken cancellationToken = default);
}
