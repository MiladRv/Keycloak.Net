using Keycloak.Net.Sdk.Contracts.Responses;
using Keycloak.Net.Sdk.Users.Contracts;

namespace Keycloak.Net.Sdk.Groups.Contracts;

public interface IGroupManagement
{
    Task<KeycloakBaseResponse> CreateGroupAsync(CreateGroupRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<GroupResponseDto>>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<GroupResponseDto>> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> UpdateGroupAsync(string groupId, UpdateGroupRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> AddUserToGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse> RemoveUserFromGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default);
    Task<KeycloakBaseResponse<List<GroupResponseDto>>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);

    // ── Hierarchy ─────────────────────────────────────────────────────────────
    /// <summary>Creates a group under <paramref name="parentGroupId"/>.</summary>
    Task<KeycloakBaseResponse> CreateChildGroupAsync(string parentGroupId, CreateGroupRequestDto requestDto, CancellationToken cancellationToken = default);
    /// <summary>Lists the direct children of a group, with optional pagination. Requires Keycloak 23 or later.</summary>
    Task<KeycloakBaseResponse<List<GroupResponseDto>>> GetChildGroupsAsync(string parentGroupId, int? first = null, int? max = null, CancellationToken cancellationToken = default);
    /// <summary>Finds a group by its full path, e.g. <c>/acme/tehran-branch</c>.</summary>
    Task<KeycloakBaseResponse<GroupResponseDto>> GetGroupByPathAsync(string path, CancellationToken cancellationToken = default);

    // ── Members ───────────────────────────────────────────────────────────────
    /// <summary>Lists the direct members of a group, with optional pagination.</summary>
    Task<KeycloakBaseResponse<List<UserInfoResponseDto>>> GetGroupMembersAsync(string groupId, int? first = null, int? max = null, CancellationToken cancellationToken = default);
}
