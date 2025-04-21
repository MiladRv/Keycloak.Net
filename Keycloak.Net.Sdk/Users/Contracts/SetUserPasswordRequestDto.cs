namespace Keycloak.Net.Sdk.Users.Contracts;

public sealed record SetUserPasswordRequestDto()
{
    public string Type => "password";
    public string Value { get; init; }
    public bool Temporary { get; init; }
}