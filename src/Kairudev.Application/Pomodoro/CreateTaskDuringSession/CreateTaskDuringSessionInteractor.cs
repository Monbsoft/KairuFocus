using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Pomodoro;
using PomodoroErrors = Kairudev.Domain.Pomodoro.DomainErrors;
using Kairudev.Domain.Tasks;

namespace Kairudev.Application.Pomodoro.CreateTaskDuringSession;

public sealed class CreateTaskDuringSessionInteractor : ICreateTaskDuringSessionUseCase
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICreateTaskDuringSessionPresenter _presenter;

    public CreateTaskDuringSessionInteractor(
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository,
        ICreateTaskDuringSessionPresenter presenter)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _presenter = presenter;
    }

    public async Task Execute(CreateTaskDuringSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetActiveAsync(cancellationToken);
        if (session is null)
        {
            _presenter.PresentFailure(PomodoroErrors.Pomodoro.SessionNotActive);
            return;
        }

        var titleResult = TaskTitle.Create(request.Title);
        if (titleResult.IsFailure)
        {
            _presenter.PresentValidationError(titleResult.Error);
            return;
        }

        var task = DeveloperTask.Create(titleResult.Value, DateTime.UtcNow);
        await _taskRepository.AddAsync(task, cancellationToken);

        session.LinkTask(task.Id);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _presenter.PresentSuccess(TaskViewModel.From(task));
    }
}
