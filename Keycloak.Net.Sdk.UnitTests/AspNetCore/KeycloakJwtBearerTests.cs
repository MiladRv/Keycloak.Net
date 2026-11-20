using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Keycloak.Net.Sdk.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests.AspNetCore;

/// <summary>
/// Runs the JWT bearer pipeline in memory with locally signed tokens shaped like Keycloak's.
/// </summary>
public class KeycloakJwtBearerTests
{
    private const string ServerUrl = "https://sso.example.test/";
    private const string Realm     = "erp";
    private const string Issuer    = "https://sso.example.test/realms/erp";
    private const string ApiClient = "erp-api";

    private static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "test-key" };

    private static string Token(
        string[]? realmRoles = null,
        Dictionary<string, string[]>? clientRoles = null,
        string audience = ApiClient,
        string issuer = Issuer,
        SecurityKey? key = null,
        IDictionary<string, object>? extra = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"]                = "user-1",
            ["preferred_username"] = "alice",
            ["aud"]                = audience
        };
        if (realmRoles is not null)
            claims["realm_access"] = new Dictionary<string, object> { ["roles"] = realmRoles };
        if (clientRoles is not null)
            claims["resource_access"] = clientRoles.ToDictionary(c => c.Key, c => (object)new Dictionary<string, object> { ["roles"] = c.Value });
        foreach (var (name, value) in extra ?? new Dictionary<string, object>())
            claims[name] = value;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer             = issuer,
            Claims             = claims,
            Expires            = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key ?? SigningKey, SecurityAlgorithms.RsaSha256)
        });
    }

    private static async Task<HttpClient> CreateClientAsync(
        Action<KeycloakJwtOptions>? configure = null,
        Action<JwtBearerOptions>? configureJwt = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddKeycloakJwtBearer(options =>
            {
                options.ServerUrl = ServerUrl;
                options.RealmName = Realm;
                options.ClientId  = ApiClient;
                configure?.Invoke(options);
            }, configureJwtBearer: jwt =>
            {
                // Offline: provide the discovery document instead of fetching it.
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(SigningKey);
                jwt.Configuration = configuration;
                configureJwt?.Invoke(jwt);
            });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", p => p.RequireRealmRole("erp-admin"));
            options.AddPolicy("Approve", p => p.RequireClientRole("finance", "invoice-approve", "finance-admin"));
        });

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            name  = user.Identity!.Name,
            roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        })).RequireAuthorization();
        app.MapGet("/accountant", () => "ok").RequireAuthorization(new AuthorizeAttribute { Roles = "accountant" });
        app.MapGet("/admin", () => "ok").RequireAuthorization("Admin");
        app.MapGet("/approve", () => "ok").RequireAuthorization("Approve");
        app.MapGet("/scope", (ClaimsPrincipal user) => user.GetDataScope("branch_id", "erp-admin").ToString()).RequireAuthorization();

        await app.StartAsync();
        return app.GetTestClient();
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<(string Name, string[] Roles)> MeAsync(HttpClient client, string token)
    {
        var response = await GetAsync(client, "/me", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (body.RootElement.GetProperty("name").GetString()!,
                body.RootElement.GetProperty("roles").EnumerateArray().Select(r => r.GetString()!).ToArray());
    }

    // ── Validation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidToken_IsAccepted_WithPreferredUsernameAsName()
    {
        var client = await CreateClientAsync();

        var (name, _) = await MeAsync(client, Token());

        Assert.Equal("alice", name);
    }

    [Fact]
    public async Task MissingToken_Returns401()
    {
        var client = await CreateClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(client, "/me", null)).StatusCode);
    }

    [Fact]
    public async Task WrongAudience_Returns401()
    {
        var client = await CreateClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(client, "/me", Token(audience: "account"))).StatusCode);
    }

    [Fact]
    public async Task ExplicitAudience_OverridesClientId()
    {
        var client = await CreateClientAsync(o => o.Audience = "erp-gateway");

        Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/me", Token(audience: "erp-gateway"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(client, "/me", Token())).StatusCode);
    }

    [Fact]
    public async Task AudienceValidationOff_AcceptsAnyAudience()
    {
        var client = await CreateClientAsync(o => o.ValidateAudience = false);

        Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/me", Token(audience: "account"))).StatusCode);
    }

    [Fact]
    public async Task OtherIssuer_Returns401()
    {
        var client = await CreateClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await GetAsync(client, "/me", Token(issuer: "https://sso.example.test/realms/other"))).StatusCode);
    }

    [Fact]
    public async Task WrongSigningKey_Returns401()
    {
        var client = await CreateClientAsync();
        var otherKey = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" };

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(client, "/me", Token(key: otherKey))).StatusCode);
    }

    // ── Role mapping ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RealmAndApiClientRoles_BecomeRoleClaims_OtherClientsDoNot()
    {
        var client = await CreateClientAsync();
        var token = Token(["accountant"], new()
        {
            [ApiClient] = ["report-view"],
            ["finance"] = ["invoice-approve"]
        });

        var (_, roles) = await MeAsync(client, token);

        Assert.Equal(["accountant", "report-view"], roles.Order().ToArray());
    }

    [Fact]
    public async Task RoleClientIds_AddMoreClients()
    {
        var client = await CreateClientAsync(o => o.RoleClientIds = ["finance"]);

        var (_, roles) = await MeAsync(client, Token(clientRoles: new() { ["finance"] = ["invoice-approve"] }));

        Assert.Equal(["invoice-approve"], roles);
    }

    [Fact]
    public async Task MappingCanBeTurnedOff()
    {
        var client = await CreateClientAsync(o => { o.MapRealmRoles = false; o.MapClientRoles = false; });

        var (_, roles) = await MeAsync(client, Token(["accountant"], new() { [ApiClient] = ["report-view"] }));

        Assert.Empty(roles);
    }

    [Fact]
    public async Task SameRoleInRealmAndClient_IsAddedOnce()
    {
        var client = await CreateClientAsync();

        var (_, roles) = await MeAsync(client, Token(["viewer"], new() { [ApiClient] = ["viewer"] }));

        Assert.Equal(["viewer"], roles);
    }

    [Fact]
    public async Task AuthorizeRoles_UsesMappedRealmRole()
    {
        var client = await CreateClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/accountant", Token(["accountant"]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync(client, "/accountant", Token(["clerk"]))).StatusCode);
    }

    [Fact]
    public async Task ReplacedEvents_StillMapRolesAndRunUserHandler()
    {
        var userHandlerRan = false;
        var client = await CreateClientAsync(configureJwt: jwt => jwt.Events = new JwtBearerEvents
        {
            OnTokenValidated = _ => { userHandlerRan = true; return Task.CompletedTask; }
        });

        var (_, roles) = await MeAsync(client, Token(["accountant"]));

        Assert.True(userHandlerRan);
        Assert.Equal(["accountant"], roles);
    }

    // ── Policies and data scope ──────────────────────────────────────────────

    [Fact]
    public async Task RequireRealmRole_ChecksRealmRolesOnly()
    {
        var client = await CreateClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/admin", Token(["erp-admin"]))).StatusCode);
        // Same name as a client role of the API client: mapped to a role claim, but not a realm role
        Assert.Equal(HttpStatusCode.Forbidden,
            (await GetAsync(client, "/admin", Token(clientRoles: new() { [ApiClient] = ["erp-admin"] }))).StatusCode);
    }

    [Fact]
    public async Task RequireClientRole_AcceptsAnyListedRoleOnThatClient()
    {
        var client = await CreateClientAsync();

        Assert.Equal(HttpStatusCode.OK,
            (await GetAsync(client, "/approve", Token(clientRoles: new() { ["finance"] = ["finance-admin"] }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await GetAsync(client, "/approve", Token(clientRoles: new() { ["inventory"] = ["invoice-approve"] }))).StatusCode);
    }

    [Fact]
    public async Task DataScope_FromTokenAttributes()
    {
        var client = await CreateClientAsync();

        var branchUser = await GetAsync(client, "/scope", Token(extra: new Dictionary<string, object> { ["branch_id"] = new[] { "12", "13" } }));
        var admin      = await GetAsync(client, "/scope", Token(["erp-admin"]));

        Assert.Equal("[12, 13]", await branchUser.Content.ReadAsStringAsync());
        Assert.Equal("Unrestricted", await admin.Content.ReadAsStringAsync());
    }

    // ── Configuration ────────────────────────────────────────────────────────

    [Fact]
    public void ConfigurationSection_IsBound_AndAuthorityBuilt()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Keycloak:ServerUrl"]            = "http://localhost:8080",
            ["Keycloak:RealmName"]            = "erp",
            ["Keycloak:ClientId"]             = "erp-api",
            ["Keycloak:ClientSecret"]         = "ignored",
            ["Keycloak:RequireHttpsMetadata"] = "false",
            ["Keycloak:RoleClientIds:0"]      = "finance"
        }).Build();
        var services = new ServiceCollection();
        services.AddAuthentication().AddKeycloakJwtBearer(configuration.GetSection("Keycloak"), "kc");

        using var provider = services.BuildServiceProvider();
        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("kc");
        var keycloak = provider.GetRequiredService<IOptionsMonitor<KeycloakJwtOptions>>().Get("kc");

        Assert.Equal("http://localhost:8080/realms/erp", jwt.Authority);
        Assert.Equal("erp-api", jwt.Audience);
        Assert.False(jwt.RequireHttpsMetadata);
        Assert.False(jwt.MapInboundClaims);
        Assert.Equal(ClaimTypes.Role, jwt.TokenValidationParameters.RoleClaimType);
        Assert.Equal("preferred_username", jwt.TokenValidationParameters.NameClaimType);
        Assert.Equal(["finance"], keycloak.RoleClientIds);
    }

    [Theory]
    [InlineData("", "erp", "erp-api", "ServerUrl")]
    [InlineData("not a url", "erp", "erp-api", "ServerUrl")]
    [InlineData("http://localhost:8080", "", "erp-api", "RealmName")]
    [InlineData("http://localhost:8080", "erp", "", "Audience")]
    public void InvalidOptions_ThrowWhenResolved(string serverUrl, string realm, string clientId, string expectedInMessage)
    {
        var services = new ServiceCollection();
        services.AddAuthentication().AddKeycloakJwtBearer(o =>
        {
            o.ServerUrl = serverUrl;
            o.RealmName = realm;
            o.ClientId  = clientId;
        });

        using var provider = services.BuildServiceProvider();
        var error = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme));

        Assert.Contains(expectedInMessage, error.Message);
    }

    [Fact]
    public void Policies_RejectEmptyRoleLists()
    {
        var builder = new AuthorizationPolicyBuilder();

        Assert.Throws<ArgumentException>(() => builder.RequireRealmRole());
        Assert.Throws<ArgumentException>(() => builder.RequireClientRole("finance", ""));
    }
}
