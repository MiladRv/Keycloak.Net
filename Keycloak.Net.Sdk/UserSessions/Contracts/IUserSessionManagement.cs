using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.UserSessions.Contracts;

public interface IUserSessionManagement
{
    /// <summary>Gets all active sessions for the specified user.</summary>
    Task<KeycloakBaseResponse<List<UserSessionResponseDto>>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Revokes a specific session by its session ID.</summary>
    Task<KeycloakBaseResponse> RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Logs the user out of all active sessions.</summary>
    Task<KeycloakBaseResponse> LogoutUserAsync(string userId, CancellationToken cancellationToken = default);
}
