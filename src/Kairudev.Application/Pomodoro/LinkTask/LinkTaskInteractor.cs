using Kairudev.Domain.Pomodoro;
using PomodoroErrors = Kairudev.Domain.Pomodoro.DomainErrors;
using Kairudev.Domain.Tasks;

namespace Kairudev.Application.Pomodoro.LinkTask;

public sealed class LinkTaskInteractor : ILinkTaskUseCase
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ILinkTaskPresenter _presenter;

    public LinkTaskInteractor(
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository,
        ILinkTaskPresenter presenter)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _presenter = presenter;
    }

    public async Task Execute(LinkTaskRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetActiveAsync(cancellationToken);
        if (session is null)
        {
            _presenter.PresentFailure(PomodoroErrors.Pomodoro.SessionNotActive);
            return;
        }

        var taskId = TaskId.From(request.TaskId);
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            _presenter.PresentNotFound();
            return;
        }

        var result = session.LinkTask(taskId);
        if (result.IsFailure)
        {
            _presenter.PresentFailure(result.Error);
            return;
        }

        await _sessionRepository.UpdateAsync(session, cancellationToken);
        _presenter.PresentSuccess();
    }
}
