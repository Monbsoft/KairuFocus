using Kairu.Application.Common;
using Kairu.Application.Tasks.Common;
using Kairu.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairu.Application.Tasks.Queries.GetTaskById;

/// <summary>
/// Handles the GetTaskById query.
/// </summary>
public sealed class GetTaskByIdQueryHandler : IQueryHandler<GetTaskByIdQuery, GetTaskByIdResult>
{
    private readonly ITaskRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTaskByIdQueryHandler> _logger;

    public GetTaskByIdQueryHandler(
        ITaskRepository repository,
        ICurrentUserService currentUserService,
        ILogger<GetTaskByIdQueryHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetTaskByIdResult> Handle(
        GetTaskByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Fetching task {TaskId} for user {UserId}", query.Id, userId);
        var taskId = TaskId.From(query.Id);
        var task = await _repository.GetByIdAsync(taskId, userId, cancellationToken);
        if (task is null)
            _logger.LogWarning("Task {TaskId} not found for user {UserId}", query.Id, userId);
        return new GetTaskByIdResult(task is null ? null : TaskViewModel.From(task));
    }
}
