using Keycloak.Net.Sdk.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keycloak.Net.Sdk.IntegrationTests;

/// <summary>
/// Finding users by data-scope attributes (e.g. the company and branch they work in) and counting them.
/// </summary>
[Collection(nameof(KeycloakCollection))]
public class UserSearchIntegrationTests(KeycloakFixture fixture)
{
    private IUserManagement User => fixture.Services.CreateScope().ServiceProvider.GetRequiredService<IUserManagement>();

    private async Task<string> CreateUserAsync(string username, params (string Key, string Value)[] attributes)
    {
        Assert.True((await User.SignupAsync(new SignupRequestDto(username, "Search@1234"))).IsSuccessful);
        var userId = (await User.GetUserByUsernameAsync(username)).Response.Single(u => u.Username == username).Id;
        foreach (var (key, value) in attributes)
            Assert.True((await User.SetUserAttributeAsync(userId, key, value)).IsSuccessful);
        return userId;
    }

    private static List<string> Sorted(IEnumerable<string> ids) => ids.OrderBy(id => id, StringComparer.Ordinal).ToList();

    [Fact]
    public async Task GetUsersAsync_ByAttributes_ReturnsOnlyUsersMatchingAll()
    {
        var run = Guid.NewGuid().ToString("N");
        var ids = new List<string>();
        try
        {
            var inBranch     = await CreateUserAsync($"a-{run}", ("company_id", run), ("branch_id", "12"));
            var otherBranch  = await CreateUserAsync($"b-{run}", ("company_id", run), ("branch_id", "13"));
            var otherCompany = await CreateUserAsync($"c-{run}", ("company_id", $"x{run}"), ("branch_id", "12"));
            ids.AddRange([inBranch, otherBranch, otherCompany]);

            var company = await User.GetUsersAsync(new GetUsersQueryDto
            {
                Attributes = new Dictionary<string, string> { ["company_id"] = run }
            });
            Assert.True(company.IsSuccessful, company.ErrorMessage);
            Assert.Equal(Sorted([inBranch, otherBranch]), Sorted(company.Response.Select(u => u.Id)));

            var branch = await User.GetUsersAsync(new GetUsersQueryDto
            {
                Attributes = new Dictionary<string, string> { ["company_id"] = run, ["branch_id"] = "12" }
            });
            Assert.True(branch.IsSuccessful, branch.ErrorMessage);
            Assert.Equal([inBranch], branch.Response.Select(u => u.Id).ToList());
        }
        finally
        {
            foreach (var id in ids) await User.DeleteUserAsync(id);
        }
    }

    [Fact]
    public async Task GetUsersAsync_AttributeValueWithSpace_IsMatched()
    {
        var run = Guid.NewGuid().ToString("N");
        var userId = await CreateUserAsync($"space-{run}", ("region", $"north {run}"));
        try
        {
            var result = await User.GetUsersAsync(new GetUsersQueryDto
            {
                Attributes = new Dictionary<string, string> { ["region"] = $"north {run}" }
            });

            Assert.True(result.IsSuccessful, result.ErrorMessage);
            Assert.Equal([userId], result.Response.Select(u => u.Id).ToList());
        }
        finally
        {
            await User.DeleteUserAsync(userId);
        }
    }

    [Fact]
    public async Task GetUsersAsync_AttributeSearchWithPaging_ReturnsPages()
    {
        var run = Guid.NewGuid().ToString("N");
        var ids = new List<string>();
        try
        {
            for (var i = 0; i < 3; i++)
                ids.Add(await CreateUserAsync($"page{i}-{run}", ("company_id", run)));

            var filter = new Dictionary<string, string> { ["company_id"] = run };
            var first  = await User.GetUsersAsync(new GetUsersQueryDto { Attributes = filter, First = 0, Max = 2 });
            var second = await User.GetUsersAsync(new GetUsersQueryDto { Attributes = filter, First = 2, Max = 2 });

            Assert.Equal(2, first.Response.Count);
            Assert.Single(second.Response);
            Assert.Equal(Sorted(ids), Sorted(first.Response.Concat(second.Response).Select(u => u.Id)));
        }
        finally
        {
            foreach (var id in ids) await User.DeleteUserAsync(id);
        }
    }

    [Fact]
    public async Task GetUsersAsync_ExactUsername_DoesNotMatchSubstrings()
    {
        var run = Guid.NewGuid().ToString("N");
        var shortId = await CreateUserAsync($"exact-{run}");
        var longId  = await CreateUserAsync($"exact-{run}-long");
        try
        {
            var fuzzy = await User.GetUsersAsync(new GetUsersQueryDto { Username = $"exact-{run}" });
            Assert.Equal(Sorted([shortId, longId]), Sorted(fuzzy.Response.Select(u => u.Id)));

            var exact = await User.GetUsersAsync(new GetUsersQueryDto { Username = $"exact-{run}", Exact = true });
            Assert.True(exact.IsSuccessful, exact.ErrorMessage);
            Assert.Equal([shortId], exact.Response.Select(u => u.Id).ToList());
        }
        finally
        {
            await User.DeleteUserAsync(shortId);
            await User.DeleteUserAsync(longId);
        }
    }

    [Fact]
    public async Task GetUsersCountAsync_CountsAllAndFilteredUsers()
    {
        var run = Guid.NewGuid().ToString("N");
        var ids = new List<string>();
        try
        {
            var before = await User.GetUsersCountAsync();
            Assert.True(before.IsSuccessful, before.ErrorMessage);
            Assert.True(before.Response.Count >= 1);

            ids.Add(await CreateUserAsync($"count-a-{run}", ("company_id", run)));
            ids.Add(await CreateUserAsync($"count-b-{run}", ("company_id", run)));

            var byAttribute = await User.GetUsersCountAsync(new GetUsersQueryDto
            {
                Attributes = new Dictionary<string, string> { ["company_id"] = run }
            });
            Assert.True(byAttribute.IsSuccessful, byAttribute.ErrorMessage);
            Assert.Equal(2, byAttribute.Response.Count);

            var bySearch = await User.GetUsersCountAsync(new GetUsersQueryDto { Search = $"count-a-{run}" });
            Assert.Equal(1, bySearch.Response.Count);
        }
        finally
        {
            foreach (var id in ids) await User.DeleteUserAsync(id);
        }
    }
}
