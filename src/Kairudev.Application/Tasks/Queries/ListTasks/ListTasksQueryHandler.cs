using Kairudev.Application.Common;
using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairudev.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Handles the ListTasks query.
/// </summary>
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
        _logger.LogDebug("Listing tasks for user {UserId}", userId);
        var tasks = await _repository.GetAllAsync(userId, cancellationToken);
        var viewModels = tasks.Select(TaskViewModel.From).ToList();
        _logger.LogDebug("Found {Count} tasks for user {UserId}", viewModels.Count, userId);
        return new ListTasksResult(viewModels);
    }
}
