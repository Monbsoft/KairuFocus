using Kairudev.Domain.Pomodoro;
using Kairudev.Domain.Tasks;

namespace Kairudev.Application.Pomodoro.Commands.LinkTask;

public sealed class LinkTaskCommandHandler
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;

    public LinkTaskCommandHandler(
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
    }

    public async Task<LinkTaskResult> HandleAsync(
        LinkTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetActiveAsync(cancellationToken);
        if (session is null)
            return LinkTaskResult.Failure("No active session");

        var task = await _taskRepository.GetByIdAsync(TaskId.From(command.TaskId), cancellationToken);
        if (task is null)
            return LinkTaskResult.NotFound();

        var result = session.LinkTask(task.Id);
        if (result.IsFailure)
            return LinkTaskResult.Failure(result.Error);

        await _sessionRepository.UpdateAsync(session, cancellationToken);
        return LinkTaskResult.Success();
    }
}
