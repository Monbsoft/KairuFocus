using Kairudev.Application.Common;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandler : ICommandHandler<DeleteTaskCommand, DeleteTaskResult>
{
    private readonly ITaskRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteTaskCommandHandler> _logger;

    public DeleteTaskCommandHandler(
        ITaskRepository repository,
        ICurrentUserService currentUserService,
        ILogger<DeleteTaskCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DeleteTaskResult> Handle(
        DeleteTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Deleting task {TaskId} for user {UserId}", command.TaskId, userId);

        var task = await _repository.GetByIdAsync(TaskId.From(command.TaskId), userId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for user {UserId}", command.TaskId, userId);
            return DeleteTaskResult.NotFound();
        }

        await _repository.DeleteAsync(task.Id, userId, cancellationToken);
        _logger.LogInformation("Task {TaskId} deleted by user {UserId}", command.TaskId, userId);
        return DeleteTaskResult.Success();
    }
}
