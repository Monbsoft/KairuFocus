using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Identity;

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];
    public int UpdateCount { get; private set; }

    public Task<User?> GetByGitHubIdAsync(string githubId, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.GitHubId == githubId));

    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }

    public Task<bool> IsLoginUsedByOtherAsync(string login, UserId userId, CancellationToken ct = default)
        => Task.FromResult(Users.Any(u =>
            u.Id != userId && string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase)));
}
