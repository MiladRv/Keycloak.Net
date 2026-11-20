using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Keycloak.Net.Sdk.AspNetCore;
using Keycloak.Net.Sdk.Clients.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Roles.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// An API protected by Keycloak.Net.Sdk.AspNetCore, called with access tokens issued by the
/// Keycloak container: discovery, signature, audience, role mapping, policies and data scope.
/// </summary>
[Collection(nameof(KeycloakCollection))]
public class AspNetCoreIntegrationTests(KeycloakFixture fixture)
{
    private const string BranchClaim = "branch_id";

    private IServiceProvider Scoped => fixture.Services.CreateScope().ServiceProvider;
    private IUserManagement User => Scoped.GetRequiredService<IUserManagement>();
    private IRoleManagement Role => Scoped.GetRequiredService<IRoleManagement>();
    private IClientManagement Client => Scoped.GetRequiredService<IClientManagement>();
    private KeycloakConfiguration Config => Scoped.GetRequiredService<IOptions<KeycloakConfiguration>>().Value;

    private async Task<HttpClient> CreateApiAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddKeycloakJwtBearer(options =>
            {
                options.ServerUrl            = Config.ServerUrl;
                options.RealmName            = KeycloakFixture.Realm;
                options.ClientId             = KeycloakFixture.SdkClientId;
                options.RequireHttpsMetadata = false; // the container serves HTTP
            });
        builder.Services.AddAuthorization(options =>
            options.AddPolicy("RealmRole", p => p.RequireRealmRole(KeycloakFixture.TestRealmRoleName)));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            id    = user.GetKeycloakUserId(),
            name  = user.Identity!.Name,
            roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            scope = user.GetDataScope(BranchClaim, "erp-admin").ToString()
        })).RequireAuthorization();
        app.MapGet("/client-role", () => "ok")
            .RequireAuthorization(new AuthorizeAttribute { Roles = KeycloakFixture.TestRoleName });
        app.MapGet("/realm-role", () => "ok").RequireAuthorization("RealmRole");

        await app.StartAsync();
        return app.GetTestClient();
    }

    private static async Task<HttpResponseMessage> CallAsync(HttpClient api, string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await api.SendAsync(request);
    }

    private async Task<string> SignInAsync()
    {
        var signin = await User.SigninAsync(KeycloakFixture.TestUsername, KeycloakFixture.TestPassword);
        Assert.True(signin.IsSuccessful, signin.ErrorMessage);
        return signin.Response.AccessToken!;
    }

    private async Task<string> AddMapperAsync(string name, string mapper, Dictionary<string, string> config)
    {
        Assert.True((await Client.CreateProtocolMapperAsync(fixture.SdkClientUuid, new CreateProtocolMapperRequestDto
        {
            Name = name, ProtocolMapper = mapper, Config = config
        })).IsSuccessful);
        return (await Client.GetProtocolMappersAsync(fixture.SdkClientUuid)).Response.Single(m => m.Name == name).Id;
    }

    [Fact]
    public async Task KeycloakIssuedToken_IsValidated_AndRolesAndScopeAreMapped()
    {
        var api = await CreateApiAsync();
        var mapperIds = new List<string>();
        try
        {
            // Without an audience mapper Keycloak does not put the API's client in aud
            var plainToken = await SignInAsync();
            var plainAudience = new JsonWebToken(plainToken).Audiences.ToList();
            if (!plainAudience.Contains(KeycloakFixture.SdkClientId))
                Assert.Equal(HttpStatusCode.Unauthorized, (await CallAsync(api, "/me", plainToken)).StatusCode);

            mapperIds.Add(await AddMapperAsync("api-audience", "oidc-audience-mapper", new()
            {
                ["included.client.audience"] = KeycloakFixture.SdkClientId,
                ["access.token.claim"]       = "true",
                ["id.token.claim"]           = "false"
            }));
            mapperIds.Add(await AddMapperAsync("branch", "oidc-usermodel-attribute-mapper", new()
            {
                ["user.attribute"]       = BranchClaim,
                ["claim.name"]           = BranchClaim,
                ["jsonType.label"]       = "String",
                ["multivalued"]          = "true",
                ["access.token.claim"]   = "true",
                ["id.token.claim"]       = "false",
                ["userinfo.token.claim"] = "false"
            }));
            Assert.True((await User.SetUserAttributeAsync(fixture.TestUserId, BranchClaim, "12")).IsSuccessful);
            Assert.True((await Role.AssignRealmRoleToUserAsync(fixture.TestUserId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName)).IsSuccessful);
            Assert.True((await Role.AssignClientRoleToUserAsync(fixture.TestUserId, fixture.TestRoleId, KeycloakFixture.TestRoleName)).IsSuccessful);

            var token = await SignInAsync();
            var me = await CallAsync(api, "/me", token);
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);

            using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
            Assert.Equal(fixture.TestUserId, body.RootElement.GetProperty("id").GetString());
            Assert.Equal(KeycloakFixture.TestUsername, body.RootElement.GetProperty("name").GetString());
            var roles = body.RootElement.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
            Assert.Contains(KeycloakFixture.TestRealmRoleName, roles);
            Assert.Contains(KeycloakFixture.TestRoleName, roles);
            Assert.Equal("[12]", body.RootElement.GetProperty("scope").GetString());

            Assert.Equal(HttpStatusCode.OK, (await CallAsync(api, "/client-role", token)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await CallAsync(api, "/realm-role", token)).StatusCode);

            // After the roles are removed, a new token no longer passes the role checks
            await Role.RemoveRealmRoleFromUserAsync(fixture.TestUserId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName);
            await Role.RemoveClientRoleFromUserAsync(fixture.TestUserId, fixture.TestRoleId, KeycloakFixture.TestRoleName);
            var reduced = await SignInAsync();
            Assert.Equal(HttpStatusCode.OK, (await CallAsync(api, "/me", reduced)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await CallAsync(api, "/client-role", reduced)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await CallAsync(api, "/realm-role", reduced)).StatusCode);
        }
        finally
        {
            await Role.RemoveRealmRoleFromUserAsync(fixture.TestUserId, fixture.TestRealmRoleId, KeycloakFixture.TestRealmRoleName);
            await Role.RemoveClientRoleFromUserAsync(fixture.TestUserId, fixture.TestRoleId, KeycloakFixture.TestRoleName);
            foreach (var id in mapperIds)
                await Client.DeleteProtocolMapperAsync(fixture.SdkClientUuid, id);
        }
    }

    [Fact]
    public async Task TamperedToken_IsRejected()
    {
        var api = await CreateApiAsync();
        var token = await SignInAsync();
        var parts = token.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}.{parts[2][..^4]}AAAA";

        Assert.Equal(HttpStatusCode.Unauthorized, (await CallAsync(api, "/me", tampered)).StatusCode);
    }
}
