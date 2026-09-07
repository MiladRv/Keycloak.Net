using System.Text.Json;
using Keycloak.Net.Sdk.Contracts.Responses;

namespace Keycloak.Net.Sdk.Extensions;

internal static class ExceptionHandler
{
    public static async Task<KeycloakBaseResponse<T>> HandleResponseAsync<T>(this HttpResponseMessage response)
        where T : class, new()
    {
        if (!response.IsSuccessStatusCode)
            return new KeycloakFailureResponse<T>(response.StatusCode, await response.GetErrorMessageAsync());

        var responseContent = await response.Content.ReadAsStringAsync();

        // A successful response can still have an empty body (e.g. 204 No Content, or a
        // 200 with nothing written to it) - deserializing that would otherwise throw a
        // JsonException instead of the failure being reported through the response type.
        if (string.IsNullOrWhiteSpace(responseContent))
            return new KeycloakBaseResponse<T>(new T(), true, response.StatusCode);

        var deserializedResponse = JsonSerializer.Deserialize<T>(responseContent)!;

        return new KeycloakBaseResponse<T>(deserializedResponse, true, response.StatusCode);
    }

    public static async Task<KeycloakBaseResponse> HandleResponseAsync(this HttpResponseMessage response)
    {
        return !response.IsSuccessStatusCode
            ? new KeycloakFailureResponse(response.StatusCode, await response.GetErrorMessageAsync())
            : new KeycloakBaseResponse(true, response.StatusCode);
    }

    // Keycloak error bodies (e.g. { "error": "invalid_token" }) are usually more useful
    // than the generic HTTP reason phrase, so prefer the body and fall back to the
    // reason phrase only when there's nothing in it.
    private static async Task<string?> GetErrorMessageAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(content) ? response.ReasonPhrase : content;
    }
}