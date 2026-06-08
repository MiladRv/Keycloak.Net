using System.Net;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.Users;
using Keycloak.Net.Sdk.Users.Contracts;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class UserManagementTests
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

    // ── SignupAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SignupAsync_Success_ReturnsSuccessResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Created);

        var result = await sut.SignupAsync(new SignupRequestDto(TestData.Username, TestData.Password));

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Contains($"admin/realms/{TestData.RealmName}/users", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.SentRequests[0].Method);
    }

    [Fact]
    public async Task SignupAsync_UserAlreadyExists_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Conflict);

        var result = await sut.SignupAsync(new SignupRequestDto(TestData.Username, TestData.Password));

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    // ── SigninAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SigninAsync_Success_ReturnsTokens()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);

        var result = await sut.SigninAsync(TestData.Username, TestData.Password);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.AccessToken, result.Response.AccessToken);
        Assert.Equal(TestData.RefreshToken, result.Response.RefreshToken);
        Assert.Equal(300, result.Response.ExpiresIn);
        Assert.Contains($"realms/{TestData.RealmName}/protocol/openid-connect/token",
            handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task SigninAsync_InvalidCredentials_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Unauthorized);

        var result = await sut.SigninAsync(TestData.Username, "wrong-password");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task SigninAsync_DoesNotMutateSharedDefaultRequestHeaders()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.SigninResponse);

        await sut.SigninAsync(TestData.Username, TestData.Password);

        // Authorization must be set per-request, not on shared DefaultRequestHeaders
        var authHeader = handler.SentRequests[0].Headers.Authorization;
        Assert.Null(authHeader);
    }

    // ── GetUserAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserAsync_Success_ReturnsUserInfo()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserInfoResponse);

        var result = await sut.GetUserAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(TestData.UserId, result.Response.Id);
        Assert.Equal(TestData.Username, result.Response.Username);
        Assert.True(result.Response.Enabled);
        Assert.Contains($"users/{TestData.UserId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetUserAsync_NotFound_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.GetUserAsync("nonexistent-id");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    // ── GetUserByUsernameAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByUsernameAsync_Success_ReturnsUserList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var result = await sut.GetUserByUsernameAsync(TestData.Username);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.UserId, result.Response[0].Id);
        Assert.Contains($"username={TestData.Username}", handler.SentRequests[0].RequestUri!.Query);
    }

    // ── SetUserPasswordAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SetUserPasswordAsync_Success_SendsCorrectRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.SetUserPasswordAsync(TestData.UserId, "newPassword!");

        Assert.Contains($"users/{TestData.UserId}/reset-password", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("\"type\":\"password\"", body);
        Assert.Contains("\"value\":\"newPassword!\"", body);
    }

    // ── DeleteUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.DeleteUserAsync(TestData.UserId);

        Assert.Contains($"users/{TestData.UserId}", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
    }

    // ── EnableUserAsync / DisableUserAsync ────────────────────────────────────

    [Fact]
    public async Task EnableUserAsync_Success_SendsEnabledTrue()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.EnableUserAsync(TestData.UserId);

        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("\"enabled\":true", body);
    }

    [Fact]
    public async Task DisableUserAsync_Success_SendsEnabledFalse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.DisableUserAsync(TestData.UserId);

        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("\"enabled\":false", body);
    }

    // ── GetUsersAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_NoQuery_ReturnsUserList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var result = await sut.GetUsersAsync();

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.UserId, result.Response[0].Id);
        Assert.DoesNotContain("?", handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_SendsFirstAndMaxParams()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var query = new GetUsersQueryDto { First = 10, Max = 5 };
        await sut.GetUsersAsync(query);

        var requestUri = handler.SentRequests[0].RequestUri!.Query;
        Assert.Contains("first=10", requestUri);
        Assert.Contains("max=5", requestUri);
    }

    [Fact]
    public async Task GetUsersAsync_WithSearch_SendsSearchParam()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var query = new GetUsersQueryDto { Search = "john" };
        await sut.GetUsersAsync(query);

        Assert.Contains("search=john", handler.SentRequests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task GetUsersAsync_WithFilters_SendsAllFilterParams()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var query = new GetUsersQueryDto
        {
            Username  = "alice",
            Email     = "alice@example.com",
            FirstName = "Alice",
            LastName  = "Smith",
            Enabled   = true
        };
        await sut.GetUsersAsync(query);

        var qs = handler.SentRequests[0].RequestUri!.Query;
        Assert.Contains("username=alice", qs);
        Assert.Contains("email=alice%40example.com", qs);
        Assert.Contains("firstName=Alice", qs);
        Assert.Contains("lastName=Smith", qs);
        Assert.Contains("enabled=true", qs);
    }

    [Fact]
    public async Task GetUsersAsync_EmptyResult_ReturnsEmptyList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        var result = await sut.GetUsersAsync(new GetUsersQueryDto { Max = 10 });

        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Response);
    }

    [Fact]
    public async Task GetUsersAsync_Forbidden_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.Forbidden);

        var result = await sut.GetUsersAsync();

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }
}
