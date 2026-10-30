namespace KairuFocus.Domain.Identity;

public interface IUserRepository
{
    Task<User?> GetByGitHubIdAsync(string githubId, CancellationToken ct = default);
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>Case-insensitive check: true when another user already has this login.</summary>
    Task<bool> IsLoginUsedByOtherAsync(string login, UserId userId, CancellationToken ct = default);
}
