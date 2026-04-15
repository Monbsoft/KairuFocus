using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Identity;

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByGitHubIdAsync(string githubId, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.GitHubId == githubId));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }
}
