using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Events.Contracts;

/// <summary>
/// The realm's audit trail: user events (logins, failures, profile changes) and admin events
/// (changes made through the admin API). Events are stored only when enabled in the realm's
/// events configuration.
/// </summary>
public interface IEventManagement
{
    Task<KeycloakBaseResponse<EventsConfigResponseDto>> GetEventsConfigAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateEventsConfigAsync(UpdateEventsConfigRequestDto requestDto, CancellationToken cancellationToken = default);

    /// <summary>User events, newest first.</summary>
    Task<KeycloakBaseResponse<List<EventResponseDto>>> GetEventsAsync(GetEventsQueryDto? query = null, CancellationToken cancellationToken = default);
    /// <summary>Admin events, newest first.</summary>
    Task<KeycloakBaseResponse<List<AdminEventResponseDto>>> GetAdminEventsAsync(GetAdminEventsQueryDto? query = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes all stored user events of the realm.</summary>
    Task<KeycloakBaseResponse> ClearEventsAsync(CancellationToken cancellationToken = default);
    /// <summary>Deletes all stored admin events of the realm.</summary>
    Task<KeycloakBaseResponse> ClearAdminEventsAsync(CancellationToken cancellationToken = default);
}
