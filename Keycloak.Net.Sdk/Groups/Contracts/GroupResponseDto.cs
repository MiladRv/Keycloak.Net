namespace Keycloak.Net.Sdk.Groups.Contracts;

public class GroupResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<GroupResponseDto> SubGroups { get; set; } = [];
}
