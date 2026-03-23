using Kairudev.Application.Common;
using Kairudev.Application.Journal.Commands.CreateEntry;
using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Journal;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using TaskStatus = Kairudev.Domain.Tasks.TaskStatus;

namespace Kairudev.Application.Tasks.Commands.ChangeTaskStatus;

public sealed class ChangeTaskStatusCommandHandler : ICommandHandler<ChangeTaskStatusCommand, ChangeTaskStatusResult>
{
    private readonly ITaskRepository _repository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChangeTaskStatusCommandHandler> _logger;

    public ChangeTaskStatusCommandHandler(
        ITaskRepository repository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<ChangeTaskStatusCommandHandler> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ChangeTaskStatusResult> Handle(
        ChangeTaskStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Changing status of task {TaskId} to {Status} for user {UserId}", command.TaskId, command.NewStatus, userId);

        var task = await _repository.GetByIdAsync(TaskId.From(command.TaskId), userId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for user {UserId}", command.TaskId, userId);
            return ChangeTaskStatusResult.NotFound();
        }

        // Parse status string to enum
        if (!Enum.TryParse<TaskStatus>(command.NewStatus, true, out var newStatus))
            return ChangeTaskStatusResult.Validation($"Invalid status: {command.NewStatus}");

        var changeResult = task.ChangeStatus(newStatus, DateTime.UtcNow);
        if (changeResult.IsFailure)
            return ChangeTaskStatusResult.Conflict(changeResult.Error);

        await _repository.UpdateAsync(task, cancellationToken);
        _logger.LogInformation("Task {TaskId} status changed to {Status} by user {UserId}", command.TaskId, newStatus, userId);

        // Generate journal entries based on status
        if (newStatus == TaskStatus.InProgress)
        {
            await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
                new CreateEntryCommand(
                    JournalEventType.TaskStarted,
                    task.Id.Value,
                    DateTime.UtcNow,
                    userId),
                cancellationToken);
        }
        else if (newStatus == TaskStatus.Done)
        {
            await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
                new CreateEntryCommand(
                    JournalEventType.TaskCompleted,
                    task.Id.Value,
                    DateTime.UtcNow,
                    userId),
                cancellationToken);
        }

        return ChangeTaskStatusResult.Success(TaskViewModel.From(task));
    }
}
