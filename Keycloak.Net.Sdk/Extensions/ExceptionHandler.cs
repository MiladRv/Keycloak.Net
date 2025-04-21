using System.Text.Json;
using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Extensions;

internal static class ExceptionHandler
{
    public static async Task<KeycloakBaseResponse<T>> HandleResponseAsync<T>(this HttpResponseMessage response)
        where T : class, new()
    {
        if (!response.IsSuccessStatusCode)
            return new KeycloakFailureResponse<T>(response.StatusCode, response.ReasonPhrase);

        var responseContent = await response.Content.ReadAsStringAsync();

        var deserializedResponse = JsonSerializer.Deserialize<T>(responseContent)!;

        return new KeycloakSuccessResponse<T>(deserializedResponse);
    }

    public static async Task<KeycloakBaseResponse> HandleResponseAsync(this HttpResponseMessage response)
    {
        return !response.IsSuccessStatusCode
            ? new KeycloakFailureResponse(response.StatusCode, response.ReasonPhrase)
            : new KeycloakSuccessResponse();
    }
}