using System.Net;

namespace Keycloak.Net.Sdk.Contracts.Responses;

public sealed record KeycloakSuccessResponse<T>(T Response)
    : KeycloakBaseResponse<T>(Response, true, HttpStatusCode.OK) where T : class, new();
    
public sealed record KeycloakSuccessResponse() : KeycloakBaseResponse(true, HttpStatusCode.OK);