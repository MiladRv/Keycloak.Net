using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Events;
using Keycloak.Net.Sdk.Events.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class EventManagementTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (EventManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new EventManagement(factory, _options), handler);
    }

    private static string Realm => $"/admin/realms/{TestData.RealmName}";

    [Fact]
    public async Task GetEventsAsync_NoQuery_CallsEventsEndpoint()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """
            [ { "time": 1700000000000, "type": "LOGIN", "clientId": "erp", "userId": "u1", "ipAddress": "10.0.0.1",
                "details": { "username": "alice", "auth_method": "openid-connect" } } ]
            """);

        var result = await sut.GetEventsAsync();

        var login = Assert.Single(result.Response);
        Assert.Equal("LOGIN", login.Type);
        Assert.Equal("alice", login.Details!["username"]);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), login.Timestamp);
        Assert.Equal($"{Realm}/events", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task GetEventsAsync_WithFilters_RepeatsTypeAndFormatsDates()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        await sut.GetEventsAsync(new GetEventsQueryDto
        {
            Types     = ["LOGIN", "LOGIN_ERROR"],
            Client    = "erp web",
            User      = "u1",
            IpAddress = "10.0.0.1",
            DateFrom  = new DateOnly(2026, 1, 2),
            DateTo    = new DateOnly(2026, 1, 31),
            First     = 0,
            Max       = 100
        });

        Assert.Equal(
            "?type=LOGIN&type=LOGIN_ERROR&client=erp%20web&user=u1&ipAddress=10.0.0.1&dateFrom=2026-01-02&dateTo=2026-01-31&first=0&max=100",
            handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task GetAdminEventsAsync_WithFilters_SendsAllParameters()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """
            [ { "time": 1, "realmId": "r", "operationType": "CREATE", "resourceType": "GROUP", "resourcePath": "groups/g1",
                "representation": "{\"name\":\"x\"}", "authDetails": { "realmId": "r", "clientId": "c", "userId": "admin", "ipAddress": "1.1.1.1" } } ]
            """);

        var result = await sut.GetAdminEventsAsync(new GetAdminEventsQueryDto
        {
            OperationTypes = ["CREATE", "DELETE"],
            ResourceTypes  = ["GROUP"],
            ResourcePath   = "groups/*",
            AuthUser       = "admin",
            DateFrom       = new DateOnly(2026, 3, 4),
            Max            = 10
        });

        var created = Assert.Single(result.Response);
        Assert.Equal("admin", created.AuthDetails!.UserId);
        Assert.Equal("""{"name":"x"}""", created.Representation);
        Assert.Equal($"{Realm}/admin-events", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal(
            "?operationTypes=CREATE&operationTypes=DELETE&resourceTypes=GROUP&resourcePath=groups%2F%2A&authUser=admin&dateFrom=2026-03-04&max=10",
            handler.SentRequests[0].RequestUri!.Query.Replace("*", "%2A"));
    }

    [Fact]
    public async Task UpdateEventsConfigAsync_MergesIntoExistingConfig()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """
            { "eventsEnabled": false, "eventsListeners": ["jboss-logging"], "enabledEventTypes": ["LOGIN"],
              "adminEventsEnabled": false, "adminEventsDetailsEnabled": false }
            """);
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.UpdateEventsConfigAsync(new UpdateEventsConfigRequestDto { EventsEnabled = true, EventsExpiration = 86400 });

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        Assert.Equal($"{Realm}/events/config", handler.SentRequests[1].RequestUri!.AbsolutePath);
        using var body = JsonDocument.Parse(await handler.SentRequests[1].Content!.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("eventsEnabled").GetBoolean());
        Assert.Equal(86400, body.RootElement.GetProperty("eventsExpiration").GetInt64());
        Assert.Equal("jboss-logging", body.RootElement.GetProperty("eventsListeners")[0].GetString());
        Assert.Equal("LOGIN", body.RootElement.GetProperty("enabledEventTypes")[0].GetString());
        Assert.False(body.RootElement.GetProperty("adminEventsEnabled").GetBoolean());
    }

    [Fact]
    public async Task UpdateEventsConfigAsync_ReplacesListsWhenGiven()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """{ "eventsListeners": ["jboss-logging"], "enabledEventTypes": ["LOGIN"] }""");
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateEventsConfigAsync(new UpdateEventsConfigRequestDto
        {
            EventsListeners   = ["jboss-logging", "email"],
            EnabledEventTypes = []
        });

        using var body = JsonDocument.Parse(await handler.SentRequests[1].Content!.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetProperty("eventsListeners").GetArrayLength());
        Assert.Equal(0, body.RootElement.GetProperty("enabledEventTypes").GetArrayLength());
    }

    [Fact]
    public async Task UpdateEventsConfigAsync_GetFails_DoesNotPut()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Forbidden);

        var result = await sut.UpdateEventsConfigAsync(new UpdateEventsConfigRequestDto { EventsEnabled = true });

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task GetEventsConfigAsync_ParsesConfig()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, """{ "eventsEnabled": true, "eventsExpiration": 600, "adminEventsEnabled": true, "adminEventsDetailsEnabled": true }""");

        var result = await sut.GetEventsConfigAsync();

        Assert.True(result.Response.EventsEnabled);
        Assert.Equal(600, result.Response.EventsExpiration);
        Assert.True(result.Response.AdminEventsDetailsEnabled);
    }

    [Fact]
    public async Task ClearMethods_SendDeletes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);
        handler.AddResponse(HttpStatusCode.NoContent);

        Assert.True((await sut.ClearEventsAsync()).IsSuccessful);
        Assert.True((await sut.ClearAdminEventsAsync()).IsSuccessful);

        Assert.All(handler.SentRequests, r => Assert.Equal(HttpMethod.Delete, r.Method));
        Assert.Equal($"{Realm}/events", handler.SentRequests[0].RequestUri!.AbsolutePath);
        Assert.Equal($"{Realm}/admin-events", handler.SentRequests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetEventsAsync_Forbidden_ReturnsFailure()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Forbidden);

        var result = await sut.GetEventsAsync();

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public void AddKeycloak_RegistersEventManagement_PlainAndNamed()
    {
        var services = new ServiceCollection();
        services.AddKeycloak("erp", s => { s.ServerUrl = "https://sso.example.test/"; s.RealmName = "erp"; });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<EventManagement>(scope.ServiceProvider.GetRequiredKeyedService<IEventManagement>("erp"));
    }
}
