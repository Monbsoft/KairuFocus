namespace KairuFocus.Domain.Identity;

public interface IUserRepository
{
    Task<User?> GetByGitHubIdAsync(string githubId, CancellationToken ct = default);
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
