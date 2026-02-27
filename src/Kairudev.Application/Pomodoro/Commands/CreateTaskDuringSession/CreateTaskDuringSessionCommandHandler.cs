using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Pomodoro;
using Kairudev.Domain.Tasks;

namespace Kairudev.Application.Pomodoro.Commands.CreateTaskDuringSession;

public sealed class CreateTaskDuringSessionCommandHandler
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;

    public CreateTaskDuringSessionCommandHandler(
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
    }

    public async Task<CreateTaskDuringSessionResult> HandleAsync(
        CreateTaskDuringSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetActiveAsync(cancellationToken);
        if (session is null)
            return CreateTaskDuringSessionResult.Failure("No active session");

        var titleResult = TaskTitle.Create(command.Title);
        if (titleResult.IsFailure)
            return CreateTaskDuringSessionResult.Failure(titleResult.Error);

        var descriptionResult = TaskDescription.Create(command.Description);
        if (descriptionResult.IsFailure)
            return CreateTaskDuringSessionResult.Failure(descriptionResult.Error);

        var task = DeveloperTask.Create(titleResult.Value, descriptionResult.Value, DateTime.UtcNow);
        await _taskRepository.AddAsync(task, cancellationToken);

        var linkResult = session.LinkTask(task.Id);
        if (linkResult.IsFailure)
            return CreateTaskDuringSessionResult.Failure(linkResult.Error);

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return CreateTaskDuringSessionResult.Success(TaskViewModel.From(task));
    }
}
