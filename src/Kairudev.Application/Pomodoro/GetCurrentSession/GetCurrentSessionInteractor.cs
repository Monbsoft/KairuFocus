using Kairudev.Application.Pomodoro.Common;
using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.GetCurrentSession;

public sealed class GetCurrentSessionInteractor : IGetCurrentSessionUseCase
{
    private readonly IPomodoroSessionRepository _repository;
    private readonly IGetCurrentSessionPresenter _presenter;

    public GetCurrentSessionInteractor(
        IPomodoroSessionRepository repository,
        IGetCurrentSessionPresenter presenter)
    {
        _repository = repository;
        _presenter = presenter;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetActiveAsync(cancellationToken);
        if (session is null)
        {
            _presenter.PresentNoSession();
            return;
        }

        _presenter.PresentSession(PomodoroSessionViewModel.From(session));
    }
}
