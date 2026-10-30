using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Identity;

internal sealed class EfCoreUserRepository : IUserRepository
{
    private readonly KairuFocusDbContext _context;

    public EfCoreUserRepository(KairuFocusDbContext context) => _context = context;

    public async Task<User?> GetByGitHubIdAsync(string githubId, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.GitHubId == githubId, ct);

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> IsLoginUsedByOtherAsync(string login, UserId userId, CancellationToken ct = default)
    {
        var normalized = login.ToLowerInvariant();
        return await _context.Users.AnyAsync(u => u.Id != userId && u.Login.ToLower() == normalized, ct);
    }
}
