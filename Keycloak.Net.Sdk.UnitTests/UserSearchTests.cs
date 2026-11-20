using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Users;
using Keycloak.Net.Sdk.Users.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class UserSearchTests
{
    private readonly IOptions<KeycloakConfiguration> _options = Options.Create(new KeycloakConfiguration
    {
        ServerUrl    = "http://localhost:8080/",
        RealmName    = TestData.RealmName,
        ClientId     = TestData.ClientId,
        ClientSecret = TestData.ClientSecret,
        ClientUuid   = TestData.ClientUuid
    });

    private (UserManagement Sut, FakeHttpMessageHandler Handler) CreateSut()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create();
        return (new UserManagement(factory, _options), handler);
    }

    private static string DecodedQuery(FakeHttpMessageHandler handler) =>
        Uri.UnescapeDataString(handler.SentRequests.Single().RequestUri!.Query);

    // ── Attribute search (q) ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_WithOneAttribute_SendsQParameter()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var result = await sut.GetUsersAsync(new GetUsersQueryDto { Attributes = new Dictionary<string, string> { ["branch_id"] = "12" } });

        Assert.True(result.IsSuccessful);
        Assert.Equal("?q=branch_id%3A12", handler.SentRequests.Single().RequestUri!.Query);
    }

    [Fact]
    public async Task GetUsersAsync_WithSeveralAttributes_JoinsTermsWithSpace()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        await sut.GetUsersAsync(new GetUsersQueryDto
        {
            Attributes = new Dictionary<string, string> { ["company_id"] = "1", ["branch_id"] = "12" }
        });

        Assert.Equal("?q=company_id:1 branch_id:12", DecodedQuery(handler));
    }

    [Theory]
    [InlineData("region", "north east", "region:\"north east\"")]
    [InlineData("cost center", "7", "\"cost center\":7")]
    [InlineData("url", "http://x", "url:\"http://x\"")]
    public async Task GetUsersAsync_TermsWithSpaceOrColon_AreQuoted(string key, string value, string expected)
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        await sut.GetUsersAsync(new GetUsersQueryDto { Attributes = new Dictionary<string, string> { [key] = value } });

        Assert.Equal($"?q={expected}", DecodedQuery(handler));
    }

    [Fact]
    public async Task GetUsersAsync_AttributeValueWithAmpersand_IsSentAsOneQueryValue()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        await sut.GetUsersAsync(new GetUsersQueryDto { Attributes = new Dictionary<string, string> { ["team"] = "a&enabled=false" } });

        var query = handler.SentRequests.Single().RequestUri!.Query;
        Assert.DoesNotContain("&enabled", query);
        Assert.Equal("?q=team:a&enabled=false", DecodedQuery(handler));
    }

    [Fact]
    public async Task GetUsersAsync_EmptyAttributes_SendsNoQParameter()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        await sut.GetUsersAsync(new GetUsersQueryDto { Attributes = new Dictionary<string, string>() });

        Assert.Equal(string.Empty, handler.SentRequests.Single().RequestUri!.Query);
    }

    [Theory]
    [InlineData("key\"x", "1")]
    [InlineData("key", "va\"lue")]
    [InlineData("", "1")]
    [InlineData("key", "")]
    public async Task GetUsersAsync_InvalidAttributeTerm_ThrowsWithoutSending(string key, string value)
    {
        var (sut, handler) = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetUsersAsync(new GetUsersQueryDto { Attributes = new Dictionary<string, string> { [key] = value } }));

        Assert.Empty(handler.SentRequests);
    }

    // ── Exact / EmailVerified ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_WithExactAndEmailVerified_SendsBothParameters()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        await sut.GetUsersAsync(new GetUsersQueryDto { Username = "alice", Exact = true, EmailVerified = false });

        var query = handler.SentRequests.Single().RequestUri!.Query;
        Assert.Contains("username=alice", query);
        Assert.Contains("exact=true", query);
        Assert.Contains("emailVerified=false", query);
    }

    // ── GetUsersCountAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersCountAsync_NoQuery_CallsCountEndpoint()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "42");

        var result = await sut.GetUsersCountAsync();

        Assert.True(result.IsSuccessful);
        Assert.Equal(42, result.Response.Count);
        var request = handler.SentRequests.Single();
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.EndsWith($"admin/realms/{TestData.RealmName}/users/count", request.RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, request.RequestUri.Query);
    }

    [Fact]
    public async Task GetUsersCountAsync_WithFilters_SendsFiltersButNotPagingOrExact()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "3");

        await sut.GetUsersCountAsync(new GetUsersQueryDto
        {
            First      = 10,
            Max        = 5,
            Exact      = true,
            Enabled    = true,
            Search     = "ali",
            Attributes = new Dictionary<string, string> { ["branch_id"] = "12" }
        });

        var query = DecodedQuery(handler);
        Assert.Contains("enabled=true", query);
        Assert.Contains("search=ali", query);
        Assert.Contains("q=branch_id:12", query);
        Assert.DoesNotContain("first=", query);
        Assert.DoesNotContain("max=", query);
        Assert.DoesNotContain("exact=", query);
    }

    [Fact]
    public async Task GetUsersCountAsync_BodyWithWhitespace_IsParsed()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, " 7\n");

        var result = await sut.GetUsersCountAsync();

        Assert.True(result.IsSuccessful);
        Assert.Equal(7, result.Response.Count);
    }

    [Fact]
    public async Task GetUsersCountAsync_Forbidden_ReturnsFailure()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Forbidden);

        var result = await sut.GetUsersCountAsync();

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task GetUsersCountAsync_NonNumericBody_ReturnsFailure()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "{\"count\":1}");

        var result = await sut.GetUsersCountAsync();

        Assert.False(result.IsSuccessful);
        Assert.Contains("Unexpected user count response", result.ErrorMessage);
    }
}
