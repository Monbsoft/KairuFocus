using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

using DomainTaskStatus = KairuFocus.Domain.Tasks.TaskStatus;

namespace KairuFocus.Infrastructure.Persistence;

internal sealed class EfCoreTaskRepository : ITaskRepository
{
    private readonly KairuFocusDbContext _context;

    public EfCoreTaskRepository(KairuFocusDbContext context) => _context = context;

    public async Task AddAsync(DeveloperTask task, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(task, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeveloperTask?> GetByIdAsync(TaskId id, UserId userId, CancellationToken cancellationToken = default) =>
        await _context.Tasks.FirstOrDefaultAsync(
            t => t.Id == id && t.OwnerId == userId, cancellationToken);

    public async Task<IReadOnlyList<DeveloperTask>> GetAllAsync(UserId userId, DomainTaskStatus[]? statuses = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.Where(t => t.OwnerId == userId);

        if (statuses is { Length: > 0 })
        {
            var statusList = statuses.ToList();
            query = query.Where(t => statusList.Contains(t.Status));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t => EF.Functions.Like(t.Title.Value, $"%{searchTerm}%"));

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(DeveloperTask task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskId id, UserId userId, CancellationToken cancellationToken = default)
    {
        var task = await GetByIdAsync(id, userId, cancellationToken);
        if (task is not null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
