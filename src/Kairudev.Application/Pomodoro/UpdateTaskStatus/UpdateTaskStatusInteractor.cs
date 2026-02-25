using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Pomodoro;
using PomodoroErrors = Kairudev.Domain.Pomodoro.DomainErrors;
using Kairudev.Domain.Tasks;

namespace Kairudev.Application.Pomodoro.UpdateTaskStatus;

public sealed class UpdateTaskStatusInteractor : IUpdateTaskStatusUseCase
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUpdateTaskStatusPresenter _presenter;

    public UpdateTaskStatusInteractor(
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository,
        IUpdateTaskStatusPresenter presenter)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _presenter = presenter;
    }

    public async Task Execute(UpdateTaskStatusRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetActiveAsync(cancellationToken);
        if (session is null)
        {
            _presenter.PresentFailure(PomodoroErrors.Pomodoro.SessionNotActive);
            return;
        }

        var taskId = TaskId.From(request.TaskId);
        if (!session.LinkedTaskIds.Contains(taskId))
        {
            _presenter.PresentFailure(PomodoroErrors.Pomodoro.TaskNotLinked);
            return;
        }

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            _presenter.PresentNotFound();
            return;
        }

        var result = request.TargetStatus switch
        {
            "InProgress" => task.StartProgress(),
            "Done" => task.Complete(),
            _ => Domain.Common.Result.Failure($"Unknown target status: {request.TargetStatus}")
        };

        if (result.IsFailure)
        {
            _presenter.PresentFailure(result.Error);
            return;
        }

        await _taskRepository.UpdateAsync(task, cancellationToken);
        _presenter.PresentSuccess(TaskViewModel.From(task));
    }
}
