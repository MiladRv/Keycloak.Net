using System.Net;

namespace Keycloak.Net.Sdk.Contracts.Responses;

public sealed record KeycloakFailureResponse(HttpStatusCode StatusCode, string? ErrorMessage = null)
    : KeycloakBaseResponse(false, StatusCode, ErrorMessage);
    
public sealed record KeycloakFailureResponse<T>(HttpStatusCode StatusCode, string? ErrorMessage = null)
    : KeycloakBaseResponse<T>(null,false, StatusCode, ErrorMessage) where T : class, new();