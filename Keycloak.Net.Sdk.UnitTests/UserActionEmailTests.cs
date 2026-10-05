using System.Net;
using System.Text.Json;
using Keycloak.Net.Sdk.Configurations;
using Keycloak.Net.Sdk.UnitTests.Helpers;
using Keycloak.Net.Sdk.Users;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keycloak.Net.Sdk.UnitTests;

public class UserActionEmailTests
{
    private static (UserManagement Users, FakeHttpMessageHandler Handler) Create()
    {
        var (factory, handler) = HttpClientFactoryHelper.Create("https://example.com/auth/");
        var options = Options.Create(new KeycloakConfiguration { RealmName = TestData.RealmName });
        return (new UserManagement(factory, options), handler);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_UsesVerificationEndpointWithoutBody()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await users.SendVerificationEmailAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        var request = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal($"/auth/admin/realms/{TestData.RealmName}/users/{TestData.UserId}/send-verify-email", request.RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, request.RequestUri.Query);
        Assert.Null(request.Content);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_UsesUpdatePasswordAction()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.NoContent);

        var result = await users.SendPasswordResetEmailAsync(TestData.UserId);

        Assert.True(result.IsSuccessful);
        var request = Assert.Single(handler.SentRequests);
        Assert.EndsWith("/execute-actions-email", request.RequestUri!.AbsolutePath);
        Assert.Equal(new[] { "UPDATE_PASSWORD" }, JsonSerializer.Deserialize<string[]>(await request.Content!.ReadAsStringAsync()));
    }

    [Fact]
    public async Task ExecuteActionsEmailAsync_EncodesOptionsAndKeepsCustomActions()
    {
        var (users, handler) = Create();
        handler.AddResponse(HttpStatusCode.NoContent);
        string[] actions = [UserRequiredActions.VerifyEmail, "custom-action"];

        var result = await users.ExecuteActionsEmailAsync(TestData.UserId, actions, new UserActionEmailOptions
        {
            ClientId = "client&one",
            RedirectUri = "https://app.example.com/done?next=profile&source=email#section",
            LifespanSeconds = 600
        });

        Assert.True(result.IsSuccessful);
        var request = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal(actions, JsonSerializer.Deserialize<string[]>(await request.Content.ReadAsStringAsync()));
        Assert.Equal("?client_id=client%26one&redirect_uri=https%3A%2F%2Fapp.example.com%2Fdone%3Fnext%3Dprofile%26source%3Demail%23section&lifespan=600", request.RequestUri!.Query);
        Assert.Empty(request.RequestUri.Fragment);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task EmailRequest_Failure_PreservesStatusAndErrorBody(HttpStatusCode status)
    {
        var (users, handler) = Create();
        handler.AddResponse(status, "{\"errorMessage\":\"Unable to send email\"}");

        var result = await users.SendVerificationEmailAsync(TestData.UserId);

        Assert.False(result.IsSuccessful);
        Assert.Equal(status, result.StatusCode);
        Assert.Contains("Unable to send email", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task EmailRequest_InvalidLifetime_DoesNotSendRequest(int lifespan)
    {
        var (users, handler) = Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => users.SendVerificationEmailAsync(
            TestData.UserId, new UserActionEmailOptions { LifespanSeconds = lifespan }));

        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task ExecuteActionsEmailAsync_InvalidActions_DoesNotSendRequest()
    {
        var (users, handler) = Create();

        await Assert.ThrowsAsync<ArgumentNullException>(() => users.ExecuteActionsEmailAsync(TestData.UserId, null!));
        await Assert.ThrowsAsync<ArgumentException>(() => users.ExecuteActionsEmailAsync(TestData.UserId, []));
        await Assert.ThrowsAsync<ArgumentException>(() => users.ExecuteActionsEmailAsync(TestData.UserId, [" "]));

        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task EmailRequest_InvalidRedirect_DoesNotSendRequest()
    {
        var (users, handler) = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => users.SendVerificationEmailAsync(
            TestData.UserId, new UserActionEmailOptions { RedirectUri = "/done" }));

        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task EmailRequest_Cancelled_DoesNotSendRequest()
    {
        var (users, handler) = Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => users.SendPasswordResetEmailAsync(
            TestData.UserId, cancellationToken: cancellation.Token));

        Assert.Empty(handler.SentRequests);
    }
}
