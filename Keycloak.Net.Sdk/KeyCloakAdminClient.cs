using System.Net.Http.Headers;
using Keycloak.Net.Sdk.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Keycloak.Net.Sdk;

public sealed class KeycloakAdminClient : IKeycloakAdminClient
{
    public KeycloakAdminClient(IHttpClientFactory httpClientFactory,
        IOptions<KeycloakConfiguration> keycloakOptions,
        ILogger logger)
    {
        var httpClient = httpClientFactory.CreateClient("keycloak");
        // Define the circuit breaker policy
        
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: keycloakOptions.Value.NumberOfRetries, // Number of retries
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(keycloakOptions.Value.DelayBetweenRetryRequestsInSeconds), // Delay between each retry
                onRetry: (outcome, timespan, retryAttempt, context) => { logger.LogWarning($"Retry {retryAttempt} after {timespan.TotalSeconds} seconds due to: {outcome.Exception?.Message ?? outcome.Result.ReasonPhrase}"); });


        var tokenManagement = new TokenManagement(retryPolicy, httpClient, keycloakOptions);

        var response = tokenManagement.GetClientTokenAsync().GetAwaiter().GetResult();

        if (!response.IsSuccessful)
        {
            throw new Exception(response.ErrorMessage);
        }

        var adminToken = response.Response.AccessToken;

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        
        TokenManagement = tokenManagement;
        UserManagement = new UserManagement(retryPolicy, httpClient, keycloakOptions);
        RoleManagement = new RoleManagement(retryPolicy, httpClient, keycloakOptions);
        RealmManagement = new RealmManagement(retryPolicy, keycloakOptions);
        ClientManagement = new ClientManagement(retryPolicy, httpClient, keycloakOptions);
    }

    public IUserManagement UserManagement { get; init; }
    public IRoleManagement RoleManagement { get; init; }
    public ITokenManagement TokenManagement { get; init; }
    public IRealmManagement RealmManagement { get; init; }
    public IClientManagement ClientManagement { get; init; }
}