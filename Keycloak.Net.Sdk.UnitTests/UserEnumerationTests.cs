using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Keycloak.Net.Sdk.Users;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class UserEnumerationTests
{
    private static (UserManagement Users, FakeHttpMessageHandler Handler) Create()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new UserManagement(factory, Options.Create(new KeycloakConfiguration
        {
            RealmName = TestData.RealmName,
            ClientId = TestData.ClientId
        })), handler);
    }

    private static string Page(params string[] ids)
        => JsonSerializer.Serialize(ids.Select(id => new { id, username = id }));

    private static async Task<List<UserInfoResponseDto>> Collect(IAsyncEnumerable<UserInfoResponseDto> source)
    {
        var users = new List<UserInfoResponseDto>();
        await foreach (var user in source) users.Add(user);
        return users;
    }

    [Fact]
    public async Task GetAllUsersAsync_PagesKeepFiltersAndAdvanceByResultsReturned()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.OK, Page("first"));
        handler.AddResponse(HttpStatusCode.OK, Page("second"));
        handler.AddResponse(HttpStatusCode.OK, "[]");
        var query = new GetUsersQueryDto { First = 5, Search = "a&b", Email = "user@example.com", Enabled = true };
        var original = query with { };

        var results = await Collect(users.GetAllUsersAsync(query, pageSize: 2));

        Assert.Equal(new[] { "first", "second" }, results.Select(user => user.Id));
        Assert.Equal(3, handler.SentRequests.Count);
        for (var index = 0; index < 3; index++)
        {
            var requestQuery = handler.SentRequests[index].RequestUri!.Query;
            Assert.Contains($"first={5 + index}", requestQuery);
            Assert.Contains("max=2", requestQuery);
            Assert.Contains("search=a%26b", requestQuery);
            Assert.Contains("email=user%40example.com", requestQuery);
            Assert.Contains("enabled=true", requestQuery);
        }
        Assert.Equal(original, query);
    }

    [Fact]
    public async Task GetAllUsersAsync_MaxLimitsTheTotalAcrossPages()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.OK, Page("one", "two"));
        handler.AddResponse(HttpStatusCode.OK, Page("three"));

        var results = await Collect(users.GetAllUsersAsync(new GetUsersQueryDto { Max = 3 }, pageSize: 2));

        Assert.Equal(3, results.Count);
        Assert.Equal(2, handler.SentRequests.Count);
        Assert.Contains("first=2&max=1", handler.SentRequests[1].RequestUri!.Query);
    }

    [Fact]
    public async Task GetAllUsersAsync_EarlyBreak_DoesNotRequestMorePages()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.OK, Page("one", "two"));
        var stream = users.GetAllUsersAsync(pageSize: 2);
        Assert.Empty(handler.SentRequests);

        await foreach (var user in stream)
        {
            Assert.Equal("one", user.Id);
            break;
        }

        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task GetAllUsersAsync_FailedPage_ReportsErrorAfterPreviouslyReturnedUsers()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.OK, Page("one"));
        handler.AddResponse(HttpStatusCode.Forbidden, "access denied");
        var returned = new List<string>();

        var error = await Assert.ThrowsAsync<KeycloakException>(async () =>
        {
            await foreach (var user in users.GetAllUsersAsync(pageSize: 1)) returned.Add(user.Id);
        });

        Assert.Equal(new[] { "one" }, returned);
        Assert.Equal(TestData.RealmName, error.RealmName);
        Assert.Equal(TestData.ClientId, error.ClientId);
        Assert.Contains("403", error.Message);
        Assert.Contains("access denied", error.Message);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithCancellation_StopsBeforeReturningMoreUsers()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.OK, Page("one", "two"));
        using var cancellation = new CancellationTokenSource();
        var returned = new List<string>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var user in users.GetAllUsersAsync().WithCancellation(cancellation.Token))
            {
                returned.Add(user.Id);
                cancellation.Cancel();
            }
        });

        Assert.Equal(new[] { "one" }, returned);
        Assert.Single(handler.SentRequests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAllUsersAsync_InvalidPageSize_DoesNotSendRequest(int pageSize)
    {
        var (users, handler) = Create();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Collect(users.GetAllUsersAsync(pageSize: pageSize)));
        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task GetAllUsersAsync_ZeroMax_DoesNotSendRequest()
    {
        var (users, handler) = Create();
        Assert.Empty(await Collect(users.GetAllUsersAsync(new GetUsersQueryDto { Max = 0 })));
        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task GetAllUsersAsync_EmptyRealm_ReturnsNoUsers()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.OK, "[]");
        Assert.Empty(await Collect(users.GetAllUsersAsync()));
        Assert.Single(handler.SentRequests);
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, -1)]
    public async Task GetAllUsersAsync_NegativeOffsetOrLimit_DoesNotSendRequest(int? first, int? max)
    {
        var (users, handler) = Create();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Collect(users.GetAllUsersAsync(new GetUsersQueryDto { First = first, Max = max })));
        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task GetAllUsersAsync_NullList_IsNotTreatedAsAnEmptyRealm()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.OK, "null");
        await Assert.ThrowsAsync<KeycloakException>(() => Collect(users.GetAllUsersAsync()));
    }
}
