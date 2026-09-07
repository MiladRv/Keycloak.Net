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

        var result = await sut.SetUserPasswordAsync(TestData.UserId, "newPassword!");

        Assert.True(result.IsSuccessful);
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

        var result = await sut.DeleteUserAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Contains($"users/{TestData.UserId}", handler.SentRequests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
    }

    // ── EnableUserAsync / DisableUserAsync ────────────────────────────────────

    [Fact]
    public async Task EnableUserAsync_Success_GetsThenPutsFullRepresentationWithEnabledTrue()
    {
        var (sut, handler) = CreateSut();
        // First call: GET the existing user; second call: PUT it back with "enabled" flipped
        handler.AddResponse(HttpStatusCode.OK, TestData.UserWithAttributesResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.EnableUserAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        var body = await handler.SentRequests[1].Content!.ReadAsStringAsync();
        Assert.Contains("\"enabled\":true", body);
        // Fields not related to the enabled flag must survive the round trip
        Assert.Contains(TestData.Username, body);
        Assert.Contains("department", body);
    }

    [Fact]
    public async Task DisableUserAsync_Success_GetsThenPutsFullRepresentationWithEnabledFalse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserWithAttributesResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DisableUserAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        var body = await handler.SentRequests[1].Content!.ReadAsStringAsync();
        Assert.Contains("\"enabled\":false", body);
        Assert.Contains(TestData.Username, body);
    }

    [Fact]
    public async Task EnableUserAsync_UserNotFound_ReturnsFailureWithoutPutting()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.EnableUserAsync("nonexistent-id");

        Assert.False(result.IsSuccessful);
        Assert.Single(handler.SentRequests);
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

    // ── UpdateUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_Success_SendsPutWithCorrectPayload()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.UpdateUserAsync(TestData.UserId, new UpdateUserRequestDto
        {
            Email     = "new@example.com",
            FirstName = "John",
            LastName  = "Doe"
        });

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[0].Method);
        Assert.Contains($"users/{TestData.UserId}", handler.SentRequests[0].RequestUri!.ToString());
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("\"email\":\"new@example.com\"", body);
        Assert.Contains("\"firstName\":\"John\"", body);
        Assert.Contains("\"lastName\":\"Doe\"", body);
    }

    [Fact]
    public async Task UpdateUserAsync_PartialUpdate_OnlySendsProvidedFields()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.UpdateUserAsync(TestData.UserId, new UpdateUserRequestDto { FirstName = "Jane" });

        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("\"firstName\":\"Jane\"", body);
    }

    // ── GetUserAttributesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserAttributesAsync_UserHasAttributes_ReturnsAttributesDictionary()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserWithAttributesResponse);

        var result = await sut.GetUserAttributesAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.True(result.Response.ContainsKey("department"));
        Assert.Equal("engineering", result.Response["department"][0]);
    }

    [Fact]
    public async Task GetUserAttributesAsync_UserHasNoAttributes_ReturnsEmptyDictionary()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserInfoResponse);

        var result = await sut.GetUserAttributesAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Response);
    }

    [Fact]
    public async Task GetUserAttributesAsync_UserNotFound_ReturnsFailure()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.GetUserAttributesAsync("nonexistent-id");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    // ── SetUserAttributeAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SetUserAttributeAsync_Success_GetsThenPutsWithAttribute()
    {
        var (sut, handler) = CreateSut();
        // First call: GET user; second call: PUT with attributes
        handler.AddResponse(HttpStatusCode.OK, TestData.UserInfoResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.SetUserAttributeAsync(TestData.UserId, "tenantId", "tenant-abc");

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Get, handler.SentRequests[0].Method);
        Assert.Equal(HttpMethod.Put, handler.SentRequests[1].Method);
        var body = await handler.SentRequests[1].Content!.ReadAsStringAsync();
        Assert.Contains("tenantId", body);
        Assert.Contains("tenant-abc", body);
        // The rest of the user representation must survive the round trip
        Assert.Contains(TestData.Username, body);
        Assert.Contains("\"enabled\":true", body);
    }

    [Fact]
    public async Task SetUserAttributeAsync_PreservesExistingAttributes()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserWithAttributesResponse);
        handler.AddResponse(HttpStatusCode.NoContent);

        await sut.SetUserAttributeAsync(TestData.UserId, "tenantId", "tenant-xyz");

        var body = await handler.SentRequests[1].Content!.ReadAsStringAsync();
        Assert.Contains("department", body);
        Assert.Contains("tenantId", body);
    }

    // ── GetUsersByEmailAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersByEmailAsync_Success_ReturnsMatchingUsers()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.UserListResponse);

        var result = await sut.GetUsersByEmailAsync("test@example.com");

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        var query = handler.SentRequests[0].RequestUri!.Query;
        Assert.Contains("email=test%40example.com", query);
        Assert.Contains("exact=true", query);
    }

    [Fact]
    public async Task GetUsersByEmailAsync_NoMatch_ReturnsEmptyList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, "[]");

        var result = await sut.GetUsersByEmailAsync("nobody@example.com");

        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Response);
    }

    // ── SetUserAttributeAsync failure ────────────────────────────────────────

    [Fact]
    public async Task SetUserAttributeAsync_UserNotFound_ReturnsFailureWithoutPutting()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.SetUserAttributeAsync("nonexistent-id", "tenantId", "tenant-abc");

        Assert.False(result.IsSuccessful);
        Assert.Single(handler.SentRequests);
    }

    // ── GetUserCredentialsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetUserCredentialsAsync_Success_ReturnsCredentialList()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.OK, TestData.CredentialsResponse);

        var result = await sut.GetUserCredentialsAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Response);
        Assert.Equal(TestData.CredentialId, result.Response[0].Id);
        Assert.Equal("password", result.Response[0].Type);
        Assert.Contains($"users/{TestData.UserId}/credentials", handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task GetUserCredentialsAsync_UserNotFound_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.GetUserCredentialsAsync("nonexistent-id");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    // ── DeleteUserCredentialAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserCredentialAsync_Success_SendsDeleteRequest()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await sut.DeleteUserCredentialAsync(TestData.UserId, TestData.CredentialId);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpMethod.Delete, handler.SentRequests[0].Method);
        Assert.Contains($"users/{TestData.UserId}/credentials/{TestData.CredentialId}", handler.SentRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task DeleteUserCredentialAsync_NotFound_ReturnsFailureResponse()
    {
        var (sut, handler) = CreateSut();
        handler.AddResponse(HttpStatusCode.NotFound);

        var result = await sut.DeleteUserCredentialAsync(TestData.UserId, "nonexistent-credential");

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
