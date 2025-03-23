using System.Net;

namespace Keycloak.Net.Sdk.Contracts;

public sealed record KeycloakSuccessResponse<T>(T Response)
    : KeycloakBaseResponse<T>(Response, true, HttpStatusCode.OK) where T : class, new();