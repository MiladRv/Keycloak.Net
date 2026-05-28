using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.Keycloak;
using Xunit;
using KeycloakConfiguration = Keycloak.Net.Sdk.Configurations.KeycloakConfiguration;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// Starts one Keycloak container for all integration tests in the collection.
/// Setup sequence:
///   1. Get master-realm admin token
///   2. Create realm "sdk-integration"
///   3. Create confidential client with service accounts
///   4. Assign realm-admin role to service account → SDK can manage users/roles
///   5. Create a test role inside the client
///   6. Create a test user
/// </summary>
public class KeycloakFixture : IAsyncLifetime
{
    private KeycloakContainer _container = null!;

    // ── Public state consumed by tests ────────────────────────────────────────
    public IServiceProvider Services     { get; private set; } = null!;
    public string           TestUserId   { get; private set; } = null!;
    public string           TestRoleId   { get; private set; } = null!;
    public const string     TestRoleName = "sdk-test-role";
    public const string     TestUsername = "sdk-test-user";
    public const string     TestPassword = "Test@1234";
    public const string     Realm        = "sdk-integration";
    public const string     SdkClientId  = "sdk-client";

    private const string AdminUsername = "admin";
    private const string AdminPassword = "admin";

    public async Task InitializeAsync()
    {
        _container = new KeycloakBuilder().Build();
        await _container.StartAsync();

        var baseAddress = _container.GetBaseAddress().TrimEnd('/');
        using var http  = new HttpClient { BaseAddress = new Uri(baseAddress + "/") };

        // 1. Admin token in master realm
        var adminToken = await GetAdminTokenAsync(http);

        // 2. Create test realm
        await CreateRealmAsync(http, adminToken, Realm);

        // 3. Create SDK client (confidential + service accounts)
        var clientUuid  = await CreateClientAsync(http, adminToken, Realm, SdkClientId);
        var clientSecret = await GetClientSecretAsync(http, adminToken, Realm, clientUuid);

        // 4. Give service account realm-admin role
        await AssignRealmAdminToServiceAccountAsync(http, adminToken, Realm, clientUuid, SdkClientId);

        // 5. Create test role inside the client
        TestRoleId = await CreateClientRoleAsync(http, adminToken, Realm, clientUuid, TestRoleName);

        // 6. Create test user
        TestUserId = await CreateTestUserAsync(http, adminToken, Realm, TestUsername, TestPassword);

        // 7. Wire up the SDK
        var config = new KeycloakConfiguration
        {
            ServerUrl     = baseAddress + "/",
            RealmName     = Realm,
            ClientId      = SdkClientId,
            ClientSecret  = clientSecret,
            ClientUuid    = clientUuid,
            AdminUsername = AdminUsername,
            AdminPassword = AdminPassword,
            NumberOfRetries = 1,
            DelayBetweenRetryRequestsInSeconds = 1
        };

        var services = new ServiceCollection();
        services.AddKeycloak(BuildConfigurationFrom(config));
        Services = services.BuildServiceProvider();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    // ── Admin API helpers ─────────────────────────────────────────────────────

    private static async Task<string> GetAdminTokenAsync(HttpClient http)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"]  = "admin-cli",
            ["grant_type"] = "password",
            ["username"]   = AdminUsername,
            ["password"]   = AdminPassword
        });
        var resp = await http.PostAsync("realms/master/protocol/openid-connect/token", form);
        resp.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    private static async Task CreateRealmAsync(HttpClient http, string token, string realm)
    {
        var req = AuthorizedPost("admin/realms", token,
            new { realm, enabled = true });
        (await http.SendAsync(req)).EnsureSuccessStatusCode();
    }

    private static async Task<string> CreateClientAsync(HttpClient http, string token, string realm, string clientId)
    {
        var req = AuthorizedPost($"admin/realms/{realm}/clients", token,
            new { clientId, enabled = true, publicClient = false, serviceAccountsEnabled = true, protocol = "openid-connect" });
        var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        // Location header contains the client URL: .../clients/{uuid}
        var uuid = resp.Headers.Location!.ToString().Split('/').Last();
        return uuid;
    }

    private static async Task<string> GetClientSecretAsync(HttpClient http, string token, string realm, string clientUuid)
    {
        var req = AuthorizedGet($"admin/realms/{realm}/clients/{clientUuid}/client-secret", token);
        var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("value").GetString()!;
    }

    private static async Task AssignRealmAdminToServiceAccountAsync(
        HttpClient http, string token, string realm, string clientUuid, string clientId)
    {
        // Get service account user
        var saReq  = AuthorizedGet($"admin/realms/{realm}/clients/{clientUuid}/service-account-user", token);
        var saResp = await http.SendAsync(saReq);
        saResp.EnsureSuccessStatusCode();
        var saJson = JsonDocument.Parse(await saResp.Content.ReadAsStringAsync());
        var saUserId = saJson.RootElement.GetProperty("id").GetString()!;

        // Get realm-management client uuid
        var rmReq  = AuthorizedGet($"admin/realms/{realm}/clients?clientId=realm-management", token);
        var rmResp = await http.SendAsync(rmReq);
        rmResp.EnsureSuccessStatusCode();
        var rmJson     = JsonDocument.Parse(await rmResp.Content.ReadAsStringAsync());
        var rmClientUuid = rmJson.RootElement[0].GetProperty("id").GetString()!;

        // Get realm-admin role
        var roleReq  = AuthorizedGet($"admin/realms/{realm}/clients/{rmClientUuid}/roles/realm-admin", token);
        var roleResp = await http.SendAsync(roleReq);
        roleResp.EnsureSuccessStatusCode();
        var roleJson = JsonDocument.Parse(await roleResp.Content.ReadAsStringAsync());
        var roleId   = roleJson.RootElement.GetProperty("id").GetString()!;
        var roleName = roleJson.RootElement.GetProperty("name").GetString()!;

        // Assign role to service account
        var assignReq = AuthorizedPost(
            $"admin/realms/{realm}/users/{saUserId}/role-mappings/clients/{rmClientUuid}", token,
            new[] { new { id = roleId, name = roleName } });
        (await http.SendAsync(assignReq)).EnsureSuccessStatusCode();
    }

    private static async Task<string> CreateClientRoleAsync(
        HttpClient http, string token, string realm, string clientUuid, string roleName)
    {
        var req  = AuthorizedPost($"admin/realms/{realm}/clients/{clientUuid}/roles", token, new { name = roleName });
        var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        // Fetch the created role to get its ID
        var getRoleReq  = AuthorizedGet($"admin/realms/{realm}/clients/{clientUuid}/roles/{roleName}", token);
        var getRoleResp = await http.SendAsync(getRoleReq);
        getRoleResp.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await getRoleResp.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateTestUserAsync(
        HttpClient http, string token, string realm, string username, string password)
    {
        var req = AuthorizedPost($"admin/realms/{realm}/users", token,
            new
            {
                username,
                enabled = true,
                credentials = new[] { new { type = "password", value = password, temporary = false } }
            });
        var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return resp.Headers.Location!.ToString().Split('/').Last();
    }

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    private static HttpRequestMessage AuthorizedPost(string url, string token, object body)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return msg;
    }

    private static HttpRequestMessage AuthorizedGet(string url, string token)
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return msg;
    }

    // ── IConfiguration shim ───────────────────────────────────────────────────

    private static Microsoft.Extensions.Configuration.IConfiguration BuildConfigurationFrom(KeycloakConfiguration cfg)
    {
        var dict = new Dictionary<string, string?>
        {
            ["keycloak:ServerUrl"]                        = cfg.ServerUrl,
            ["keycloak:RealmName"]                        = cfg.RealmName,
            ["keycloak:ClientId"]                         = cfg.ClientId,
            ["keycloak:ClientSecret"]                     = cfg.ClientSecret,
            ["keycloak:ClientUuid"]                       = cfg.ClientUuid,
            ["keycloak:AdminUsername"]                    = cfg.AdminUsername,
            ["keycloak:AdminPassword"]                    = cfg.AdminPassword,
            ["keycloak:NumberOfRetries"]                  = cfg.NumberOfRetries.ToString(),
            ["keycloak:DelayBetweenRetryRequestsInSeconds"] = cfg.DelayBetweenRetryRequestsInSeconds.ToString()
        };
        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }
}

[CollectionDefinition(nameof(KeycloakCollection))]
public class KeycloakCollection : ICollectionFixture<KeycloakFixture>;
