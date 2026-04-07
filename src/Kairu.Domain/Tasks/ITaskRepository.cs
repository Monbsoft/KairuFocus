using Kairu.Domain.Identity;

namespace Kairu.Domain.Tasks;

public interface ITaskRepository
{
    Task AddAsync(DeveloperTask task, CancellationToken cancellationToken = default);
    Task<DeveloperTask?> GetByIdAsync(TaskId id, UserId userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeveloperTask>> GetAllAsync(UserId userId, TaskStatus[]? statuses = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task UpdateAsync(DeveloperTask task, CancellationToken cancellationToken = default);
    Task DeleteAsync(TaskId id, UserId userId, CancellationToken cancellationToken = default);
}
