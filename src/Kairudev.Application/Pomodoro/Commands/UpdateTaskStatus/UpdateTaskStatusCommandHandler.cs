using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Pomodoro;
using Kairudev.Domain.Tasks;
using TaskStatus = Kairudev.Domain.Tasks.TaskStatus;

namespace Kairudev.Application.Pomodoro.Commands.UpdateTaskStatus;

public sealed class UpdateTaskStatusCommandHandler
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;

    public UpdateTaskStatusCommandHandler(
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
    }

    public async Task<UpdateTaskStatusResult> HandleAsync(
        UpdateTaskStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetActiveAsync(cancellationToken);
        if (session is null)
            return UpdateTaskStatusResult.Failure("No active session");

        var task = await _taskRepository.GetByIdAsync(TaskId.From(command.TaskId), cancellationToken);
        if (task is null)
            return UpdateTaskStatusResult.NotFound();

        if (!Enum.TryParse<TaskStatus>(command.TargetStatus, true, out var newStatus))
            return UpdateTaskStatusResult.Failure($"Invalid status: {command.TargetStatus}");

        var result = task.ChangeStatus(newStatus, DateTime.UtcNow);
        if (result.IsFailure)
            return UpdateTaskStatusResult.Failure(result.Error);

        await _taskRepository.UpdateAsync(task, cancellationToken);

        return UpdateTaskStatusResult.Success(TaskViewModel.From(task));
    }
}
