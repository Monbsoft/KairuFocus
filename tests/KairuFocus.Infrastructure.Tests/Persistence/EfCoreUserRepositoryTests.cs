using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Identity;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence;

public sealed class EfCoreUserRepositoryTests
{
    private readonly InMemoryDatabase _database = new();

    [Fact]
    public async Task Should_ReturnTrue_When_LoginBelongsToOtherWithDifferentCase()
    {
        await _database.SeedAsync(User.Create("gh-1", "OctoCat", "Octo", null));

        var used = await IsLoginUsedByOtherAsync("octocat", UserId.New());

        Assert.True(used);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_LoginIsOwn()
    {
        var owner = User.Create("gh-1", "octocat", "Octo", null);
        await _database.SeedAsync(owner);

        var used = await IsLoginUsedByOtherAsync("OCTOCAT", owner.Id);

        Assert.False(used);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_LoginIsUnknown()
    {
        await _database.SeedAsync(User.Create("gh-1", "octocat", "Octo", null));

        var used = await IsLoginUsedByOtherAsync("someone-else", UserId.New());

        Assert.False(used);
    }

    [Fact]
    public async Task Should_NotSelectEmailOrDisplayName_When_CheckingLogin()
    {
        var command = await SqlCommandCapture.CaptureAsync(context =>
            new EfCoreUserRepository(context).IsLoginUsedByOtherAsync("octocat", UserId.New()));

        Assert.DoesNotContain("[Email]", command.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[DisplayName]", command.Text, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IsLoginUsedByOtherAsync(string login, UserId userId)
    {
        await using var context = _database.CreateContext();
        return await new EfCoreUserRepository(context).IsLoginUsedByOtherAsync(login, userId);
    }
}
