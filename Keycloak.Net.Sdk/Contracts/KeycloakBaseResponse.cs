using System.Net;

namespace Keycloak.Net.Sdk.Contracts;

public record KeycloakBaseResponse<T>(
    T Response,
    bool IsSuccessful,
    HttpStatusCode StatusCode,
    string? ErrorMessage = null)
    : KeycloakBaseResponse(IsSuccessful, StatusCode, ErrorMessage)
    where T : class, new()
{
    public T Response { get; protected init; } = Response;
    public bool IsSuccessful { get; protected init; } = IsSuccessful;
    public HttpStatusCode StatusCode { get; protected init; } = StatusCode;
    public string? ErrorMessage { get; protected init; } = ErrorMessage;
}

public record KeycloakBaseResponse(
    bool IsSuccessful,
    HttpStatusCode StatusCode,
    string? ErrorMessage = null)
{
    public bool IsSuccessful { get; protected init; } = IsSuccessful;
    public HttpStatusCode StatusCode { get; protected init; } = StatusCode;
    public string? ErrorMessage { get; protected init; } = ErrorMessage;
}