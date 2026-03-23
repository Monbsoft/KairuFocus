using Kairudev.Application.Common;
using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

using DomainTaskStatus = Kairudev.Domain.Tasks.TaskStatus;

namespace Kairudev.Application.Tasks.Queries.ListTasks;

public sealed class ListTasksQueryHandler : IQueryHandler<ListTasksQuery, ListTasksResult>
{
    private readonly ITaskRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ListTasksQueryHandler> _logger;

    public ListTasksQueryHandler(
        ITaskRepository repository,
        ICurrentUserService currentUserService,
        ILogger<ListTasksQueryHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ListTasksResult> Handle(
        ListTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Listing tasks for user {UserId} with filter {StatusFilter} and search '{SearchTerm}'",
            userId, query.StatusFilter, query.SearchTerm);

        var statuses = ToStatuses(query.StatusFilter);
        var tasks = await _repository.GetAllAsync(userId, statuses, query.SearchTerm, cancellationToken);
        var viewModels = tasks.Select(TaskViewModel.From).ToList();

        _logger.LogDebug("Found {Count} tasks for user {UserId}", viewModels.Count, userId);
        return new ListTasksResult(viewModels);
    }

    private static DomainTaskStatus[]? ToStatuses(TaskStatusFilter filter) => filter switch
    {
        TaskStatusFilter.OpenOnly => [DomainTaskStatus.Pending, DomainTaskStatus.InProgress],
        TaskStatusFilter.All => null,
        TaskStatusFilter.Pending => [DomainTaskStatus.Pending],
        TaskStatusFilter.InProgress => [DomainTaskStatus.InProgress],
        TaskStatusFilter.Done => [DomainTaskStatus.Done],
        _ => null
    };
}
