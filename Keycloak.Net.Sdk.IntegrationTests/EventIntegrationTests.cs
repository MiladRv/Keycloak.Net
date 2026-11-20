using Keycloak.Net.Sdk.Events.Contracts;
using Keycloak.Net.Sdk.Groups.Contracts;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// The realm's audit trail: login events and admin events, recorded after enabling them.
/// The test restores the realm's events configuration afterwards.
/// </summary>
[Collection(nameof(KeycloakCollection))]
public class EventIntegrationTests(KeycloakFixture fixture)
{
    private IServiceProvider Scoped => fixture.Services.CreateScope().ServiceProvider;
    private IEventManagement Events => Scoped.GetRequiredService<IEventManagement>();
    private IUserManagement User => Scoped.GetRequiredService<IUserManagement>();
    private IGroupManagement Group => Scoped.GetRequiredService<IGroupManagement>();

    private static async Task<T> EventuallyAsync<T>(Func<Task<T>> read, Func<T, bool> done)
    {
        var value = await read();
        for (var attempt = 0; attempt < 20 && !done(value); attempt++)
        {
            await Task.Delay(250);
            value = await read();
        }
        return value;
    }

    [Fact]
    public async Task EventsConfig_LoginAndAdminEvents_AreRecordedAndQueried()
    {
        var original = (await Events.GetEventsConfigAsync()).Response;
        string? groupId = null;
        try
        {
            var update = await Events.UpdateEventsConfigAsync(new UpdateEventsConfigRequestDto
            {
                EventsEnabled             = true,
                EventsExpiration          = 3600,
                AdminEventsEnabled        = true,
                AdminEventsDetailsEnabled = true
            });
            Assert.True(update.IsSuccessful, update.ErrorMessage);

            var config = (await Events.GetEventsConfigAsync()).Response;
            Assert.True(config.EventsEnabled);
            Assert.True(config.AdminEventsEnabled);
            Assert.True(config.AdminEventsDetailsEnabled);
            Assert.Equal(3600, config.EventsExpiration);
            Assert.Equal(original.EventsListeners ?? [], config.EventsListeners ?? []);

            // User events
            Assert.True((await User.SigninAsync(KeycloakFixture.TestUsername, KeycloakFixture.TestPassword)).IsSuccessful);
            Assert.False((await User.SigninAsync(KeycloakFixture.TestUsername, "wrong-password")).IsSuccessful);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var userEvents = await EventuallyAsync(
                () => Events.GetEventsAsync(new GetEventsQueryDto
                {
                    User     = fixture.TestUserId,
                    Types    = ["LOGIN", "LOGIN_ERROR"],
                    DateFrom = today.AddDays(-1),
                    Max      = 50
                }),
                r => r.IsSuccessful && r.Response.Any(e => e.Type == "LOGIN") && r.Response.Any(e => e.Type == "LOGIN_ERROR"));
            Assert.True(userEvents.IsSuccessful, userEvents.ErrorMessage);
            var login = userEvents.Response.First(e => e.Type == "LOGIN");
            Assert.Equal(KeycloakFixture.SdkClientId, login.ClientId);
            Assert.Equal(fixture.TestUserId, login.UserId);
            Assert.True(login.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-10));
            Assert.Equal("invalid_user_credentials", userEvents.Response.First(e => e.Type == "LOGIN_ERROR").Error);
            Assert.All(userEvents.Response, e => Assert.Contains(e.Type, new[] { "LOGIN", "LOGIN_ERROR" }));

            var onlyErrors = await Events.GetEventsAsync(new GetEventsQueryDto { User = fixture.TestUserId, Types = ["LOGIN_ERROR"] });
            Assert.NotEmpty(onlyErrors.Response);
            Assert.All(onlyErrors.Response, e => Assert.Equal("LOGIN_ERROR", e.Type));

            // Admin events
            var groupName = $"audited-{Guid.NewGuid():N}";
            Assert.True((await Group.CreateGroupAsync(new CreateGroupRequestDto { Name = groupName })).IsSuccessful);
            groupId = (await Group.GetGroupByPathAsync(groupName)).Response.Id;

            var adminEvents = await EventuallyAsync(
                () => Events.GetAdminEventsAsync(new GetAdminEventsQueryDto
                {
                    OperationTypes = ["CREATE"],
                    ResourceTypes  = ["GROUP"],
                    ResourcePath   = $"groups/{groupId}"
                }),
                r => r.IsSuccessful && r.Response.Count > 0);
            Assert.True(adminEvents.IsSuccessful, adminEvents.ErrorMessage);
            var created = Assert.Single(adminEvents.Response);
            Assert.Equal("CREATE", created.OperationType);
            Assert.Equal("GROUP", created.ResourceType);
            Assert.Contains(groupName, created.Representation);
            Assert.False(string.IsNullOrEmpty(created.AuthDetails?.UserId));

            // Clearing
            Assert.True((await Events.ClearAdminEventsAsync()).IsSuccessful);
            Assert.Empty((await Events.GetAdminEventsAsync(new GetAdminEventsQueryDto { ResourcePath = $"groups/{groupId}" })).Response);
            Assert.True((await Events.ClearEventsAsync()).IsSuccessful);
            Assert.Empty((await Events.GetEventsAsync(new GetEventsQueryDto { User = fixture.TestUserId })).Response);
        }
        finally
        {
            if (groupId is not null) await Group.DeleteGroupAsync(groupId);
            await Events.UpdateEventsConfigAsync(new UpdateEventsConfigRequestDto
            {
                EventsEnabled             = original.EventsEnabled,
                AdminEventsEnabled        = original.AdminEventsEnabled,
                AdminEventsDetailsEnabled = original.AdminEventsDetailsEnabled
            });
        }
    }
}
