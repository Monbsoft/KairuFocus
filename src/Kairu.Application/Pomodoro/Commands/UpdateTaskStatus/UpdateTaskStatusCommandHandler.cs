using Kairu.Application.Common;
using Kairu.Application.Tasks.Common;
using Kairu.Domain.Pomodoro;
using Kairu.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using TaskStatus = Kairu.Domain.Tasks.TaskStatus;

namespace Kairu.Application.Pomodoro.Commands.UpdateTaskStatus;

public sealed class UpdateTaskStatusCommandHandler : ICommandHandler<UpdateTaskStatusCommand, UpdateTaskStatusResult>
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateTaskStatusCommandHandler> _logger;

    public UpdateTaskStatusCommandHandler(
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        ILogger<UpdateTaskStatusCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdateTaskStatusResult> Handle(
        UpdateTaskStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Updating task {TaskId} status to {Status} during session for user {UserId}", command.TaskId, command.TargetStatus, userId);

        var session = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("No active session found for user {UserId}", userId);
            return UpdateTaskStatusResult.Failure("No active session");
        }

        var task = await _taskRepository.GetByIdAsync(TaskId.From(command.TaskId), userId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for user {UserId}", command.TaskId, userId);
            return UpdateTaskStatusResult.NotFound();
        }

        if (!Enum.TryParse<TaskStatus>(command.TargetStatus, true, out var newStatus))
            return UpdateTaskStatusResult.Failure($"Invalid status: {command.TargetStatus}");

        var result = task.ChangeStatus(newStatus, DateTime.UtcNow);
        if (result.IsFailure)
            return UpdateTaskStatusResult.Failure(result.Error);

        await _taskRepository.UpdateAsync(task, cancellationToken);
        _logger.LogInformation("Task {TaskId} status updated to {Status} during session for user {UserId}", command.TaskId, newStatus, userId);

        return UpdateTaskStatusResult.Success(TaskViewModel.From(task));
    }
}
