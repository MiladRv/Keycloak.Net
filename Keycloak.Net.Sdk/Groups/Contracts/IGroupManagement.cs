using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Groups.Contracts;

public interface IGroupManagement
{
    Task<KeycloakBaseResponse> CreateGroupAsync(CreateGroupRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<GroupResponseDto>>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<GroupResponseDto>> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AddUserToGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveUserFromGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<GroupResponseDto>>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);
}
