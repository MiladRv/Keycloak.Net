using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

[Collection(nameof(KeycloakCollection))]
public class UserEnumerationIntegrationTests(KeycloakFixture fixture)
{
    [Fact]
    public async Task GetAllUsersAsync_FiltersAndStreamsMultiplePages()
    {
        using var scope = fixture.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredKeyedService<IUserManagement>("secondary");
        var prefix = $"stream-{Guid.NewGuid():N}";
        var created = new List<string>();
        try
        {
            for (var index = 0; index < 5; index++)
            {
                var username = $"{prefix}-{index}";
                var signup = await users.SignupAsync(new SignupRequestDto(username, "StreamTest@123"));
                Assert.True(signup.IsSuccessful, signup.ErrorMessage);
                var found = await users.GetUserByUsernameAsync(username);
                Assert.True(found.IsSuccessful, found.ErrorMessage);
                created.Add(Assert.Single(found.Response).Id);
            }

            var returned = new List<string>();
            await foreach (var user in users.GetAllUsersAsync(new GetUsersQueryDto { Search = prefix }, pageSize: 2))
                returned.Add(user.Id);
            Assert.Equal(created.OrderBy(id => id), returned.OrderBy(id => id));

            var limited = new List<string>();
            await foreach (var user in users.GetAllUsersAsync(new GetUsersQueryDto { Search = prefix, First = 1, Max = 3 }, pageSize: 2))
                limited.Add(user.Id);
            Assert.Equal(3, limited.Count);
            Assert.Equal(3, limited.Distinct().Count());
        }
        finally
        {
            foreach (var id in created) await users.DeleteUserAsync(id);
        }
    }
}
