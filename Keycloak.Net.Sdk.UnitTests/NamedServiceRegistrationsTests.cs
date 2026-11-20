using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Keycloak.Net.Sdk.Authentications.Contracts;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.Realms.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class NamedServiceRegistrationsTests
{
    private static void Configure(KeycloakConfiguration settings, string realm, string server = "https://example.com/auth/")
    {
        settings.ServerUrl = server;
        settings.RealmName = realm;
        settings.ClientId = $"{realm}-client";
        settings.ClientSecret = $"{realm}-secret";
        settings.AdminUsername = $"{realm}-admin";
        settings.AdminPassword = $"{realm}-password";
    }

    [Fact]
    public async Task NamedConnections_KeepServersCredentialsAndCachedTokensSeparate()
    {
        var requests = new ConcurrentBag<(Uri Uri, string? Authorization, string Body)>();
        var services = new ServiceCollection();
        services.AddKeycloak("first", settings => Configure(settings, "first", "https://first.example.com/auth"));
        services.AddKeycloak("second", settings => Configure(settings, "second", "https://second.example.com/auth/"));
        services.ConfigureAll<HttpClientFactoryOptions>(options => options.HttpMessageHandlerBuilderActions.Add(
            builder => builder.PrimaryHandler = new RecordingHandler(requests)));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var first = scope.ServiceProvider.GetRequiredKeyedService<IUserManagement>("first");
        var second = scope.ServiceProvider.GetRequiredKeyedService<IUserManagement>("second");

        await Task.WhenAll(first.GetUsersAsync(), second.GetUsersAsync());
        using var otherScope = provider.CreateScope();
        await otherScope.ServiceProvider.GetRequiredKeyedService<IUserManagement>("first").GetUsersAsync();
        await otherScope.ServiceProvider.GetRequiredKeyedService<IUserManagement>("second").GetUsersAsync();

        var tokens = requests.Where(request => request.Uri.AbsolutePath.EndsWith("/token")).ToList();
        Assert.Equal(2, tokens.Count);
        foreach (var realm in new[] { "first", "second" })
        {
            var tokenRequest = Assert.Single(tokens.Where(request => request.Uri.Host == $"{realm}.example.com"));
            Assert.Equal($"/auth/realms/{realm}/protocol/openid-connect/token", tokenRequest.Uri.AbsolutePath);
            Assert.Contains($"client_id={realm}-client", tokenRequest.Body);
            Assert.Contains($"client_secret={realm}-secret", tokenRequest.Body);
            Assert.Null(tokenRequest.Authorization);
            var userRequests = requests.Where(request => request.Uri.AbsolutePath == $"/auth/admin/realms/{realm}/users").ToList();
            Assert.Equal(2, userRequests.Count);
            Assert.All(userRequests, request => Assert.Equal($"Bearer {realm}-token", request.Authorization));
        }
    }

    [Fact]
    public async Task NamedConnections_KeepAdminCredentialsAndTokenCachesSeparate()
    {
        var requests = new ConcurrentBag<(Uri Uri, string? Authorization, string Body)>();
        var services = new ServiceCollection();
        foreach (var name in new[] { "first", "second" })
            services.AddKeycloak(name, settings => Configure(settings, name, $"https://{name}.example.com/auth/"));
        services.ConfigureAll<HttpClientFactoryOptions>(options => options.HttpMessageHandlerBuilderActions.Add(
            builder => builder.PrimaryHandler = new RecordingHandler(requests)));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        foreach (var name in new[] { "first", "second" })
        {
            var realms = scope.ServiceProvider.GetRequiredKeyedService<IKeycloakManagement>(name).RealmManagement;
            await realms.GetRealmsAsync();
            await realms.GetRealmsAsync();
        }

        foreach (var name in new[] { "first", "second" })
        {
            var token = Assert.Single(requests.Where(request => request.Uri.Host == $"{name}.example.com" &&
                request.Uri.AbsolutePath.EndsWith("/token")));
            Assert.Equal("/auth/realms/master/protocol/openid-connect/token", token.Uri.AbsolutePath);
            Assert.Contains($"username={name}-admin", token.Body);
            Assert.Contains($"password={name}-password", token.Body);
            var adminRequests = requests.Where(request => request.Uri.Host == $"{name}.example.com" &&
                request.Uri.AbsolutePath == "/auth/admin/realms").ToList();
            Assert.Equal(2, adminRequests.Count);
            Assert.All(adminRequests, request => Assert.Equal($"Bearer {name}-token", request.Authorization));
        }
    }

    [Fact]
    public void NamedAndDefaultConnections_CoexistWithIndependentScopedManagers()
    {
        var services = new ServiceCollection();
        services.AddKeycloak(new ConfigurationBuilder().Build(), settings => Configure(settings, "default"));
        services.AddKeycloak("named", settings => Configure(settings, "named"));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var named = scope.ServiceProvider.GetRequiredKeyedService<IKeycloakManagement>("named");
        var unnamed = scope.ServiceProvider.GetRequiredService<IKeycloakManagement>();

        Assert.NotSame(named.UserManagement, unnamed.UserManagement);
        Assert.Same(named.UserManagement, scope.ServiceProvider.GetRequiredKeyedService<IUserManagement>("named"));
        Assert.NotNull(named.ClientManagement);
        Assert.NotNull(named.GroupManagement);
        Assert.NotNull(named.RealmManagement);
        Assert.NotNull(named.RoleManagement);
        Assert.NotNull(named.TokenManagement);
        Assert.NotNull(named.UserSessionManagement);
        Assert.NotSame(provider.GetRequiredService<ITokenProvider>(), provider.GetRequiredKeyedService<ITokenProvider>("named"));
        Assert.NotSame(provider.GetRequiredService<IRealmAdminTokenProvider>(), provider.GetRequiredKeyedService<IRealmAdminTokenProvider>("named"));
        using var secondScope = provider.CreateScope();
        Assert.NotSame(named, secondScope.ServiceProvider.GetRequiredKeyedService<IKeycloakManagement>("named"));
        Assert.Equal("default", provider.GetRequiredService<IOptions<KeycloakConfiguration>>().Value.RealmName);
        Assert.Equal("named", provider.GetRequiredKeyedService<IOptions<KeycloakConfiguration>>("named").Value.RealmName);
    }

    [Fact]
    public void NamedConnection_BindsSectionAndAppliesCallbackOnce()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["realms:store:ServerUrl"] = "https://example.com/",
            ["realms:store:RealmName"] = "store"
        }).Build();
        var services = new ServiceCollection();
        var calls = 0;
        services.AddKeycloak("store", configuration.GetSection("realms:store"), settings =>
        {
            calls++;
            settings.ClientId = "overridden";
        });
        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredKeyedService<IOptions<KeycloakConfiguration>>("store").Value;

        Assert.Equal("store", settings.RealmName);
        Assert.Equal("overridden", settings.ClientId);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void NamedConnection_DuplicateName_IsRejected()
    {
        var services = new ServiceCollection();
        services.AddKeycloak("store", settings => Configure(settings, "store"));

        Assert.Throws<InvalidOperationException>(() => services.AddKeycloak("store", settings => Configure(settings, "another")));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void NamedConnection_EmptyName_IsRejected(string? name)
    {
        var services = new ServiceCollection();
        Assert.ThrowsAny<ArgumentException>(() => services.AddKeycloak(name!, settings => Configure(settings, "store")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/auth/")]
    [InlineData("ftp://example.com/")]
    public void NamedConnection_InvalidServer_IsRejected(string server)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddKeycloak("store", settings => Configure(settings, "store", server)));
    }

    private sealed class RecordingHandler(ConcurrentBag<(Uri Uri, string? Authorization, string Body)> requests) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            requests.Add((uri, request.Headers.Authorization?.ToString(), body));
            var realm = uri.Host.Split('.')[0];
            var responseBody = uri.AbsolutePath.EndsWith("/token")
                ? $"{{\"access_token\":\"{realm}-token\",\"expires_in\":300}}"
                : "[]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
