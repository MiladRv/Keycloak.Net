using System.Net.Http.Json;
using System.Text.Json;
using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

[Collection(nameof(KeycloakCollection))]
public class UserActionEmailIntegrationTests(KeycloakFixture fixture)
{
    [Theory]
    [InlineData("verify")]
    [InlineData("password")]
    [InlineData("actions")]
    public async Task UserEmail_IsDeliveredWithAnActionLink(string kind)
    {
        using var scope = fixture.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserManagement>();
        var username = $"email-{Guid.NewGuid():N}";
        var email = $"{username}@example.test";
        var signup = await users.SignupAsync(new SignupRequestDto(username, "EmailTest@123")
        {
            Email = email,
            Firstname = "Email",
            Lastname = "Test"
        });
        Assert.True(signup.IsSuccessful, signup.ErrorMessage);
        var found = await users.GetUserByUsernameAsync(username);
        Assert.True(found.IsSuccessful, found.ErrorMessage);
        var userId = Assert.Single(found.Response).Id;

        try
        {
            var options = new UserActionEmailOptions { LifespanSeconds = 600 };
            var result = kind switch
            {
                "verify" => await users.SendVerificationEmailAsync(userId, options),
                "password" => await users.SendPasswordResetEmailAsync(userId, options),
                _ => await users.ExecuteActionsEmailAsync(userId,
                    [UserRequiredActions.UpdateProfile, UserRequiredActions.UpdatePassword], options)
            };
            Assert.True(result.IsSuccessful, result.ErrorMessage);

            using var mailpit = new HttpClient { BaseAddress = fixture.MailpitUrl };
            using var messages = await mailpit.GetFromJsonAsync<JsonDocument>("api/v1/messages");
            var message = Assert.Single(messages!.RootElement.GetProperty("messages").EnumerateArray()
                .Where(message => message.GetProperty("To").EnumerateArray()
                    .Any(recipient => recipient.GetProperty("Address").GetString() == email)));
            Assert.False(string.IsNullOrWhiteSpace(message.GetProperty("Subject").GetString()));
            using var details = await mailpit.GetFromJsonAsync<JsonDocument>($"api/v1/message/{message.GetProperty("ID").GetString()}");
            Assert.Contains("/login-actions/action-token", details!.RootElement.GetProperty("HTML").GetString());
        }
        finally
        {
            await users.DeleteUserAsync(userId);
        }
    }
}
