using System.Text.Json;

namespace Keycloak.Net.Sdk.Contracts;

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
}