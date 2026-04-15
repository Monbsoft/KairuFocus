using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Tasks;

using DomainTaskStatus = KairuFocus.Domain.Tasks.TaskStatus;

namespace KairuFocus.Application.Tests.Tasks;

internal sealed class FakeTaskRepository : ITaskRepository
{
    public List<DeveloperTask> Tasks { get; } = [];

    public Task AddAsync(DeveloperTask task, CancellationToken cancellationToken = default)
    {
        Tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task<DeveloperTask?> GetByIdAsync(TaskId id, UserId userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tasks.FirstOrDefault(t => t.Id == id && t.OwnerId == userId));

    public Task<IReadOnlyList<DeveloperTask>> GetAllAsync(
        UserId userId,
        DomainTaskStatus[]? statuses = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = Tasks.Where(t => t.OwnerId == userId);

        if (statuses is { Length: > 0 })
            query = query.Where(t => statuses.Contains(t.Status));

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t => t.Title.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        var result = query.OrderByDescending(t => t.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<DeveloperTask>>(result.AsReadOnly());
    }

    public Task UpdateAsync(DeveloperTask task, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(TaskId id, UserId userId, CancellationToken cancellationToken = default)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == id && t.OwnerId == userId);
        if (task is not null) Tasks.Remove(task);
        return Task.CompletedTask;
    }
}
